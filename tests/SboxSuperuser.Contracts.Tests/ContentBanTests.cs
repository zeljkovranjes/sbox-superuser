using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.Content;
using SboxSuperuser.Engine.Votes;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ContentBanTests
{
	[Fact]
	public async Task BannedMapsAreRefusedAndBansSurviveReload()
	{
		using var rig = new DispatcherRig();
		var content = new HostContentService( rig.Store, () => rig.Active, rig.Errors.Add );
		await content.Initialization;
		var votes = new VoteSession( _ => { }, () => Array.Empty<string>(), _ => Task.CompletedTask );
		using var commands = new AdministrationCommands( rig.Policy, null, rig.Registry, rig.Dispatcher,
			() => new CommandResult( CommandStatus.Success, "Test" ), votes: votes, content: content );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "content.ban", arguments: new[] { "map", "facepunch.construct", "Too", "big" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Submit( "content.ban", arguments: new[] { "addon", "badauthor.*" } )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Submit( "content.ban", arguments: new[] { "vehicle", "car" } )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Submit( "content.ban", DispatcherRig.User, new[] { "map", "x" } )).Status );

		var refused = await rig.Submit( "votemap", arguments: new[] { "facepunch.flatgrass", "facepunch.construct" } );
		Assert.Equal( CommandStatus.Denied, refused.Status );
		Assert.Contains( "facepunch.construct is banned", refused.Message );
		Assert.False( content.IsAllowed( "addon", "BadAuthor.Guns" ) );

		var reloaded = new HostContentService( rig.Store, () => rig.Active, rig.Errors.Add );
		await reloaded.Initialization;
		Assert.Equal( 2, reloaded.Bans.Bans.Count );
		Assert.Equal( "Too big", reloaded.Bans.Find( "map", "facepunch.construct" ).Reason );

		Assert.Equal( CommandStatus.Success, (await rig.Submit( "content.unban", arguments: new[] { "map", "facepunch.construct" } )).Status );
		Assert.Equal( CommandStatus.NoChange, (await rig.Submit( "content.unban", arguments: new[] { "map", "facepunch.construct" } )).Status );
		Assert.True( content.IsAllowed( "map", "facepunch.construct" ) );
		Assert.Empty( rig.Errors );
	}
}
