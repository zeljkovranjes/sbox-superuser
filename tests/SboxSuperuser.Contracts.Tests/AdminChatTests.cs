using SboxSuperuser.Core.Chat;
using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminChatTests
{
	private static Task<AdminReply> Read( ModerationRig rig, RecentChat chat, AdminQuery query, ActorIdentity actor )
		=> new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, chat: chat )
			.QueryAsync( () => actor, query, Guid.NewGuid(), CancellationToken.None );

	[Fact]
	public async Task ModeratorsReadRecentChatAndPlayersDoNot()
	{
		using var rig = new ModerationRig();
		var chat = new RecentChat();
		chat.Record( DispatcherRig.TargetId, "Target", "first", DateTimeOffset.UtcNow );
		chat.Record( DispatcherRig.UserId, "User", "second", DateTimeOffset.UtcNow );

		var denied = await Read( rig, chat, new AdminQuery( AdminQueryKind.Chat ), DispatcherRig.User );
		Assert.Equal( CommandStatus.Denied, denied.Status );
		Assert.Empty( denied.Chat );

		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		var all = await Read( rig, chat, new AdminQuery( AdminQueryKind.Chat ), DispatcherRig.User );
		Assert.Equal( new[] { "second", "first" }, all.Chat.Select( line => line.Message ) );

		var one = await Read( rig, chat, new AdminQuery( AdminQueryKind.Chat, DispatcherRig.TargetId ), DispatcherRig.User );
		Assert.Equal( "first", Assert.Single( one.Chat ).Message );
	}

	[Fact]
	public void ChatQueriesTakeOnlyAnExactSteamId()
	{
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.Chat, "name" ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.Chat, "", 1 ) );
	}
}
