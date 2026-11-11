namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private const int MaximumPropOwners = 100;

	private AdminReply ReadProps( Guid id, Func<string, bool> can )
	{
		if ( !can( Props.PropCommands.CleanupPermission ) ) return Failure( id, CommandStatus.Denied, $"Prop protection needs {Props.PropCommands.CleanupPermission}." );
		if ( _props is null || _propCleanup is null ) return Failure( id, CommandStatus.Unsupported, "Prop protection isn't running." );
		var online = (_players() ?? Array.Empty<CommandPlayer>()).ToDictionary( player => player.SteamId, player => player.DisplayName, StringComparer.Ordinal );
		var delay = _propCleanup.Current.Delay;
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = Props.HostPropCleanup.Describe( delay ),
			PropCleanupSeconds = delay is { } value ? (long)value.TotalSeconds : null,
			PropOwners = _props.Owners().Take( MaximumPropOwners ).Select( owner => new AdminPropOwnerRow
			{
				SteamId = owner.SteamId, Name = online.GetValueOrDefault( owner.SteamId, owner.SteamId ), Online = online.ContainsKey( owner.SteamId ), Props = owner.Count
			} ).ToArray()
		};
	}
}
