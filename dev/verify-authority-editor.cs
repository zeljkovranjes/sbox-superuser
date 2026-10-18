// Trusted test for the isolated editor host. Requires no existing security/policy or security/owners store.
if ( !Game.IsEditor || !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Editor host required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var oldSwitch = bootstrap.AllowEditorTesting;
const string ownerFile = "superuser/editor-owners.json";
var original = FileSystem.Data.FileExists( ownerFile ) ? FileSystem.Data.ReadAllText( ownerFile ) : null;
var actorId = Connection.Local.SteamId.ToString();
const string targetId = "76561198000000003";
var token = System.Threading.CancellationToken.None;
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
void Restart()
{
	bootstrap.Enabled = false;
	bootstrap.Enabled = true;
}
try
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	FileSystem.Data.CreateDirectory( "superuser" );
	FileSystem.Data.WriteAllText( ownerFile, "{\"schemaVersion\":1,\"owners\":[\"" + actorId + "\"]}" );
	bootstrap.Enabled = true;
	var store = SboxSuperuser.Superuser.GetStore();
	var key = new SboxSuperuser.StoreKey( "security", "policy" );
	Require( (await store.ReadAsync( key, 1, token )).Status == SboxSuperuser.StoreReadStatus.Missing, "Refuse to overwrite an existing test policy." );
	Require( SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Configured owner not recognized." );
	var beforeOwner = await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, actorId, "admin.players.ban" );
	Require( beforeOwner.IsAllowed && beforeOwner.OwnerBypass, "Owner bypass missing from explain." );
	var change = await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local,
		SboxSuperuser.PolicyChange.AddUserGroup( actorId, "admin" ), SboxSuperuser.CommandOrigin.Console );
	Require( change.Status == SboxSuperuser.CommandStatus.Success, "Owner policy change failed." );
	change = await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local,
		SboxSuperuser.PolicyChange.AddUserGroup( targetId, "admin" ), SboxSuperuser.CommandOrigin.Console );
	Require( change.Status == SboxSuperuser.CommandStatus.Success, "Offline group assignment failed." );
	change = await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local,
		SboxSuperuser.PolicyChange.SetUserPermission( actorId, "admin.security.manage", SboxSuperuser.PermissionEffect.Allow ), SboxSuperuser.CommandOrigin.Console );
	Require( change.Status == SboxSuperuser.CommandStatus.Success, "Direct entry change failed." );
	Require( !(await SboxSuperuser.Superuser.CheckPunitiveTargetAsync( Connection.Local, actorId )).IsAllowed, "Owner self-targeting allowed." );
	Require( (await SboxSuperuser.Superuser.CheckPunitiveTargetAsync( Connection.Local, targetId )).IsAllowed, "Owner rank did not exceed offline admin." );
	var ownersBefore = await store.ReadAsync( new SboxSuperuser.StoreKey( "security", "owners" ), 1, token );
	ConsoleSystem.Run( "superuser_owner_recover", "clear" );
	Require( (await store.ReadAsync( new SboxSuperuser.StoreKey( "security", "owners" ), 1, token )).Status == ownersBefore.Status, "Editor local owner recovery should refuse." );
	FileSystem.Data.WriteAllText( ownerFile, "{\"schemaVersion\":1,\"owners\":[]}" );
	Restart();
	Require( !SboxSuperuser.Superuser.IsOwner( Connection.Local ), "Removing bootstrap ownership did not revoke it." );
	var admin = await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, actorId, "admin.security.manage" );
	Require( admin.IsAllowed && !admin.OwnerBypass, "Expected ordinary admin with explicit security entry." );
	change = await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( Connection.Local,
		SboxSuperuser.PolicyChange.CreateGroup( "escalation" ), SboxSuperuser.CommandOrigin.Console );
	Require( change.Status == SboxSuperuser.CommandStatus.Denied, "Ordinary admin escalated through policy API." );
	Require( (await SboxSuperuser.Superuser.ApplyPolicyChangeAsync( null,
		SboxSuperuser.PolicyChange.CreateGroup( "nullroot" ), SboxSuperuser.CommandOrigin.Console )).Status == SboxSuperuser.CommandStatus.Denied, "Null caller gained root." );
	var protectedTarget = await SboxSuperuser.Superuser.CheckPunitiveTargetAsync( Connection.Local, targetId );
	Require( !protectedTarget.IsAllowed && protectedTarget.ActorLevel == 50 && protectedTarget.TargetLevel == 50, "Equal offline rank was punishable." );
	Require( await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, targetId, "admin.help" ) is null, "Other-user explain bypassed its permission." );
	Require( SboxSuperuser.Superuser.GetAuthorityDiagnostics().PolicyRevision == 3, "Denied operations changed policy revision." );
	var activeStore = SboxSuperuser.Superuser.GetStore();
	var state = "{\"policySchema\":1,\"groups\":[{\"id\":\"player\",\"parents\":[],\"permissions\":{},\"protectionLevel\":0}],\"users\":[]}";
	var intent = new SboxSuperuser.AuditIntent( Guid.NewGuid(), Guid.NewGuid(), SboxSuperuser.ActorKind.InternalService,
		"test.authority", SboxSuperuser.CommandOrigin.InternalService, "test.recovery", DateTimeOffset.UtcNow );
	var recovery = new SboxSuperuser.PolicyTransaction( state, new[] { intent } );
	Require( (await activeStore.WriteAsync( recovery.CreateWriteRequest( key, 3 ), token )).Status == SboxSuperuser.StoreWriteStatus.Committed, "Trusted recovery write failed." );
	Require( !(await SboxSuperuser.Superuser.ExplainPermissionAsync( Connection.Local, actorId, "admin.security.manage" )).IsAllowed,
		"Storage change left cached authorization active." );
	Log.Info( "[T06] editor authority PASS: owner-only policy, persistence/reload, ordinary-admin denial, null refusal, offline equal-rank denial, protected explain, editor recovery refusal, store invalidation" );
	return SboxSuperuser.Superuser.GetAuthorityDiagnostics();
}
finally
{
	bootstrap.Enabled = false;
	if ( original is null ) FileSystem.Data.DeleteFile( ownerFile );
	else FileSystem.Data.WriteAllText( ownerFile, original );
	bootstrap.AllowEditorTesting = oldSwitch;
	bootstrap.Enabled = true;
}
