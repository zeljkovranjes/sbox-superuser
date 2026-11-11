using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.Messages;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class MessageTemplateTests
{
	[Fact]
	public async Task OwnersChangeWordingThatAnnouncementsUseAndSurvivesReload()
	{
		using var rig = new DispatcherRig();
		var messages = new HostMessageService( rig.Store, () => rig.Active, rig.Errors.Add );
		await messages.Initialization;
		using var commands = new AdministrationCommands( rig.Policy, null, rig.Registry, rig.Dispatcher,
			() => new CommandResult( CommandStatus.Success, "Test" ), messages: messages );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "messages.set", arguments: new[] { "kick", "{target}", "was", "removed", "by", "{actor}." } )).Status );
		Assert.Equal( "{target} was removed by {actor}.", messages.Catalog.Template( "kick" ) );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Submit( "messages.set", arguments: new[] { "kick", "{map}" } )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "messages.set", DispatcherRig.User, new[] { "tag", "HACKED" } )).Status );

		var reloaded = new HostMessageService( rig.Store, () => rig.Active, rig.Errors.Add );
		await reloaded.Initialization;
		Assert.Equal( "{target} was removed by {actor}.", reloaded.Catalog.Template( "kick" ) );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "messages.reset", arguments: new[] { "kick" } )).Status );
		Assert.Empty( messages.Catalog.Overrides );
		Assert.Empty( rig.Errors );
	}
}
