namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	private void RegisterAccess()
	{
		Edit( "group.create", "Create a group. Example: admin group create helpers", new[] { Text( "group", "New group key" ) },
			arguments => PolicyChange.CreateGroup( arguments.Get<string>( "group" ) ) );
		Edit( "group.delete", "Delete an unreferenced group. Example: admin group delete helpers", new[] { Text( "group", "Existing group key" ) },
			arguments => PolicyChange.DeleteGroup( arguments.Get<string>( "group" ) ) );
		Edit( "group.parents.set", "Replace group parents. Example: admin group helpers parents set player", new[]
		{
			Text( "group", "Group key" ), Text( "parents", "Comma-separated parent keys; an empty quoted value clears parents" )
		}, arguments => PolicyChange.SetParents( arguments.Get<string>( "group" ), arguments.Get<string>( "parents" ).Length == 0
			? Array.Empty<string>() : arguments.Get<string>( "parents" ).Split( ',' ) ) );
		Edit( "group.protection.set", "Set target protection. Example: admin group helpers protection set 5", new[]
		{
			Text( "group", "Group key" ), new CommandArgumentDescriptor( "level", "Nonnegative protection level", CommandArgumentKind.Integer,
				minimum: 0, maximum: int.MaxValue, audit: CommandArgumentAudit.SafeValue )
		}, arguments => PolicyChange.SetGroupProtection( arguments.Get<string>( "group" ), (int)arguments.Get<long>( "level" ) ) );
		foreach ( var operation in new[] { "add", "remove" } )
		{
			var add = operation == "add";
			Edit( "user.group." + operation, $"{operation} user group. Example: admin user <steamid> group {operation} moderator",
				new[] { Player(), Text( "group", "Group key; owner is reserved" ) }, arguments => add
					? PolicyChange.AddUserGroup( arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "group" ) )
					: PolicyChange.RemoveUserGroup( arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "group" ) ) );
		}
		foreach ( var effect in new[] { PermissionEffect.Allow, PermissionEffect.Deny, PermissionEffect.Unset } )
		{
			var operation = effect.ToString().ToLowerInvariant();
			Edit( "group.permission." + operation, $"{operation} a group permission. Example: admin group moderator permission {operation} admin.help",
				new[] { Text( "group", "Group key" ), Text( "permission", "Exact lowercase permission key" ) }, arguments =>
					PolicyChange.SetGroupPermission( arguments.Get<string>( "group" ), arguments.Get<string>( "permission" ), effect ) );
			Edit( "user.permission." + operation, $"{operation} a direct user permission. Example: admin user <steamid> permission {operation} admin.help",
				new[] { Player(), Text( "permission", "Exact lowercase permission key" ) }, arguments =>
					PolicyChange.SetUserPermission( arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "permission" ), effect ) );
		}

		Register( "group.list", "List groups, parents and protection. Example: admin group list", "admin.security.manage", new[] { Page() },
			( context, arguments ) => Task.FromResult( GroupList( context, arguments ) ) );
		Register( "group.parents.list", "List a group's parents. Example: admin group moderator parents list", "admin.security.manage",
			new[] { Text( "group", "Group key" ), Page() }, ( context, arguments ) => Task.FromResult( ParentList( context, arguments ) ) );
		Register( "user.group.list", "List direct and implicit user groups. Example: admin user <steamid> group list", "admin.security.manage",
			new[] { Player(), Page() }, ( context, arguments ) => Task.FromResult( UserGroups( context, arguments ) ) );
		Register( "permissions.explain", "Explain a permission; others require admin.permissions.explain. Example: admin permissions explain <steamid> admin.help",
			"admin.help", new[] { Player(), Text( "permission", "Exact lowercase permission key" ), Page() }, ExplainAsync );
	}

	private void Edit( string name, string description, CommandArgumentDescriptor[] arguments, Func<CommandArguments, PolicyChange> create )
	{
		Register( name, description, "admin.security.manage", arguments, async ( context, parsed ) =>
		{
			if ( !IsOwner( context ) )
			{
				return Denied();
			}

			try
			{
				return await _policy.ApplyAsync( () => Authenticate( context ), create( parsed ), context.Origin, context.RequestId, context.Cancellation );
			}
			catch ( ArgumentException )
			{
				return new CommandResult( CommandStatus.InvalidArguments, "Invalid policy key or value; owner membership is reserved for dedicated-local recovery." );
			}
		}, mutation: true );
	}

	private CommandResult GroupList( CommandContext context, CommandArguments arguments )
	{
		var policy = _policy.ReadPolicy( Authenticate( context ) );
		return policy is null ? Denied() : Rows( policy.Groups.Values.OrderBy( group => group.Id ).Select( group =>
			$"{group.Id}; parents={string.Join( ",", group.Parents )}; protection={group.ProtectionLevel}; entries={group.Entries.Count}" ),
			PageNumber( arguments ), "Stored groups; owner membership is configured separately." );
	}

	private CommandResult ParentList( CommandContext context, CommandArguments arguments )
	{
		var policy = _policy.ReadPolicy( Authenticate( context ) );
		if ( policy is null )
		{
			return Denied();
		}
		return policy.Groups.TryGetValue( arguments.Get<string>( "group" ), out var group )
			? Rows( group.Parents, PageNumber( arguments ), "Direct parent groups." ) : new CommandResult( CommandStatus.NotFound, "Group not found." );
	}

	private CommandResult UserGroups( CommandContext context, CommandArguments arguments )
	{
		var policy = _policy.ReadPolicy( Authenticate( context ) );
		if ( policy is null )
		{
			return Denied();
		}
		policy.Users.TryGetValue( arguments.Get<PlayerReference>( "user" ).SteamId, out var user );
		return Rows( new[] { "player (implicit)" }.Concat( user?.Groups ?? Array.Empty<string>() ), PageNumber( arguments ), "User group membership." );
	}

	private async Task<CommandResult> ExplainAsync( CommandContext context, CommandArguments arguments )
	{
		try
		{
			var result = await _policy.ExplainForAsync( () => Authenticate( context ), arguments.Get<PlayerReference>( "user" ).SteamId,
				arguments.Get<string>( "permission" ) );
			return result is null ? new CommandResult( CommandStatus.Denied, "Permission explanation access denied." )
				: Rows( new[] { $"allowed={result.IsAllowed}; registered={result.IsRegistered}; ownerBypass={result.OwnerBypass}; direct={result.DirectEntry}; revision={result.PolicyRevision}", result.Reason }
					.Concat( result.Groups.Select( group => group.GroupId + ": " + group.Effect ) ), PageNumber( arguments ), "Permission explanation." );
		}
		catch ( ArgumentException )
		{
			return new CommandResult( CommandStatus.InvalidArguments, "Expected an exact lowercase permission key." );
		}
	}
}
