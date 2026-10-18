using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	[ConCmd( "superuser_owner_add" )]
	private static void AddOwnerLocally( params string[] arguments ) => _ = RunOwnerConsoleAsync( "add", arguments );

	[ConCmd( "superuser_owner_remove" )]
	private static void RemoveOwnerLocally( params string[] arguments ) => _ = RunOwnerConsoleAsync( "remove", arguments );

	[ConCmd( "superuser_owner_recover" )]
	private static void RecoverOwnersLocally( params string[] arguments ) => _ = RunOwnerConsoleAsync( "recover", arguments );

	[ConCmd( "superuser_policy_reload" )]
	private static void ReloadPolicyLocally() => _ = RunOwnerConsoleAsync( "reload", Array.Empty<string>() );

	[ConCmd( "superuser_authority_status" )]
	private static void PrintAuthorityStatus()
	{
		var health = GetAuthorityDiagnostics();
		if ( health is null )
		{
			Log.Info( "[Superuser] Authority runtime unavailable." );
			return;
		}

		Log.Info( $"[Superuser] policy={health.PolicyStatus}; revision={health.PolicyRevision}; ownerSource={health.OwnerSource}; owners={health.OwnerStatus}; ownerRevision={health.OwnerRevision}; count={health.OwnerCount}; lastWrite={health.LastWriteStatus}" );
	}

	private static async Task RunOwnerConsoleAsync( string operation, string[] arguments )
	{
		try
		{
			var runtime = FindAuthorityRuntime();
			if ( runtime is null || !runtime.TryResolveDedicatedConsole( out _ ) )
			{
				Log.Warning( "[Superuser] Owner recovery requires the non-forwarded local console on an actual dedicated runtime." );
				return;
			}

			ActorIdentity Authenticate() => runtime.TryResolveDedicatedConsole( out var actor ) ? actor : null;
			CommandResult result;
			if ( operation == "reload" )
			{
				result = await runtime.Policy.ReloadAsync( CancellationToken.None );
			}
			else if ( operation is "add" or "remove" && arguments.Length == 1 )
			{
				result = await runtime.Policy.ChangeOwnerAsync( Authenticate, arguments[ 0 ], operation == "add", Guid.NewGuid(), CancellationToken.None );
			}
			else if ( operation == "recover" && arguments.Length >= 1
				&& (arguments[ 0 ] == "replace" && arguments.Length >= 2 || arguments[ 0 ] == "clear" && arguments.Length == 1) )
			{
				var replacement = arguments[ 0 ] == "clear" ? Array.Empty<string>() : arguments.Skip( 1 ).ToArray();
				result = await runtime.Policy.RecoverOwnersAsync( Authenticate, replacement, Guid.NewGuid(), CancellationToken.None );
			}
			else
			{
				Log.Info( "[Superuser] Usage: superuser_owner_add <steamid>; superuser_owner_remove <steamid>; superuser_owner_recover replace <steamid> [steamid...]; superuser_owner_recover clear; superuser_policy_reload" );
				return;
			}

			Log.Info( $"[Superuser] Owner/policy operation status={result.Status}; {result.Message}" );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Owner recovery request failed; type={error.GetType().Name}; no success reported." );
		}
	}
}
