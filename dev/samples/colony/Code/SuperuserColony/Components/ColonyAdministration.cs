using Sandbox;
using SboxSuperuser;
using SuperuserColony.Engine.Administration;
using SuperuserColony.Systems;

namespace SuperuserColony.Components;

/// <summary>Connects this game's scene roster to optional administration commands.</summary>
public sealed class ColonyAdministration : Component, Component.INetworkListener
{
	private CommandRegistrationScope _registration;
	private int _generation;

	/// <summary>Installs the optional gameplay service; re-enable the component to apply changes.</summary>
	[Property]
	public bool ProvideGameplayCapability { get; set; } = true;

	protected override void OnEnabled() => _ = RegisterWhenReadyAsync( ++_generation );

	private async Task RegisterWhenReadyAsync( int generation )
	{
		try
		{
			for ( var attempt = 0; attempt < 200 && IsValid && Enabled && generation == _generation; attempt++ )
			{
				if ( Superuser.GetDiagnostics().RuntimeId.HasValue )
				{
					foreach ( var connection in Connection.All ) AddPlayer( connection );
					_registration = ColonyCommands.Register( ProvideGameplayCapability ? new RosterSupplies( Scene.GetSystem<ColonyRoster>().Replenish ) : null );
					return;
				}
				await Task.DelaySeconds( 0.05f );
			}
		}
		catch ( OperationCanceledException ) { }
		catch ( Exception error ) { Log.Warning( $"Colony administration could not start: {error.GetType().Name}" ); }
	}

	private void AddPlayer( Connection connection )
	{
		if ( Networking.IsHost && connection.IsActive && connection.SteamId != 0 ) Scene.GetSystem<ColonyRoster>().Join( connection.SteamId.ToString() );
	}

	void Component.INetworkListener.OnActive( Connection connection ) => AddPlayer( connection );

	protected override void OnDisabled()
	{
		_generation++;
		_registration?.Dispose();
		_registration = null;
	}
}
