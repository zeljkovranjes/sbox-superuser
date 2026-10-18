if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual restarted dedicated fixture required." );
var stored = await SboxSuperuser.Superuser.GetStore().ReadAsync( new SboxSuperuser.StoreKey( "moderation", "history" ), 1, cancellationToken );
if ( stored.Status != SboxSuperuser.StoreReadStatus.Found || stored.Document.PayloadJson != FileSystem.Data.ReadAllText( "superuser-t16/restart-proof.json" ) )
	throw new InvalidOperationException( "Restart changed IDs, original UTC expiry, history or intent payload." );
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var moderation = runtime.GetType().GetProperty( "Moderation", flags ).GetValue( runtime );
var kindType = typeof( SboxSuperuser.Superuser ).Assembly.GetType( "SboxSuperuser.Core.Moderation.ModerationKind" );
foreach ( var restriction in new[] { ("76561198000000050", "Ban"), ("76561198000000051", "Ban"), ("76561198000000050", "TextMute") } )
{
	var result = (SboxSuperuser.CommandResult)moderation.GetType().GetMethod( "CheckRestriction", flags ).Invoke( moderation,
		new object[] { restriction.Item1, Enum.Parse( kindType, restriction.Item2 ) } );
	if ( result.Status != SboxSuperuser.CommandStatus.Denied ) throw new InvalidOperationException( "Restriction was lost across restart." );
}
return new { Pass = true, Dedicated = true, stored.Document.Revision, OriginalUtcExpiryPreserved = true, ActiveRestrictions = 3 };
