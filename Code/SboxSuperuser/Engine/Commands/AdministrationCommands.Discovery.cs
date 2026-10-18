namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	private void RegisterDiscovery()
	{
		Register( "help", "Show permitted commands or detailed syntax. Example: admin help version", "admin.help",
			new[] { Text( "command", "Exact command key, or * to list a later page", true ), Page() },
			( context, arguments ) => Task.FromResult( Help( context, arguments ) ) );
		Register( "commands", "List permitted command keys. Example: admin commands 2", "admin.help", new[] { Page() },
			( context, arguments ) => Task.FromResult( Rows( Visible( context ).Select( item => item.Name ), PageNumber( arguments ), "Permitted commands." ) ) );
		Register( "version", "Show the framework API version. Example: admin version", "admin.help", Array.Empty<CommandArgumentDescriptor>(),
			( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Superuser API 1; in-process dedicated-server administration." ) ) );
		Register( "diagnostics", "Show host authority, audit and transport health. Example: admin diagnostics", "admin.security.manage",
			Array.Empty<CommandArgumentDescriptor>(), ( context, _ ) => Task.FromResult( IsOwner( context ) ? _diagnostics() : Denied() ) );
		Register( "modules.list", "List registered module IDs. Example: admin modules list", "admin.security.manage", new[] { Page() },
			( context, arguments ) => Task.FromResult( IsOwner( context )
				? Rows( _registry.Descriptors.Select( item => item.ModuleId ).Distinct().Order(), PageNumber( arguments ), "Registered command modules." ) : Denied() ) );
		var filters = new[] { Text( "command", "Exact command key or * for all", true ), Text( "cursor", "Opaque revision:sequence from the previous page", true ) };
		Register( "audit.query", "Read permitted audit records. Example: admin audit query *", "admin.audit.read", filters,
			( context, arguments ) => AuditAsync( context, arguments, false ), auditQuery: true );
		Register( "results", "Read only your own action results. Example: admin results *", "admin.results.own", filters,
			( context, arguments ) => AuditAsync( context, arguments, true ), auditQuery: true );
	}

	private IEnumerable<CommandDescriptor> Visible( CommandContext context ) => _registry.Descriptors.Where( descriptor =>
		_dispatcher.CanUse( context, descriptor.Permission ) && (descriptor.Permission != "admin.security.manage" || IsOwner( context )) );

	private CommandResult Help( CommandContext context, CommandArguments arguments )
	{
		var command = arguments.Contains( "command" ) ? arguments.Get<string>( "command" ) : "*";
		if ( command == "*" )
		{
			return Rows( Visible( context ).Select( descriptor => CommandSyntaxBuilder.Build( descriptor ).Syntax + " — " + descriptor.Description ),
				PageNumber( arguments ), "Use help <command> for constraints and required permission. Native root defaults to admin." );
		}

		var descriptor = _registry.Find( command )?.Descriptor;
		if ( descriptor is null || !Visible( context ).Contains( descriptor ) )
		{
			return new CommandResult( CommandStatus.NotFound, "No permitted command matches that key." );
		}

		var syntax = CommandSyntaxBuilder.Build( descriptor );
		var rows = new[] { syntax.Syntax, syntax.Description, "permission: " + syntax.Permission }
			.Concat( syntax.Arguments ).Concat( descriptor.Aliases.Count == 0 ? Array.Empty<string>() : new[] { "aliases: " + string.Join( ", ", descriptor.Aliases ) } );
		return Rows( rows, PageNumber( arguments ), "Command help; angle brackets are required and square brackets optional." );
	}

	private async Task<CommandResult> AuditAsync( CommandContext context, CommandArguments arguments, bool ownOnly )
	{
		AuditCursor cursor = null;
		if ( arguments.Contains( "cursor" ) )
		{
			var values = arguments.Get<string>( "cursor" ).Split( ':' );
			if ( values.Length != 2 || !long.TryParse( values[ 0 ], out var revision ) || !long.TryParse( values[ 1 ], out var before ) || revision < 1 || before < 1 )
			{
				return new CommandResult( CommandStatus.InvalidArguments, "Use the revision:sequence cursor from the previous result." );
			}
			cursor = new AuditCursor( revision, before );
		}

		var command = arguments.Contains( "command" ) ? arguments.Get<string>( "command" ) : "*";
		AuditQuery query;
		try
		{
			query = new AuditQuery( 5, cursor, command: command == "*" ? null : command );
		}
		catch ( ArgumentException )
		{
			return new CommandResult( CommandStatus.InvalidArguments, "Expected a command key or *." );
		}

		var page = await _audit.QueryAsync( () => Authenticate( context ), _ => !ownOnly && _dispatcher.CanUse( context, "admin.audit.read" ),
			_ => _dispatcher.CanUse( context, "admin.results.own" ), query, context.Cancellation );
		var data = page.Entries.Select( (entry, index) => new KeyValuePair<string, string>( "row" + (index + 1),
			$"{entry.CreatedUtc:O} {entry.RequestId} {entry.ActorKind}:{entry.ActorSubject} {entry.Command} {entry.Outcome}" ) ).ToDictionary( pair => pair.Key, pair => pair.Value );
		if ( page.NextCursor is not null )
		{
			data[ "cursor" ] = $"{page.NextCursor.Revision}:{page.NextCursor.BeforeSequence}";
		}
		return new CommandResult( page.Status, page.Message, data );
	}
}
