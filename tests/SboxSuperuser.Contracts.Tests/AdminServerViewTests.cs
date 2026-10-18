using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminServerViewTests
{
	private const string SecondTarget = "76561198000000004";

	private static HostAdminQueries Queries( ModerationRig rig ) => new(
		rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players,
		commands: () => rig.Rig.Registry.Descriptors,
		diagnostics: () => new Dictionary<string, string> { [ "mode" ] = "test" } );

	private static Task<AdminReply> Read( ModerationRig rig, AdminQuery query, ActorIdentity actor = null )
		=> Queries( rig ).QueryAsync( () => actor ?? DispatcherRig.Owner, query, Guid.NewGuid(), CancellationToken.None );

	[Fact]
	public async Task RestrictionsListActiveBansAcrossPlayersAndHideBansFromModerators()
	{
		using var rig = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await rig.Run( "ban", DispatcherRig.TargetId, "1d", "Griefing" )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "mute", SecondTarget, "1h", "Spam" )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "warn", SecondTarget, "Language" )).Status );

		var owner = await Read( rig, new AdminQuery( AdminQueryKind.Restrictions ) );
		Assert.Equal( 2, owner.Restrictions.Length );
		Assert.Contains( owner.Restrictions, row => row.SteamId == DispatcherRig.TargetId && row.Kind == "Ban" );
		Assert.Contains( AdminAction.Unban, owner.Actions );

		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		var moderator = await Read( rig, new AdminQuery( AdminQueryKind.Restrictions ), DispatcherRig.User );
		Assert.Equal( "TextMute", Assert.Single( moderator.Restrictions ).Kind );
		Assert.DoesNotContain( AdminAction.Unban, moderator.Actions );
	}

	[Fact]
	public async Task RevokedRestrictionsLeaveTheList()
	{
		using var rig = new ModerationRig();
		await rig.Run( "ban", DispatcherRig.TargetId, "1d", "Griefing" );
		await rig.Run( "unban", DispatcherRig.TargetId, "Appeal accepted" );
		Assert.Empty( (await Read( rig, new AdminQuery( AdminQueryKind.Restrictions ) )).Restrictions );
	}

	[Fact]
	public async Task PlayerRowsCountWarningsOnlyForHistoryReaders()
	{
		using var rig = new ModerationRig();
		await rig.Run( "warn", DispatcherRig.TargetId, "First" );
		await rig.Run( "warn", DispatcherRig.TargetId, "Second" );
		Assert.Equal( 2, Assert.Single( (await Read( rig, new AdminQuery( AdminQueryKind.Players ) )).Players ).Warnings );

		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		var moderator = await Read( rig, new AdminQuery( AdminQueryKind.Players ), DispatcherRig.User );
		Assert.Equal( 0, Assert.Single( moderator.Players ).Warnings );
	}

	[Fact]
	public async Task NotesAreRecordedForModeratorsAndShownInHistory()
	{
		using var rig = new ModerationRig();
		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		var note = await rig.Rig.Submit( "note", DispatcherRig.User, new[] { DispatcherRig.TargetId, "Seen", "near", "spawn" } );
		Assert.Equal( CommandStatus.Success, note.Status );
		var history = await Read( rig, new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ) );
		var row = Assert.Single( history.History );
		Assert.Equal( "Note", row.Kind );
		Assert.Equal( "Seen near spawn", row.Reason );
	}

	[Fact]
	public async Task CommandsListOnlyPermittedCommandsWithTheirArguments()
	{
		using var rig = new ModerationRig();
		var owner = await Read( rig, new AdminQuery( AdminQueryKind.Commands ) );
		var ban = Assert.Single( owner.Commands, command => command.Name == "ban" );
		Assert.Equal( new[] { "user", "duration", "reason" }, ban.Arguments.Select( argument => argument.Name ) );
		Assert.Equal( CommandArgumentKind.Player, ban.Arguments[ 0 ].Kind );

		var user = await Read( rig, new AdminQuery( AdminQueryKind.Commands ), DispatcherRig.User );
		Assert.DoesNotContain( user.Commands, command => command.Name == "ban" );
		Assert.Contains( user.Commands, command => command.Name == "help" );
	}

	[Fact]
	public async Task PermissionsAndServerStatusAreOwnerOnly()
	{
		using var rig = new ModerationRig();
		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.TargetId, "moderator" ) );

		var owner = await Read( rig, new AdminQuery( AdminQueryKind.Permissions ) );
		Assert.Equal( CommandStatus.Success, owner.Status );
		var moderators = Assert.Single( owner.Groups, group => group.Id == "moderator" );
		Assert.Contains( DispatcherRig.TargetId, moderators.Members );
		Assert.Contains( "admin.players.note", moderators.Allowed );
		Assert.Contains( "admin.players.ban", owner.PermissionKeys );
		Assert.Equal( "test", (await Read( rig, new AdminQuery( AdminQueryKind.About ) )).Data[ "mode" ] );

		var user = await Read( rig, new AdminQuery( AdminQueryKind.Permissions ), DispatcherRig.User );
		Assert.Equal( CommandStatus.Denied, user.Status );
		Assert.Empty( user.Groups );
		Assert.Empty( (await Read( rig, new AdminQuery( AdminQueryKind.About ), DispatcherRig.User )).Data );
	}

	[Fact]
	public void ActivitySearchAndCommandRequestsValidateInput()
	{
		Assert.Throws<ArgumentException>( () => new AdminQuery( "warn", "not-a-steam-id", null ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( "bad key!", "", null ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.Commands, "x" ) );
		var query = new AdminQuery( "warn", DispatcherRig.OwnerId, CommandStatus.Success, 2 );
		Assert.Equal( AdminQueryKind.ActivitySearch, query.Kind );
		Assert.Equal( 2, query.Page );

		Assert.Throws<ArgumentException>( () => new AdminCommandRequest( "warn", new[] { "line\nbreak" } ) );
		Assert.Throws<ArgumentException>( () => new AdminCommandRequest( "warn", Enumerable.Repeat( "x", 17 ) ) );
		Assert.Equal( 2, new AdminCommandRequest( "warn", new[] { DispatcherRig.TargetId, "reason" } ).Arguments.Count );
	}
}
