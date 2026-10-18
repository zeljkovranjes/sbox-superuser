using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Contracts.Tests;

internal sealed class AdministrationRig : IDisposable
{
	internal readonly AuditStoreHarness Store = new();
	internal readonly HostAuditService Audit;
	internal readonly HostPolicyService Policy;
	internal readonly CommandRegistry Registry;
	internal readonly HostCommandDispatcher Dispatcher;
	internal readonly AdministrationCommands Commands;

	internal AdministrationRig()
	{
		Audit = new( Store, () => true, _ => { } );
		Policy = new( Store, new[] { DispatcherRig.OwnerId }, StoreReadStatus.Found, () => true, _ => { }, Audit );
		Registry = new( Policy.RegisteredPermissions, Policy.ReplaceRegisteredPermissions, () => true );
		Dispatcher = new( Policy, Registry, () => true, () => Array.Empty<CommandPlayer>(), _ => { }, audit: Audit );
		Commands = new( Policy, Audit, Registry, Dispatcher, () => new CommandResult( CommandStatus.Success, "healthy" ) );
	}

	internal Task<CommandResult> Run( string[] tokens, ActorIdentity actor = null )
	{
		var route = CommandRoute.Resolve( tokens );
		return Dispatcher.SubmitAsync( () => actor ?? DispatcherRig.Owner, CommandOrigin.Console,
			new CommandRequest( route.Command, route.Arguments, Guid.NewGuid() ), CancellationToken.None );
	}

	public void Dispose()
	{
		Commands.Dispose();
		Dispatcher.Dispose();
	}
}
