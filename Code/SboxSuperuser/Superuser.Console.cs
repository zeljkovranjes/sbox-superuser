using Sandbox;
using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Console;

namespace SboxSuperuser;

public static partial class Superuser
{
	[ConCmd( "superuser_audit_status" )]
	private static void NativeAuditStatus()
	{
		var health = GetAuditDiagnostics();
		if ( health is null )
		{
			Log.Info( "[Superuser] Audit diagnostics require an eligible host-local runtime." );
			return;
		}

		// Host-local recovery diagnostics must remain readable when required command auditing is unavailable.
		Log.Info( $"[Superuser] auditDegraded={health.IsDegraded}; read={health.ReadStatus}; write={health.LastWriteStatus}; revision={health.Revision}; retained={health.RetainedCount}; retentionPending={health.RetentionPending}" );
	}

	[ConCmd( "admin" )]
	private static void NativeAdministration( params string[] arguments ) => LaunchConsole( "admin", arguments );

	[ConCmd( "superuser" )]
	private static void NativeConfiguredRoot( string root, params string[] arguments ) => LaunchConsole( root, arguments );

	private static void LaunchConsole( string root, string[] arguments )
	{
		if ( !Networking.IsActive )
		{
			Log.Warning( "[Superuser] An active dedicated session or opted-in editor host is required." );
			return;
		}

		var id = Guid.NewGuid();
		if ( Application.IsDedicatedServer )
		{
			_ = RunConsoleAsync( null, true, id, root, arguments );
			return;
		}

		if ( root is null || root.Length > 32 || CommandTokenizer.ValidateTokens( arguments ) is not null )
		{
			Log.Warning( "[Superuser] Console request exceeds its input limits." );
			return;
		}

		// The engine quotes argument-array values and supplies the authenticated Connection on the host.
		ConsoleSystem.Run( "superuser_request", new object[] { id.ToString( "N" ), root }.Concat( arguments.Cast<object>() ).ToArray() );
	}

	[ConCmd( "superuser_request", ConVarFlags.Server )]
	private static void ForwardConsole( Connection caller, string requestId, string root, params string[] arguments )
	{
		if ( !Guid.TryParseExact( requestId, "N", out var id ) || id == Guid.Empty )
		{
			ConsoleReplies.Send( caller, false, Guid.NewGuid(), new CommandResult( CommandStatus.InvalidArguments, "An explicit request ID is required." ) );
			return;
		}
		_ = RunConsoleAsync( caller, false, id, root, arguments );
	}

	private static async Task RunConsoleAsync( Connection caller, bool dedicatedLocal, Guid requestId, string root, string[] arguments )
	{
		CommandResult result;
		var runtime = FindAuthorityRuntime();
		var entered = false;
		try
		{
			if ( runtime is null || !runtime.ConsoleRoot.IsAvailable || runtime.ConsoleRoot.Name != root )
			{
				result = new CommandResult( CommandStatus.Unsupported, "Console root unavailable or mismatched. Check superuser_status and the bootstrap Command Root." );
			}
			else if ( !dedicatedLocal && !runtime.TryResolvePlayer( caller, out _ ) )
			{
				result = new CommandResult( CommandStatus.Denied, "A live authenticated player is required." );
			}
			else if ( !dedicatedLocal && !runtime.AdminRequests.TryEnter( caller.Id, DateTimeOffset.UtcNow ) )
			{
				result = new CommandResult( CommandStatus.Conflict, "Too many administration requests; wait before trying again." );
			}
			else
			{
				entered = !dedicatedLocal;
				ActorIdentity Authenticate() => dedicatedLocal
					? runtime.TryResolveDedicatedConsole( out var local ) ? local : null
					: runtime.TryResolvePlayer( caller, out var player ) ? player : null;
				var route = CommandRoute.Resolve( arguments );
				result = await runtime.Dispatcher.SubmitAsync( Authenticate, CommandOrigin.Console,
					new CommandRequest( route.Command, route.Arguments, requestId ), CancellationToken.None );
			}
		}
		catch ( ArgumentException )
		{
			result = new CommandResult( CommandStatus.InvalidArguments, "Invalid console tokens. Use admin help; no native command text is evaluated." );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Console request failed; type={error.GetType().Name}." );
			result = new CommandResult( CommandStatus.Unknown, "Console request has no confirmed outcome." );
		}
		finally
		{
			if ( entered ) runtime.AdminRequests.Exit( caller.Id );
		}

		ConsoleReplies.Send( caller, dedicatedLocal, requestId, result );
	}
}
