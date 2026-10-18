using Sandbox;
using SboxSuperuser;
using SboxSuperuser.Components;
using SuperuserExamples.Engine;

namespace SuperuserExamples.Components;

/// <summary>Selects storage before enabling a disabled bootstrap in an active host session.</summary>
public sealed class CustomStorageSetup : Component
{
	/// <summary>The scene's initially disabled Superuser Bootstrap.</summary>
	[Property]
	public SuperuserBootstrap Bootstrap { get; set; }

	protected override void OnStart() => _ = ConfigureAsync();

	private async Task ConfigureAsync()
	{
		try
		{
			while ( !Networking.IsActive ) await Task.DelaySeconds( 0.05f );
			if ( !Networking.IsHost ) return;
			if ( !Bootstrap.IsValid() || Bootstrap.Enabled )
				throw new InvalidOperationException( "Assign one initially disabled Superuser Bootstrap." );
			Superuser.ConfigureStorage( context => new CustomAdminStore( context.LocalStore ) );
			Bootstrap.Enabled = true;
		}
		catch ( OperationCanceledException ) { }
		catch ( Exception error ) { Log.Warning( $"Custom storage setup: {error.Message}" ); }
	}
}
