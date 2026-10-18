using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.Moderation;

namespace SboxSuperuser.Contracts.Tests;

// Orchestration fixture with a storage port and disconnect observer; it does not simulate Sandbox networking.
internal sealed class ModerationRig : IDisposable
{
	internal DispatcherRig Rig { get; } = new();
	internal HostModerationService Moderation { get; }
	internal AdministrationCommands Commands { get; }
	internal DateTimeOffset Now = new( 2026, 10, 18, 0, 0, 0, TimeSpan.Zero );
	internal int DisconnectRequests;
	internal bool RecordExistedBeforeDisconnect;

	internal ModerationRig()
	{
		Moderation = new( Rig.Store, () => Rig.Active, Rig.Dispatcher.CanUse,
			(actor, target) => Rig.Policy.CheckTarget( actor, target ).IsAllowed, Rig.Errors.Add, () => Now );
		Commands = new( Rig.Policy, null, Rig.Registry, Rig.Dispatcher, () => new CommandResult( CommandStatus.Success, "Test" ),
			moderation: Moderation, disconnect: (context, target, permission, ban) =>
			{
				DisconnectRequests++;
				RecordExistedBeforeDisconnect = Rig.Store.Documents.ContainsKey( MemoryPolicyStore.Id( HostModerationService.Key ) );
				return Task.FromResult( new CommandResult( CommandStatus.Success, "Disconnect observer invoked" ) );
			} );
	}

	internal Task<CommandResult> Run( string command, params string[] arguments ) => Rig.Submit( command, arguments: arguments );
	public void Dispose()
	{
		Commands.Dispose();
		Rig.Dispose();
	}
}
