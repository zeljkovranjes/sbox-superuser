using Sandbox;
using SboxSuperuser;
using SuperuserArena.Engine.Administration;

namespace SuperuserArena.Components;

/// <summary>Connects this game's component-based players to optional administration commands.</summary>
public sealed class ArenaAdministration : Component, Component.INetworkListener
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
					_registration = ArenaCommands.Register( ProvideGameplayCapability ? new PawnRecovery( RestorePawn ) : null );
					return;
				}
				await Task.DelaySeconds( 0.05f );
			}
		}
		catch ( OperationCanceledException ) { }
		catch ( Exception error ) { Log.Warning( $"Arena administration could not start: {error.GetType().Name}" ); }
	}

	private bool RestorePawn( string steamId )
	{
		var pawn = Scene.GetAllComponents<ArenaPawn>().FirstOrDefault( item => item.SteamId == steamId );
		if ( !pawn.IsValid() ) return false;
		pawn.Restore();
		return true;
	}

	private void AddPlayer( Connection connection )
	{
		if ( !Networking.IsHost || !connection.IsActive || connection.SteamId == 0 ) return;
		var id = connection.SteamId.ToString();
		if ( Scene.GetAllComponents<ArenaPawn>().Any( item => item.SteamId == id ) ) return;
		var player = new GameObject( true, "Arena Player" );
		player.Components.Create<ArenaPawn>().Bind( id );
	}

	void Component.INetworkListener.OnActive( Connection connection ) => AddPlayer( connection );

	protected override void OnDisabled()
	{
		_generation++;
		_registration?.Dispose();
		_registration = null;
	}
}
