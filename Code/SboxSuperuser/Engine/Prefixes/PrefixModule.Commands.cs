using System.Globalization;
using SboxSuperuser.Core.Prefixes;

namespace SboxSuperuser.Engine.Prefixes;

internal sealed partial class PrefixModule
{
	private static readonly string[] Fields = { "name", "tag", "template", "palette", "tag-effect", "name-effect" };

	private void RegisterCommands( IModuleRegistration registration )
	{
		registration.RegisterCommand( Command( "prefix.list", "List the server's prefixes", "admin.help", CommandExecutionPolicy.ReadOnly,
			new CommandArgumentDescriptor( "page", "One-based page number", CommandArgumentKind.Integer, isOptional: true, minimum: 1, maximum: 100,
				audit: CommandArgumentAudit.SafeValue ) ), List );
		registration.RegisterCommand( Command( "prefix.export", "Read every prefix and assignment as JSON, for the menu and backups",
			ManagePermission, CommandExecutionPolicy.ReadOnly ), Export );
		registration.RegisterCommand( Command( "prefix.create", "Create a prefix with a plain tag; style it with prefix update", ManagePermission,
			CommandExecutionPolicy.Mutation, PrefixArgument(), Text( "tag", "Tag text, such as [VIP]", optional: true, remainder: true ) ), CreateAsync );
		registration.RegisterCommand( Command( "prefix.delete", "Delete a prefix and every assignment that uses it", ManagePermission,
			CommandExecutionPolicy.Mutation, PrefixArgument() ), DeleteAsync );
		registration.RegisterCommand( Command( "prefix.update", "Change a prefix: name, tag, template, palette, tag-effect or name-effect",
			ManagePermission, CommandExecutionPolicy.Mutation, PrefixArgument(),
			new CommandArgumentDescriptor( "field", "What to change", CommandArgumentKind.Enum, choices: Fields, audit: CommandArgumentAudit.SafeValue ),
			Text( "value", "New value; effects take preset:<id>, code:<id> or a script", optional: true, remainder: true ) ), UpdateAsync );
		registration.RegisterCommand( Command( "prefix.group.set", "Give every member of a group a prefix (the highest protection group wins)",
			ManagePermission, CommandExecutionPolicy.Mutation, GroupArgument(), PrefixArgument() ), ( context, arguments ) => AssignGroupAsync( context, arguments, true ) );
		registration.RegisterCommand( Command( "prefix.group.clear", "Remove a group's prefix", ManagePermission, CommandExecutionPolicy.Mutation,
			GroupArgument() ), ( context, arguments ) => AssignGroupAsync( context, arguments, false ) );
		registration.RegisterCommand( Command( "prefix.user.set", "Give one player a prefix; it beats their group prefixes", ManagePermission,
			CommandExecutionPolicy.Mutation, UserArgument(), PrefixArgument() ), ( context, arguments ) => AssignUserAsync( context, arguments, true ) );
		registration.RegisterCommand( Command( "prefix.user.clear", "Remove a player's own prefix so their group prefix applies again",
			ManagePermission, CommandExecutionPolicy.Mutation, UserArgument() ), ( context, arguments ) => AssignUserAsync( context, arguments, false ) );
	}

	private Task<CommandResult> List( CommandContext context, CommandArguments arguments )
	{
		const int pageSize = 8;
		var page = arguments.Contains( "page" ) ? (int)arguments.Get<long>( "page" ) : 1;
		var rows = _catalog.Prefixes.Skip( (page - 1) * pageSize ).Take( pageSize + 1 ).ToArray();
		var data = rows.Take( pageSize ).Select( ( prefix, index ) => new KeyValuePair<string, string>( "row" + (index + 1),
			$"{prefix.Id}: {prefix.Format( "<name>" )}; tag {Describe( prefix.TagStyle )}; name {Describe( prefix.NameStyle )}" ) )
			.ToDictionary( pair => pair.Key, pair => pair.Value );
		data[ "more" ] = (rows.Length > pageSize).ToString();
		data[ "page" ] = page.ToString( CultureInfo.InvariantCulture );
		return Task.FromResult( new CommandResult( CommandStatus.Success, $"{_catalog.Prefixes.Count} prefixes.", data ) );
	}

	private Task<CommandResult> Export( CommandContext context, CommandArguments arguments )
	{
		if ( _problem is not null ) return Task.FromResult( new CommandResult( CommandStatus.StorageUnavailable, _problem ) );
		return Task.FromResult( new CommandResult( CommandStatus.Success, "Prefix catalog.", new Dictionary<string, string>
		{
			[ "catalog" ] = PrefixCodec.WriteState( _catalog ),
			[ "revision" ] = _revision.ToString( CultureInfo.InvariantCulture ),
			// Every group, so the menu can list them all; IDs are dotted keys, so "id:protection" pairs need no escaping.
			[ "groups" ] = string.Join( ",", _host.Groups().Select( group =>
				group.GroupId + ":" + group.Protection.ToString( CultureInfo.InvariantCulture ) ) )
		} ) );
	}

	private Task<CommandResult> CreateAsync( CommandContext context, CommandArguments arguments )
	{
		var id = arguments.Get<string>( "prefix" );
		if ( _catalog.Find( id ) is not null ) return Done( CommandStatus.Conflict, $"A prefix called '{id}' already exists." );
		var tag = arguments.Contains( "tag" ) ? arguments.Get<string>( "tag" ) : $"[{id.ToUpperInvariant()}]";
		return Change( context, "prefix.create", () => _catalog.Save( PrefixDefinition.Create( id, id, tag,
			tagStyle: new PrefixStyle( PrefixStyleKind.Preset, "pink" ) ) ), $"Created prefix '{id}'." );
	}

	private Task<CommandResult> DeleteAsync( CommandContext context, CommandArguments arguments )
	{
		var id = arguments.Get<string>( "prefix" );
		if ( _catalog.Find( id ) is null ) return Missing( id );
		return Change( context, "prefix.delete", () => _catalog.Delete( id ), $"Deleted prefix '{id}' and its assignments." );
	}

	private Task<CommandResult> UpdateAsync( CommandContext context, CommandArguments arguments )
	{
		var id = arguments.Get<string>( "prefix" );
		var prefix = _catalog.Find( id );
		if ( prefix is null ) return Missing( id );
		var field = arguments.Get<string>( "field" );
		var value = arguments.Contains( "value" ) ? arguments.Get<string>( "value" ) : "";
		if ( field is "tag-effect" or "name-effect" )
		{
			PrefixStyle style;
			try { style = PrefixStyleText.Parse( value.Length == 0 ? "preset:white" : value ); }
			catch ( ArgumentException error ) { return Done( CommandStatus.InvalidArguments, error.Message ); }
			if ( style.Kind == PrefixStyleKind.Script && _host.CompileProblem( style.Value ) is { } problem )
				return Done( CommandStatus.InvalidArguments, $"The effect has an error: {problem}" );
			return Change( context, "prefix.update", () => _catalog.Save( field == "tag-effect"
				? prefix.With( tagStyle: style ) : prefix.With( nameStyle: style ) ), $"Updated the {field} of '{id}'." );
		}

		return Change( context, "prefix.update", () => _catalog.Save( field switch
		{
			"name" => prefix.With( name: value ),
			"tag" => prefix.With( tag: value ),
			"template" => prefix.With( template: value ),
			_ => prefix.With( palette: value.Split( new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries ) )
		} ), $"Updated the {field} of '{id}'." );
	}

	private Task<CommandResult> AssignGroupAsync( CommandContext context, CommandArguments arguments, bool set )
	{
		var group = arguments.Get<string>( "group" );
		if ( _host.Groups().All( known => known.GroupId != group ) ) return Done( CommandStatus.NotFound, $"There is no group called '{group}'." );
		var id = set ? arguments.Get<string>( "prefix" ) : null;
		if ( set && _catalog.Find( id ) is null ) return Missing( id );
		if ( !set && !_catalog.Groups.ContainsKey( group ) ) return Done( CommandStatus.NoChange, $"Group '{group}' has no prefix." );
		return Change( context, set ? "prefix.group.set" : "prefix.group.clear", () => _catalog.AssignGroup( group, id ),
			set ? $"Group '{group}' now wears '{id}'." : $"Group '{group}' no longer has a prefix." );
	}

	private Task<CommandResult> AssignUserAsync( CommandContext context, CommandArguments arguments, bool set )
	{
		var steamId = arguments.Get<PlayerReference>( "user" ).SteamId;
		var id = set ? arguments.Get<string>( "prefix" ) : null;
		if ( set && _catalog.Find( id ) is null ) return Missing( id );
		if ( !set && !_catalog.Users.ContainsKey( steamId ) ) return Done( CommandStatus.NoChange, "That player has no prefix of their own." );
		return Change( context, set ? "prefix.user.set" : "prefix.user.clear", () => _catalog.AssignUser( steamId, id ),
			set ? $"{steamId} now wears '{id}'." : $"{steamId} uses their group prefix again." );
	}

	// Validation errors from the domain come back as invalid arguments, with their plain message.
	private Task<CommandResult> Change( CommandContext context, string command, Func<PrefixCatalog> change, string done )
	{
		PrefixCatalog candidate;
		try { candidate = change(); }
		catch ( ArgumentException error ) { return Done( CommandStatus.InvalidArguments, error.Message.Split( " (Parameter" )[ 0 ] ); }
		return SaveAsync( context, command, candidate, done );
	}

	private static string Describe( PrefixStyle style ) => style.Kind switch
	{
		PrefixStyleKind.Preset => "preset " + style.Value,
		PrefixStyleKind.Code => "C# " + style.Value,
		_ => $"script ({style.Value.Length} characters)"
	};

	private static Task<CommandResult> Missing( string id ) => Done( CommandStatus.NotFound, $"There is no prefix called '{id}'." );

	private static Task<CommandResult> Done( CommandStatus status, string message ) => Task.FromResult( new CommandResult( status, message ) );

	private static CommandDescriptor Command( string name, string description, string permission, CommandExecutionPolicy policy,
		params CommandArgumentDescriptor[] arguments ) => new( name, description, permission, Id, arguments, executionPolicy: policy );

	private static CommandArgumentDescriptor PrefixArgument() => Text( "prefix", "Prefix ID, such as vip" );

	private static CommandArgumentDescriptor GroupArgument() => Text( "group", "Group ID, such as admin, or owner for every owner" );

	private static CommandArgumentDescriptor UserArgument() => new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player,
		targetPolicy: CommandTargetPolicy.AnyPlayer, audit: CommandArgumentAudit.SafeValue );

	private static CommandArgumentDescriptor Text( string name, string help, bool optional = false, bool remainder = false ) =>
		new( name, help, CommandArgumentKind.String, isOptional: optional, isRemainder: remainder );
}
