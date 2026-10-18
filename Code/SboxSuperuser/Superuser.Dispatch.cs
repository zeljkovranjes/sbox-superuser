using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Dispatches host-side authenticated player input; transport adapters supply the live caller, never a client-authored identity.</summary>
	public static Task<CommandResult> ExecuteCommandAsync( Sandbox.Connection caller, CommandRequest request,
		CommandOrigin origin, CancellationToken cancellation = default )
	{
		ArgumentNullException.ThrowIfNull( request );
		if ( origin is not (CommandOrigin.Console or CommandOrigin.Chat) )
		{
			throw new ArgumentException( "Player transport must be console or chat.", nameof( origin ) );
		}

		var runtime = FindAuthorityRuntime();
		return runtime is null ? Task.FromResult( new CommandResult( CommandStatus.Unsupported, "An eligible authoritative runtime is required." ) )
			: runtime.Dispatcher.SubmitAsync( () => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null, origin, request, cancellation );
	}

	/// <summary>Issues a host-code service caller with explicit exact permissions; never expose this factory through a player transport.</summary>
	public static CommandService CreateCommandService( string serviceId, IReadOnlyList<string> permissions )
	{
		ContractGuard.Key( serviceId, nameof( serviceId ) );
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		if ( permissions is null || permissions.Any( key => !runtime.Policy.RegisteredPermissions.Contains( key, StringComparer.Ordinal ) ) )
		{
			throw new ArgumentException( "Service capabilities must name registered exact permissions.", nameof( permissions ) );
		}

		return new CommandService( serviceId, permissions, ( service, request, cancellation ) => runtime.Dispatcher.SubmitAsync(
			() => service.IsActive && runtime.IsUsable ? new ActorIdentity( ActorKind.InternalService, service.Id ) : null,
			CommandOrigin.InternalService, request, cancellation, service ) );
	}
}
