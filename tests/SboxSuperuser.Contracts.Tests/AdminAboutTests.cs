using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminAboutTests
{
	private static readonly ModuleStatus[] Installed =
	{
		new( new ModuleDescriptor( "sample.counter", "Counter", 1 ), ModuleState.Enabled, "Running." )
	};

	private static Task<AdminReply> About( ModerationRig rig, ActorIdentity actor )
	{
		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, () => Installed );
		return queries.QueryAsync( () => actor, new AdminQuery( AdminQueryKind.About ), Guid.NewGuid(), CancellationToken.None );
	}

	[Fact]
	public async Task OwnerSeesInstalledModules()
	{
		using var rig = new ModerationRig();
		var reply = await About( rig, DispatcherRig.Owner );
		Assert.Equal( CommandStatus.Success, reply.Status );
		Assert.True( reply.ModulesAllowed );
		var module = Assert.Single( reply.Modules );
		Assert.Equal( "sample.counter", module.Id );
		Assert.Equal( "Enabled", module.State );
	}

	[Fact]
	public async Task OrdinaryPlayerGetsAboutWithoutModules()
	{
		using var rig = new ModerationRig();
		var reply = await About( rig, DispatcherRig.User );
		Assert.Equal( CommandStatus.Success, reply.Status );
		Assert.False( reply.ModulesAllowed );
		Assert.Empty( reply.Modules );
	}

	[Fact]
	public void AboutAcceptsNoSearchOrPage()
	{
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.About, "x" ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.About, "", 1 ) );
	}
}
