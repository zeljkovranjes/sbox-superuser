if ( !Game.IsEditor || !Networking.IsHost || !SboxSuperuser.Superuser.IsOwner( Connection.Local ) ) throw new InvalidOperationException( "Isolated editor owner required." );
const string actorId = "90071996842377217";
const string equalId = "76561198000000047";
var actor = Connection.All.Single( connection => connection.SteamId.ToString() == actorId );
var release = new TaskCompletionSource();
SboxSuperuser.CommandRegistrationScope scope = null;
Task<SboxSuperuser.CommandResult> held = null;
Task<SboxSuperuser.CommandResult> queued = null;
async Task Edit( SboxSuperuser.PolicyChange change )
{
	var result = await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local, change, SboxSuperuser.CommandOrigin.Console, cancellationToken );
	if ( result.Status is not (SboxSuperuser.CommandStatus.Success or SboxSuperuser.CommandStatus.NoChange) ) throw new InvalidOperationException( "Policy fixture edit failed." );
}
try
{
	await Edit( SboxSuperuser.PolicyChange.AddUserGroup( actorId, "moderator" ) );
	await Edit( SboxSuperuser.PolicyChange.AddUserGroup( equalId, "moderator" ) );
	var permission = await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, actorId, "admin.players.warn" );
	if ( !permission.IsAllowed ) throw new InvalidOperationException( "Moderator fixture lacks warning permission." );
	var key = new SboxSuperuser.StoreKey( "moderation", "history" );
	var before = await SboxSuperuser.Superuser.GetStore().ReadAsync( key, 1, cancellationToken );
	var equal = await SboxSuperuser.Superuser.ExecuteCommandAsync( actor,
		new SboxSuperuser.CommandRequest( "warn", new[] { equalId, "Equal-rank refusal" }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
	if ( equal.Status != SboxSuperuser.CommandStatus.Denied ) throw new InvalidOperationException( "Equal-rank punishment was accepted." );
	scope = SboxSuperuser.Superuser.BeginCommandRegistration( "test.moderationqueue" );
	scope.RegisterCommand( new SboxSuperuser.CommandDescriptor( "test.moderationhold", "Hold the isolated request queue", "admin.help", scope.ModuleId ),
		async (_, _) => { await release.Task; return new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Success, "Released" ); } );
	scope.Commit();
	held = SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local,
		new SboxSuperuser.CommandRequest( "test.moderationhold", Array.Empty<string>(), Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
	queued = SboxSuperuser.Superuser.ExecuteCommandAsync( actor,
		new SboxSuperuser.CommandRequest( "warn", new[] { "90071996842377218", "Revoked queued warning" }, Guid.NewGuid() ), SboxSuperuser.CommandOrigin.Console, cancellationToken );
	if ( queued.IsCompleted ) throw new InvalidOperationException( "Warning did not queue behind the hold." );
	await Edit( SboxSuperuser.PolicyChange.SetUserPermission( actorId, "admin.players.warn", SboxSuperuser.PermissionEffect.Deny ) );
	release.TrySetResult();
	await held;
	var rejected = await queued;
	if ( rejected.Status != SboxSuperuser.CommandStatus.Denied
		|| (await SboxSuperuser.Superuser.GetStore().ReadAsync( key, 1, cancellationToken )).Document.Revision != before.Document.Revision )
		throw new InvalidOperationException( "Revoked queued authority changed moderation state." );
	return new { Pass = true, EqualRank = equal.Status, RevokedQueuedRequest = rejected.Status, ModerationRevision = before.Document.Revision };
}
finally
{
	release.TrySetResult();
	if ( held is not null ) await held;
	if ( queued is not null ) await queued;
	scope?.Dispose();
	await Edit( SboxSuperuser.PolicyChange.SetUserPermission( actorId, "admin.players.warn", SboxSuperuser.PermissionEffect.Unset ) );
	await Edit( SboxSuperuser.PolicyChange.RemoveUserGroup( actorId, "moderator" ) );
	await Edit( SboxSuperuser.PolicyChange.RemoveUserGroup( equalId, "moderator" ) );
}
