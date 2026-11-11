using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Core.Content;
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

	[Fact]
	public async Task TheMenuListsBansByKindForAdminsOnly()
	{
		using var rig = new ModerationRig();
		var content = new HostContentService( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add );
		await content.Initialization;
		using var commands = new AdministrationCommands( rig.Rig.Policy, null, rig.Rig.Registry, rig.Rig.Dispatcher,
			() => new CommandResult( CommandStatus.Success, "Test" ), content: content );
		await rig.Rig.Submit( "content.ban", arguments: new[] { "entity", "models/dev/box.vmdl" } );
		await rig.Rig.Submit( "content.ban", arguments: new[] { "map", "facepunch.construct", "Too", "big" } );

		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, content: content );
		var reply = await queries.QueryAsync( () => DispatcherRig.Owner, new AdminQuery( AdminQueryKind.ContentBans ), Guid.NewGuid(), CancellationToken.None );
		Assert.Equal( new[] { "map", "entity" }, reply.ContentBans.Select( ban => ban.Kind ) );
		Assert.Equal( "Too big", reply.ContentBans[ 0 ].Reason );
		var denied = await queries.QueryAsync( () => DispatcherRig.User, new AdminQuery( AdminQueryKind.ContentBans ), Guid.NewGuid(), CancellationToken.None );
		Assert.Equal( CommandStatus.Denied, denied.Status );
	}

	private sealed class Catalog : IContentCatalog
	{
		public Task<IReadOnlyList<ContentCatalogItem>> SearchAsync( string kind, string query, int take ) => Task.FromResult<IReadOnlyList<ContentCatalogItem>>(
			Enumerable.Range( 1, 40 ).Select( number => new ContentCatalogItem( $"facepunch.map{number}", $"Map {number}", $"https://example.com/{new string( 'x', 200 )}/{number}.png" ) ).ToArray() );

		public Task<ContentCatalogItem> DescribeAsync( string kind, string id ) => Task.FromResult( new ContentCatalogItem( id, "Described", "thumb.png" ) );
	}

	[Fact]
	public async Task SearchLeavesOutBannedItemsAndFitsTheResultLimit()
	{
		using var rig = new ModerationRig();
		var content = new HostContentService( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add );
		await content.Initialization;
		var catalog = new Catalog();
		using var commands = new AdministrationCommands( rig.Rig.Policy, null, rig.Rig.Registry, rig.Rig.Dispatcher,
			() => new CommandResult( CommandStatus.Success, "Test" ), content: content, contentCatalog: catalog );
		await rig.Rig.Submit( "content.ban", arguments: new[] { "map", "facepunch.map1" } );

		var found = await rig.Rig.Submit( "content.search", arguments: new[] { "map", "map" } );
		Assert.True( found.Status == CommandStatus.Success, found.Message );
		Assert.InRange( found.Data.Count, 1, 16 );
		Assert.DoesNotContain( found.Data.Values, json => json.Contains( "\"facepunch.map1\"" ) );

		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, content: content, contentCatalog: catalog );
		var banned = await queries.QueryAsync( () => DispatcherRig.Owner, new AdminQuery( AdminQueryKind.ContentBans ), Guid.NewGuid(), CancellationToken.None );
		Assert.Equal( "Described", Assert.Single( banned.ContentBans ).Title );
	}
}
