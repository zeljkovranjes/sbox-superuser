namespace SboxSuperuser;

public sealed partial class PolicySnapshot
{
	/// <summary>Creates the initial player/moderator/admin graph; reserved owners are resolved separately by trusted host authority.</summary>
	public static PolicySnapshot CreateDefault( long revision = 0 )
	{
		PermissionEntry Allow( string key ) => new( key, PermissionEffect.Allow );
		IEnumerable<PermissionEntry> Players( params string[] commands ) => commands.Select( command => Allow( "admin.players." + command ) );
		return new PolicySnapshot( revision, new[]
		{
			new GroupPolicy( "player", permissions: new[]
			{
				Allow( "admin.help" ), Allow( "admin.results.own" ),
				Allow( "admin.chat.psay" ), Allow( "admin.chat.asay" ), Allow( "admin.chat.thetime" ), Allow( "admin.votes.map" ), Allow( "admin.props.share" ), Allow( "admin.props.cleanupown" )
			} ),
			new GroupPolicy( "moderator", new[] { "player" }, new[]
			{
				Allow( "admin.players.kick" ), Allow( "admin.players.warn" ),
				Allow( "admin.players.mute" ), Allow( "admin.players.unmute" ),
				Allow( "admin.players.gag" ), Allow( "admin.players.ungag" ), Allow( "admin.players.note" ),
				Allow( "admin.players.gimp" ), Allow( "admin.players.ungimp" ),
				Allow( "admin.chat.read" ), Allow( "admin.chat.asay.read" ),
				Allow( "admin.votes.vote" ), Allow( "admin.votes.kick" ), Allow( "admin.votes.ban" ),
				Allow( "admin.props.touchall" ), Allow( "admin.props.cleanup" )
			}.Concat( Players( "slay", "sslay", "slap", "whip", "freeze", "unfreeze", "blind", "unblind", "ignite", "unignite", "unigniteall",
				"jail", "unjail", "jailtp", "bring", "goto", "send", "teleport", "return", "spectate", "unspectate", "noclip" ) ).ToArray(), 10 ),
			new GroupPolicy( "admin", new[] { "moderator" }, new[]
			{
				Allow( "admin.players.ban" ), Allow( "admin.players.unban" ), Allow( "admin.players.history" ),
				Allow( "admin.settings.get" ), Allow( "admin.settings.set" ), Allow( "admin.settings.reset" ),
				Allow( "admin.prefixes.manage" ), Allow( "admin.messages.manage" ), Allow( "admin.chat.csay" ), Allow( "admin.chat.tsay" ),
				Allow( "admin.votes.mapforce" ), Allow( "admin.votes.stop" ), Allow( "admin.votes.veto" ), Allow( "admin.content.manage" ), Allow( "admin.props.cleanupall" ), Allow( "admin.alts.read" )
			}.Concat( Players( "maul", "strip", "ragdoll", "unragdoll", "god", "ungod", "hp", "armor", "cloak", "uncloak", "playsound", "map" ) ).ToArray(), 50 )
		} );
	}
}
