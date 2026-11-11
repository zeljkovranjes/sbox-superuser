using SboxSuperuser.Core.Content;
using SboxSuperuser.Engine.Content;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	internal const string ContentPermission = "admin.content.manage";

	private void RegisterContent( HostContentService content )
	{
		var kinds = string.Join( ", ", ContentBanList.Kinds );
		Register( "content.ban", $"Ban a map, addon or entity ({kinds}); * matches anything, as in author.*", ContentPermission,
			new[] { Kind(), Pattern(), new CommandArgumentDescriptor( "reason", "Why, shown when it is refused", CommandArgumentKind.String, isOptional: true, isRemainder: true ) },
			(context, arguments) => content.BanAsync( context, arguments.Get<string>( "kind" ), arguments.Get<string>( "pattern" ),
				arguments.Contains( "reason" ) ? arguments.Get<string>( "reason" ) : "" ), mutation: true );
		Register( "content.unban", "Allow a banned map, addon or entity again", ContentPermission, new[] { Kind(), Pattern() },
			(context, arguments) => content.UnbanAsync( context, arguments.Get<string>( "kind" ), arguments.Get<string>( "pattern" ) ), mutation: true );
		Register( "content.list", "List banned maps, addons and entities", ContentPermission, new[] { Page() },
			(_, arguments) => Task.FromResult( Rows( content.Bans.Bans.Select( ban => $"{ban.Kind} {ban.Pattern}{(ban.Reason.Length > 0 ? " — " + ban.Reason : "")}" ),
				PageNumber( arguments ), content.Bans.Bans.Count == 0 ? "Nothing is banned." : "Banned content." ) ) );
	}

	private static CommandArgumentDescriptor Kind() => new( "kind", "map, addon or entity", CommandArgumentKind.String, audit: CommandArgumentAudit.Redacted );

	private static CommandArgumentDescriptor Pattern() => new( "pattern", "Map or package ident, prefab or model path, or component type; * matches anything",
		CommandArgumentKind.String );
}
