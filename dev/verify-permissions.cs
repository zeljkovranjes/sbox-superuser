// Trusted development script. Exercises the actual library assembly through the engine whitelist.
const string subject = "76561198000000001";
const string permission = "test.permission";
void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}

SboxSuperuser.PolicySnapshot Policy( long revision, SboxSuperuser.PermissionEffect direct ) => new( revision, new[]
{
	new SboxSuperuser.GroupPolicy( "player" ),
	new SboxSuperuser.GroupPolicy( "allow", permissions: new[] { new SboxSuperuser.PermissionEntry( permission, SboxSuperuser.PermissionEffect.Allow ) } ),
	new SboxSuperuser.GroupPolicy( "deny", permissions: new[] { new SboxSuperuser.PermissionEntry( permission, SboxSuperuser.PermissionEffect.Deny ) } ),
	new SboxSuperuser.GroupPolicy( "staff", new[] { "deny", "allow" } )
}, new[] { new SboxSuperuser.UserPolicy( subject, new[] { "staff" }, new[] { new SboxSuperuser.PermissionEntry( permission, direct ) } ) } );
var registered = new[] { new SboxSuperuser.PermissionDescriptor( permission, "Test permission" ) };
var resolver = new SboxSuperuser.PermissionResolver( Policy( 1, SboxSuperuser.PermissionEffect.Allow ), registered );
Require( resolver.Explain( subject, permission ).IsAllowed, "Direct allow did not override inherited deny." );
resolver.ReplacePolicy( Policy( 2, SboxSuperuser.PermissionEffect.Unset ) );
var denied = resolver.Explain( subject, permission );
Require( !denied.IsAllowed && denied.PolicyRevision == 2 && denied.Groups.Count == 2, "Cache or inherited denial failed." );
Require( denied.Groups[ 0 ].GroupId == "allow" && denied.Groups[ 1 ].GroupId == "deny", "Explanation order was unstable." );
resolver.ReplacePolicy( Policy( 3, SboxSuperuser.PermissionEffect.Allow ) );
resolver.ReplaceRegisteredPermissions( Array.Empty<SboxSuperuser.PermissionDescriptor>() );
Require( !resolver.Explain( subject, permission ).IsAllowed && !resolver.Explain( subject, permission ).IsRegistered, "Unknown permission was granted." );
resolver.ReplaceRegisteredPermissions( registered );
Require( resolver.Explain( subject, permission ).IsAllowed, "Registry update left a stale denial." );
var cycleRefused = false;
try
{
	_ = new SboxSuperuser.PolicySnapshot( 4, new[] { new SboxSuperuser.GroupPolicy( "player", new[] { "staff" } ), new SboxSuperuser.GroupPolicy( "staff", new[] { "player" } ) } );
}
catch ( ArgumentException )
{
	cycleRefused = true;
}
Require( cycleRefused && resolver.PolicyRevision == 3, "Invalid cycle replaced existing policy." );
Log.Info( "[T05] permission resolver PASS: direct precedence, inherited deny, policy/registry invalidation, unknown denial, cycle rejection" );
return new { Pass = true, PolicyRevision = resolver.PolicyRevision, RegistryRevision = resolver.RegistryRevision };
