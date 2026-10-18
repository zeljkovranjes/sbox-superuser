// Run through the actual engine bridge. This consumes the exported library API;
// it does not register commands, create authority, or fake a successful store.
var aliases = new[] { "sample-alias" };
var descriptor = new SboxSuperuser.CommandDescriptor(
	"sample", "Contract verification", "sample.read", "sample",
	new[] { new SboxSuperuser.CommandArgumentDescriptor( "target", "Target",
		SboxSuperuser.CommandArgumentKind.Player,
		targetPolicy: SboxSuperuser.CommandTargetPolicy.AnyPlayer ) },
	aliases, SboxSuperuser.CommandExecutionPolicy.ReadOnly );
aliases[ 0 ] = "changed";
if ( descriptor.Aliases[ 0 ] != "sample-alias" )
{
	throw new InvalidOperationException( "Descriptor aliases were not snapshotted." );
}

var request = new SboxSuperuser.CommandRequest( descriptor.Name, new[] { "76561199542962865" }, Guid.NewGuid() );
var player = new SboxSuperuser.PlayerReference( request.Arguments[ 0 ] );
var duration = new SboxSuperuser.CommandDuration( TimeSpan.FromMinutes( 5 ) );
var module = new SboxSuperuser.ModuleDescriptor( "sample", "Sample metadata", 1,
	new[] { new SboxSuperuser.ModuleDependency( "security", 1 ) } );
var permission = new SboxSuperuser.PermissionEntry( "sample.read", SboxSuperuser.PermissionEffect.Deny );
var write = new SboxSuperuser.StoreWriteRequest( new SboxSuperuser.StoreKey( "sample", "state" ),
	new SboxSuperuser.VersionedStoreDocument( 1, 1, "{}" ), 0 );
var unknown = new SboxSuperuser.StoreWriteResult( SboxSuperuser.StoreWriteStatus.Unknown );
var result = new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Unsupported,
	"Foundation only; no runtime service is installed." );

if ( unknown.CommittedRevision.HasValue || duration.IsPermanent || write.ExpectedRevision != 0
	|| player.SteamId != request.Arguments[ 0 ] || module.Dependencies.Count != 1
	|| permission.Effect != SboxSuperuser.PermissionEffect.Deny
	|| result.Status != SboxSuperuser.CommandStatus.Unsupported )
{
	throw new InvalidOperationException( "Contract smoke check failed." );
}

try
{
	_ = new SboxSuperuser.StoreReadResult( SboxSuperuser.StoreReadStatus.Found );
	throw new InvalidOperationException( "Found must require an actual document." );
}
catch ( ArgumentException )
{
	// Expected structural rejection, not a storage operation.
}

Log.Info( "[Superuser T02] PASS: exported contracts constructed in s&box; no runtime systems invoked." );
return "T02 contract smoke passed.";
