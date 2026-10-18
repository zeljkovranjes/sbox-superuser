using SboxSuperuser.Engine.Modules;

namespace SboxSuperuser.Contracts.Tests;

internal sealed class ModuleFixture : IDisposable
{
	internal DispatcherRig Rig { get; } = new();
	internal ModuleManager Manager { get; }
	internal ModuleFixture( int timeoutMilliseconds = 1000 )
	{
		Manager = new ModuleManager( Rig.Registry, Rig.Policy, () => Rig.Active, Rig.Errors.Add,
			deadline: TimeSpan.FromMilliseconds( timeoutMilliseconds ) );
	}
	internal Task<CommandResult> Enable( string id, ActorIdentity actor = null )
		=> Manager.SetEnabledAsync( () => actor ?? DispatcherRig.Owner, id, true, CancellationToken.None );
	internal Task<CommandResult> Disable( string id, ActorIdentity actor = null )
		=> Manager.SetEnabledAsync( () => actor ?? DispatcherRig.Owner, id, false, CancellationToken.None );
	internal ModuleState State( string id ) => Manager.Statuses.Single( item => item.Descriptor.Id == id ).State;
	public void Dispose()
	{
		Manager.Dispose();
		Rig.Dispose();
	}
}
