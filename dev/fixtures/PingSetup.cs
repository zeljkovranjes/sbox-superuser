using Sandbox;
using SboxSuperuser;
using SuperuserExamples.Engine;

namespace SuperuserExamples.Components;

/// <summary>Installs the tutorial module once the host bootstrap is ready.</summary>
public sealed class PingSetup : Component
{
	protected override void OnStart() => _ = RegisterAsync();

	private async Task RegisterAsync()
	{
		try
		{
			for ( var attempt = 0; attempt < 200; attempt++ )
			{
				if ( Superuser.GetDiagnostics().RuntimeId.HasValue )
				{
					var result = await Superuser.RegisterModulesAsync( new IAdminModule[] { new PingModule() } );
					if ( result.Status != CommandStatus.Success )
					{
						Log.Warning( $"Ping setup: {result.Status}" );
					}
					return;
				}
				await Task.DelaySeconds( 0.05f );
			}
			if ( Networking.IsHost ) Log.Warning( "Ping setup: bootstrap was not ready." );
		}
		catch ( OperationCanceledException ) { }
		catch ( Exception error ) { Log.Warning( $"Ping setup: {error.GetType().Name}" ); }
	}
}
