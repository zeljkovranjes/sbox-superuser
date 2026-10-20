namespace SboxSuperuser;

public sealed partial class PolicySnapshot
{
	/// <summary>Creates the initial player/moderator/admin graph; reserved owners are resolved separately by trusted host authority.</summary>
	public static PolicySnapshot CreateDefault( long revision = 0 )
	{
		PermissionEntry Allow( string key ) => new( key, PermissionEffect.Allow );
		return new PolicySnapshot( revision, new[]
		{
			new GroupPolicy( "player", permissions: new[] { Allow( "admin.help" ), Allow( "admin.results.own" ) } ),
			new GroupPolicy( "moderator", new[] { "player" }, new[]
			{
				Allow( "admin.players.kick" ), Allow( "admin.players.warn" ),
				Allow( "admin.players.mute" ), Allow( "admin.players.unmute" ), Allow( "admin.players.note" ),
				Allow( "admin.chat.read" )
			}, 10 ),
			new GroupPolicy( "admin", new[] { "moderator" }, new[]
			{
				Allow( "admin.players.ban" ), Allow( "admin.players.unban" ), Allow( "admin.players.history" ),
				Allow( "admin.settings.get" ), Allow( "admin.settings.set" ), Allow( "admin.settings.reset" ),
				Allow( "admin.prefixes.manage" )
			}, 50 )
		} );
	}
}
