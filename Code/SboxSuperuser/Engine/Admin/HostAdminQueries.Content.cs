namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private async Task<AdminReply> ReadContentBansAsync( Guid id, Func<string, bool> can )
	{
		if ( !can( Commands.AdministrationCommands.ContentPermission ) )
			return Failure( id, CommandStatus.Denied, $"Content bans need {Commands.AdministrationCommands.ContentPermission}." );
		if ( _content is null ) return Failure( id, CommandStatus.Unsupported, "Content bans aren't running." );
		var bans = _content.Bans.Bans.OrderBy( ban => Core.Content.ContentBanList.Kinds.ToList().IndexOf( ban.Kind ) )
			.ThenBy( ban => ban.Pattern, StringComparer.OrdinalIgnoreCase ).ToArray();
		var rows = new AdminContentBanRow[ bans.Length ];
		for ( var index = 0; index < bans.Length; index++ )
		{
			var ban = bans[ index ];
			var item = _contentCatalog is null ? null : await _contentCatalog.DescribeAsync( ban.Kind, ban.Pattern );
			rows[ index ] = new AdminContentBanRow { Kind = ban.Kind, Pattern = ban.Pattern, Reason = ban.Reason, Title = item?.Title ?? ban.Pattern, Icon = item?.Icon ?? "" };
		}
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = bans.Length == 0 ? "Nothing is banned." : $"{bans.Length} banned.", ContentBans = rows
		};
	}
}
