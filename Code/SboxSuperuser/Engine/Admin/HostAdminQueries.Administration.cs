namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private const string SecurityPermission = "admin.security.manage";

	private bool IsOwner( ActorIdentity actor ) => actor.Kind == ActorKind.Player && _policy.IsOwner( actor.SubjectId );

	// Module IDs and states match "admin modules list", and the status rows match "admin diagnostics"; both are owner-only.
	private AdminReply ReadAbout( Guid id, ActorIdentity actor )
	{
		var owner = IsOwner( actor );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "About Superuser.", ModulesAllowed = owner,
			Modules = owner
				? _modules().Select( status => new AdminModuleRow
				{
					Id = status.Descriptor.Id, ApiVersion = status.Descriptor.ApiVersion,
					State = status.State.ToString(), Message = status.Message
				} ).OrderBy( row => row.Id, StringComparer.Ordinal ).ToArray()
				: Array.Empty<AdminModuleRow>(),
			Data = owner ? _diagnostics().ToDictionary( pair => pair.Key, pair => pair.Value ) : new Dictionary<string, string>()
		};
	}

	// Same visibility as "admin commands": permitted commands, with security management limited to owners.
	private AdminReply ReadCommands( Guid id, ActorIdentity actor, Func<string, bool> can )
	{
		var owner = IsOwner( actor );
		var rows = _commands()
			.Where( command => can( command.Permission ) && (command.Permission != SecurityPermission || owner) )
			.Select( command => new AdminCommandRow
			{
				Name = command.Name, Description = command.Description, ModuleId = command.ModuleId,
				Mutation = command.ExecutionPolicy == CommandExecutionPolicy.Mutation,
				Arguments = command.Arguments.Select( argument => new AdminCommandArgumentRow
				{
					Name = argument.Name, Description = argument.Description, Kind = argument.Kind,
					Optional = argument.IsOptional, Remainder = argument.IsRemainder,
					Minimum = argument.Minimum, Maximum = argument.Maximum, Choices = argument.Choices.ToArray()
				} ).ToArray()
			} ).ToArray();
		return new AdminReply { RequestId = id, Status = CommandStatus.Success, Message = "Permitted commands.", Commands = rows };
	}

	private const string ChatPermission = "admin.chat.read";

	// Chat lines come from memory only and were already public; reading them still needs its own permission.
	private AdminReply ReadChat( Guid id, AdminQuery query, Func<string, bool> can )
	{
		if ( !can( ChatPermission ) ) return Failure( id, CommandStatus.Denied, "Reading recent chat requires admin.chat.read." );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Recent chat.",
			Chat = _chat.Read( query.Search ).Select( line => new AdminChatRow
			{
				SteamId = line.SteamId, Name = line.Name, Message = line.Message, CreatedUtc = line.CreatedUtc
			} ).ToArray()
		};
	}

	// Policy edits are owner-only commands, so only owners see the policy they would edit.
	private AdminReply ReadPermissions( Guid id, ActorIdentity actor )
	{
		var policy = IsOwner( actor ) ? _policy.ReadPolicy( actor ) : null;
		if ( policy is null ) return Failure( id, CommandStatus.Denied, "Only owners can view and edit permissions." );

		static string[] With( IReadOnlyDictionary<string, int> entries, PermissionEffect effect ) => entries
			.Where( entry => entry.Value == (int)effect ).Select( entry => entry.Key ).Order( StringComparer.Ordinal ).ToArray();

		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Permission policy.",
			Groups = policy.Groups.Values.OrderBy( group => group.Id, StringComparer.Ordinal ).Select( group => new AdminGroupRow
			{
				Id = group.Id, Parents = group.Parents.ToArray(), Protection = group.ProtectionLevel,
				Allowed = With( group.Entries, PermissionEffect.Allow ), Denied = With( group.Entries, PermissionEffect.Deny ),
				Members = policy.Users.Values.Where( user => user.Groups.Contains( group.Id ) )
					.Select( user => user.SteamId ).Order( StringComparer.Ordinal ).ToArray()
			} ).ToArray(),
			Users = policy.Users.Values.OrderBy( user => user.SteamId, StringComparer.Ordinal ).Select( user => new AdminUserRow
			{
				SteamId = user.SteamId, Groups = user.Groups.ToArray(),
				Allowed = With( user.Entries, PermissionEffect.Allow ), Denied = With( user.Entries, PermissionEffect.Deny )
			} ).ToArray(),
			PermissionKeys = _policy.RegisteredPermissions.Order( StringComparer.Ordinal ).ToArray()
		};
	}
}
