using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Audit;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminQueryTests
{
	private static HostAdminQueries Queries( ModerationRig rig, HostAuditService audit = null ) => new(
		rig.Rig.Policy, rig.Moderation, audit, () => rig.Rig.Players );

	private static Task<AdminReply> Read( HostAdminQueries queries, AdminQuery query, ActorIdentity actor = null )
		=> queries.QueryAsync( () => actor ?? DispatcherRig.Owner, query, Guid.NewGuid(), CancellationToken.None );

	[Fact]
	public async Task OrdinaryPlayerCannotReadDirectoryRolesOrHistory()
	{
		using var rig = new ModerationRig();
		foreach ( var query in new[] { new AdminQuery( AdminQueryKind.Players ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ) } )
		{
			var reply = await Read( Queries( rig ), query, DispatcherRig.User );
			Assert.Equal( CommandStatus.Denied, reply.Status );
			Assert.Empty( reply.Players );
			Assert.Empty( reply.Roles );
			Assert.Empty( reply.History );
			Assert.Empty( reply.Restrictions );
		}
	}

	[Fact]
	public async Task SearchKeepsDuplicateNamesDistinctAndPagesAreBounded()
	{
		using var rig = new ModerationRig();
		rig.Rig.Players.Clear();
		for ( var i = 0; i < 25; i++ ) rig.Rig.Players.Add( new CommandPlayer( (76561198000000100UL + (ulong)i).ToString(), "Same name" ) );
		var first = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Players, "SAME" ) );
		var next = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Players, "same", 1 ) );
		Assert.Equal( 20, first.Players.Length );
		Assert.True( first.HasMore );
		Assert.Equal( 5, next.Players.Length );
		Assert.False( next.HasMore );
		Assert.Equal( 25, first.Players.Concat( next.Players ).Select( row => row.SteamId ).Distinct().Count() );
	}

	[Fact]
	public async Task ModeratorSeesMuteButNotBanReasonOrHistory()
	{
		using var rig = new ModerationRig();
		await rig.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "moderator" ) );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "ban", DispatcherRig.TargetId, "permanent", "Private ban reason" )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "mute", DispatcherRig.TargetId, "1h", "Visible mute reason" )).Status );
		var reply = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ), DispatcherRig.User );
		Assert.Equal( CommandStatus.Success, reply.Status );
		Assert.False( reply.HistoryAllowed );
		Assert.Empty( reply.History );
		Assert.Equal( "Visible mute reason", Assert.Single( reply.Restrictions ).Reason );
		Assert.DoesNotContain( AdminAction.Ban, reply.Actions );
		Assert.DoesNotContain( AdminAction.Unban, reply.Actions );
	}

	[Fact]
	public async Task OfflineTargetCannotBeKickedAndOwnerCannotBePunished()
	{
		using var rig = new ModerationRig();
		rig.Rig.Players.Clear();
		var offline = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ) );
		Assert.False( offline.Online );
		Assert.DoesNotContain( AdminAction.Kick, offline.Actions );
		Assert.Contains( AdminAction.Ban, offline.Actions );
		var owner = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.OwnerId ) );
		Assert.Contains( "owner", owner.Roles );
		Assert.DoesNotContain( AdminAction.Warn, owner.Actions );
		Assert.DoesNotContain( AdminAction.Ban, owner.Actions );
	}

	[Fact]
	public async Task ActivityFiltersOtherActorsWithoutReturningArguments()
	{
		using var rig = new ModerationRig();
		var audit = new HostAuditService( new AuditStoreHarness(), () => true, rig.Rig.Errors.Add );
		await audit.RecordAsync( new AuditAction( DispatcherRig.Owner, CommandOrigin.Interface, Guid.NewGuid(), "warn", DateTimeOffset.UtcNow ), CommandStatus.Success, default );
		await audit.RecordAsync( new AuditAction( DispatcherRig.User, CommandOrigin.Interface, Guid.NewGuid(), "ban", DateTimeOffset.UtcNow ), CommandStatus.Denied, default );
		var own = await Read( Queries( rig, audit ), new AdminQuery( AdminQueryKind.Activity ), DispatcherRig.User );
		Assert.Equal( DispatcherRig.UserId, Assert.Single( own.Activity ).Actor );
		var all = await Read( Queries( rig, audit ), new AdminQuery( AdminQueryKind.Activity ) );
		Assert.Equal( 2, all.Activity.Length );
	}

	[Fact]
	public async Task InterfaceActionRechecksPermissionsAndPersistsOrigin()
	{
		using var rig = new ModerationRig();
		var request = new AdminActionRequest( AdminAction.Warn, DispatcherRig.TargetId, "Interface warning" );
		var denied = await rig.Rig.Dispatcher.SubmitAsync( () => DispatcherRig.User, CommandOrigin.Interface, request.ToCommand( Guid.NewGuid() ), default );
		Assert.Equal( CommandStatus.Denied, denied.Status );
		Assert.Equal( 0, rig.Rig.Store.WriteCount );
		var success = await rig.Rig.Dispatcher.SubmitAsync( () => DispatcherRig.Owner, CommandOrigin.Interface, request.ToCommand( Guid.NewGuid() ), default );
		Assert.Equal( CommandStatus.Success, success.Status );
		Assert.Contains( "Interface", rig.Rig.Store.Documents[ "moderation/history" ].PayloadJson );
		var history = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ) );
		Assert.Equal( "Interface warning", Assert.Single( history.History ).Reason );
	}

	[Fact]
	public async Task LostAuthenticationAndDegradedStorageReturnNoPrivateData()
	{
		using var rig = new ModerationRig();
		var calls = 0;
		var reply = await Queries( rig ).QueryAsync( () => ++calls == 1 ? DispatcherRig.Owner : null,
			new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ), Guid.NewGuid(), default );
		Assert.Equal( CommandStatus.Denied, reply.Status );
		rig.Rig.Active = false;
		reply = await Read( Queries( rig ), new AdminQuery( AdminQueryKind.Player, DispatcherRig.TargetId ) );
		Assert.Equal( CommandStatus.StorageUnavailable, reply.Status );
		Assert.Empty( reply.History );
		Assert.Empty( reply.Roles );
	}

	[Fact]
	public void InputCannotSelectByNameInjectCommandsOrOmitRequiredReason()
	{
		Assert.Throws<ArgumentException>( () => new AdminActionRequest( AdminAction.Kick, "Same name", "reason" ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => new AdminActionRequest( (AdminAction)99, DispatcherRig.TargetId, "reason" ) );
		Assert.Throws<ArgumentException>( () => new AdminActionRequest( AdminAction.Ban, DispatcherRig.TargetId, "reason" ) );
		Assert.Throws<ArgumentException>( () => new AdminActionRequest( AdminAction.Warn, DispatcherRig.TargetId, "\nadmin ban" ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.Activity, DispatcherRig.OwnerId ) );
		Assert.Throws<ArgumentException>( () => new AdminQuery( AdminQueryKind.Players, new string( 'x', 257 ) ) );
	}
}
