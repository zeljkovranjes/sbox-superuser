using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser;

public static partial class Superuser
{
	[ConCmd( "superuser_store_backups" )]
	private static void InspectStoreBackups( params string[] arguments )
	{
		_ = RunStorageConsoleAsync( false, arguments );
	}

	[ConCmd( "superuser_store_recover" )]
	private static void RestoreStoreBackup( params string[] arguments )
	{
		_ = RunStorageConsoleAsync( true, arguments );
	}

	private static async Task RunStorageConsoleAsync( bool recover, string[] arguments )
	{
		try
		{
			var scene = Game.ActiveScene;
			var runtime = scene.IsValid() ? scene.GetSystem<SuperuserSession>()?.GetRuntime() : null;
			if ( runtime is null || !runtime.TryResolveDedicatedConsole( out var actor ) )
			{
				Log.Warning( "[Superuser] Storage recovery requires the non-forwarded local console on an actual dedicated runtime." );
				return;
			}

			if ( arguments.Length != (recover ? 4 : 2) )
			{
				Log.Info( "[Superuser] Usage: superuser_store_backups <module> <document>; superuser_store_recover <module> <document> <generation-guid> <observed-head-revision>" );
				return;
			}

			var key = new StoreKey( arguments[ 0 ], arguments[ 1 ] );
			if ( !recover )
			{
				var backups = await runtime.Store.GetBackupsAsync( key, CancellationToken.None );
				Log.Info( $"[Superuser] Storage status={backups.Status}; head={backups.HeadRevision}; compatibleBackups={backups.Backups.Count}; more={backups.HasMore}" );
				foreach ( var backup in backups.Backups )
				{
					Log.Info( $"[Superuser] Backup generation={backup.GenerationId:N}; revision={backup.Revision}; schema={backup.SchemaVersion}" );
				}

				return;
			}

			if ( !Guid.TryParse( arguments[ 2 ], out var generation ) || !long.TryParse( arguments[ 3 ], out var head ) )
			{
				throw new ArgumentException( "Recovery requires a generation GUID and observed head revision." );
			}

			var intent = new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), actor.Kind, actor.SubjectId,
				CommandOrigin.Console, "storage.recover", DateTimeOffset.UtcNow,
				new Dictionary<string, string> { [ "generation" ] = generation.ToString( "N" ), [ "head" ] = head.ToString() } );
			var result = await runtime.Store.RecoverAsync( key, generation, head, intent, CancellationToken.None );
			Log.Info( $"[Superuser] Recovery status={result.Status}; committedRevision={result.CommittedRevision}" );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Storage console request failed; type={error.GetType().Name}. No success reported; inspect storage before retrying." );
		}
	}
}
