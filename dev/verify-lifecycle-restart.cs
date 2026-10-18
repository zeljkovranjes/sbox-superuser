if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Fresh actual dedicated process required." );
var proof = Json.Deserialize<System.Collections.Generic.Dictionary<string, string>>( FileSystem.Data.ReadAllText( "superuser-t17/scene-proof.json" ) );
foreach ( var key in new[] { new SboxSuperuser.StoreKey( "security", "policy" ), new SboxSuperuser.StoreKey( "moderation", "history" ) } )
{
	var stored = await SboxSuperuser.Superuser.GetStore().ReadAsync( key, 1, cancellationToken );
	if ( stored.Document?.PayloadJson != proof[ key.ModuleId ] ) throw new InvalidOperationException( "Restart lost or rewrote " + key.ModuleId );
}
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var runtime = typeof( SboxSuperuser.Superuser ).GetMethod( "FindAuthorityRuntime", flags ).Invoke( null, null );
var moderation = runtime.GetType().GetProperty( "Moderation", flags ).GetValue( runtime );
var kind = typeof( SboxSuperuser.Superuser ).Assembly.GetType( "SboxSuperuser.Core.Moderation.ModerationKind" );
foreach ( var restriction in new[] { "Ban", "TextMute" } )
{
	var result = (SboxSuperuser.CommandResult)moderation.GetType().GetMethod( "CheckRestriction", flags ).Invoke( moderation,
		new object[] { "76561198000000061", Enum.Parse( kind, restriction ) } );
	if ( result.Status != SboxSuperuser.CommandStatus.Denied ) throw new InvalidOperationException( "Restart lost active " + restriction );
}
if ( SboxSuperuser.Superuser.GetDiagnostics().BootstrapCount != 1 || SboxSuperuser.Superuser.GetCommandDescriptors().Any( command => command.ModuleId.StartsWith( "sample." ) ) )
	throw new InvalidOperationException( "Restart retained temporary modules or duplicate bootstrap." );
return new { Pass = true, Dedicated = true, OriginalPolicyAndExpiry = true, ActiveRestrictions = 2, SboxSuperuser.Superuser.GetDiagnostics().RuntimeId };
