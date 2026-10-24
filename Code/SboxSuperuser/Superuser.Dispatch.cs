using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Dispatches host-side authenticated player input; transport adapters supply the live caller, never a client-authored identity.</summary>
	public static async Task<CommandResult> ExecuteCommandAsync( Sandbox.Connection caller, CommandRequest request,
		CommandOrigin origin, CancellationToken cancellation = default )
	{
		ArgumentNullException.ThrowIfNull( request );
		if ( origin is not (CommandOrigin.Console or CommandOrigin.Chat) )
		{
			throw new ArgumentException( "Player transport must be console or chat.", nameof( origin ) );
		}

		var runtime = FindAuthorityRuntime();
		if ( runtime is null ) return new CommandResult( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
		if ( !runtime.TryResolvePlayer( caller, out _ ) ) return new CommandResult( CommandStatus.Denied, "A live authenticated player is required." );
		if ( !runtime.AdminRequests.TryEnter( caller.Id, DateTimeOffset.UtcNow ) )
			return new CommandResult( CommandStatus.Conflict, "Too many administration requests; wait before trying again." );
		try
		{
			return await runtime.Dispatcher.SubmitAsync( () => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null,
				origin, request, cancellation );
		}
		finally
		{
			runtime.AdminRequests.Exit( caller.Id );
		}
	}

	/// <summary>Issues a host-code service caller with explicit exact permissions; never expose this factory through a player transport.</summary>
	public static CommandService CreateCommandService( string serviceId, IReadOnlyList<string> permissions )
	{
		ContractGuard.Key( serviceId, nameof( serviceId ) );
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		var registered = runtime.Policy.RegisteredPermissions;
		if ( permissions is null || permissions.Count > 64 || permissions.Any( key => !registered.Contains( key, StringComparer.Ordinal ) ) )
		{
			throw new ArgumentException( "Service capabilities must name registered exact permissions.", nameof( permissions ) );
		}

		return new CommandService( serviceId, permissions, ( service, request, cancellation ) => runtime.Dispatcher.SubmitAsync(
			() => service.IsActive && runtime.IsUsable ? new ActorIdentity( ActorKind.InternalService, service.Id ) : null,
			CommandOrigin.InternalService, request, cancellation, service ) );
	}
}
