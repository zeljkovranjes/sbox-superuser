using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Contracts.Tests;

internal sealed class DispatcherRig : IDisposable
{
	internal const string OwnerId = "76561198000000001";
	internal const string UserId = "76561198000000002";
	internal const string TargetId = "76561198000000003";
	internal static readonly ActorIdentity Owner = new( ActorKind.Player, OwnerId );
	internal static readonly ActorIdentity User = new( ActorKind.Player, UserId );
	internal static readonly ActorIdentity ConsoleActor = new( ActorKind.DedicatedConsole, "dedicated-console" );
	internal readonly MemoryPolicyStore Store = new();
	internal readonly List<Exception> Errors = new();
	internal readonly List<CommandPlayer> Players = new() { new CommandPlayer( TargetId, "Target" ) };
	internal bool Active = true;
	internal HostPolicyService Policy { get; }
	internal CommandRegistry Registry { get; }
	internal HostCommandDispatcher Dispatcher { get; }
	private readonly List<CommandRegistrationScope> _scopes = new();

	internal DispatcherRig()
	{
		Policy = new HostPolicyService( Store, new[] { OwnerId }, StoreReadStatus.Found, () => Active, Errors.Add );
		Registry = new CommandRegistry( Policy.RegisteredPermissions, Policy.ReplaceRegisteredPermissions, () => Active );
		Dispatcher = new HostCommandDispatcher( Policy, Registry, () => Active, () => Players.ToArray(), Errors.Add );
		var permission = new CommandRegistrationScope( Registry, "permissions" );
		permission.RegisterPermission( new PermissionDescriptor( "test.use", "Test permission" ) );
		permission.Commit();
		_scopes.Add( permission );
	}

	internal CommandRegistrationScope Register( string name, CommandHandler handler, CommandArgumentDescriptor[] arguments = null,
		CommandExecutionPolicy execution = CommandExecutionPolicy.Mutation, string module = null, Action<CommandRegistrationScope> services = null )
	{
		var scope = new CommandRegistrationScope( Registry, module ?? name );
		scope.RegisterCommand( new CommandDescriptor( name, "Test command", "test.use", module ?? name, arguments, executionPolicy: execution ), handler );
		services?.Invoke( scope );
		scope.Commit();
		_scopes.Add( scope );
		return scope;
	}

	internal Task<CommandResult> Submit( string name, ActorIdentity actor = null, string[] arguments = null, Guid? id = null,
		CancellationToken cancellation = default, Func<ActorIdentity> authenticate = null, CommandOrigin origin = CommandOrigin.Console )
		=> Dispatcher.SubmitAsync( authenticate ?? (() => actor ?? Owner), origin,
			new CommandRequest( name, arguments ?? Array.Empty<string>(), id ?? Guid.NewGuid() ), cancellation );

	internal Task<CommandResult> Edit( PolicyChange change ) => Policy.ApplyAsync( () => Owner, change, CommandOrigin.Console, Guid.NewGuid(), CancellationToken.None );

	public void Dispose()
	{
		Dispatcher.Dispose();
		foreach ( var scope in _scopes.AsEnumerable().Reverse() ) scope.Dispose();
	}
}
