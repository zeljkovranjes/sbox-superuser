using SboxSuperuser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperuserExamples.Engine;

/// <summary>
/// A larger third-party module: players report other players, staff claim and close the reports. It shows several
/// permissions, commands with several kinds of arguments, multi-page documentation and two admin menu windows.
/// Reports live in memory; a real module would keep them in Superuser's store.
/// </summary>
[AdminModule]
public sealed class ReportsModule : IAdminModule
{
	private const string FilePermission = "example.reports.file";
	private const string ReadPermission = "example.reports.read";
	private const string HandlePermission = "example.reports.handle";
	private const string ManagePermission = "example.reports.manage";
	private const int PageSize = 8;
	private const int MaximumReports = 200;

	private static readonly string[] Filters = { "open", "claimed", "closed", "all" };
	private static readonly string[] Outcomes = { "resolved", "rejected", "duplicate" };

	private readonly List<Report> _reports = new();
	private int _nextId = 1;

	/// <summary>Who the module is and who made it; shown on its Overview page.</summary>
	public ModuleDescriptor Descriptor { get; } = new( "example.reports", "Lets players report problems and staff handle them", 1 )
	{
		Version = "1.0.0",
		Author = "Superuser examples",
		Website = "https://github.com/zeljkovranjes/sbox-superuser"
	};

	/// <summary>Registers the permissions, commands, guides and menu windows; Superuser publishes them together.</summary>
	public Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		RegisterPermissions( registration );
		RegisterCommands( registration );
		RegisterDocumentation( registration );
		RegisterMenuWindows( registration );
		return Task.CompletedTask;
	}

	/// <summary>Reports are kept in memory, so they are gone when the module stops.</summary>
	public Task StopAsync( CancellationToken cancellation )
	{
		_reports.Clear();
		return Task.CompletedTask;
	}

	private static void RegisterPermissions( IModuleRegistration registration )
	{
		registration.RegisterPermission( new PermissionDescriptor( FilePermission, "Report a player and see your own reports" ) );
		registration.RegisterPermission( new PermissionDescriptor( ReadPermission, "See every report" ) );
		registration.RegisterPermission( new PermissionDescriptor( HandlePermission, "Claim and close reports" ) );
		registration.RegisterPermission( new PermissionDescriptor( ManagePermission, "Delete closed or all reports" ) );
	}

	private void RegisterCommands( IModuleRegistration registration )
	{
		registration.RegisterCommand( Command( "report", "Report a player to the staff", FilePermission, CommandExecutionPolicy.Mutation,
			new CommandArgumentDescriptor( "user", "The player you are reporting", CommandArgumentKind.Player,
				targetPolicy: CommandTargetPolicy.AnyPlayer, audit: CommandArgumentAudit.SafeValue ),
			new CommandArgumentDescriptor( "reason", "What happened", CommandArgumentKind.String, isRemainder: true ) ),
			( context, arguments ) => Task.FromResult( File( context, arguments ) ) );

		registration.RegisterCommand( Command( "reports.mine", "See the reports you filed and what happened to them", FilePermission,
			CommandExecutionPolicy.ReadOnly ),
			( context, arguments ) => Task.FromResult( Mine( context ) ) );

		registration.RegisterCommand( Command( "reports.list", "List reports, open ones first", ReadPermission, CommandExecutionPolicy.ReadOnly,
			new CommandArgumentDescriptor( "status", "open, claimed, closed or all", CommandArgumentKind.Enum, isOptional: true,
				choices: Filters, audit: CommandArgumentAudit.SafeValue ),
			new CommandArgumentDescriptor( "page", "Page number", CommandArgumentKind.Integer, isOptional: true, minimum: 1, maximum: 100,
				audit: CommandArgumentAudit.SafeValue ) ),
			( context, arguments ) => Task.FromResult( List( arguments ) ) );

		registration.RegisterCommand( Command( "reports.claim", "Take an open report, so other staff know you have it", HandlePermission,
			CommandExecutionPolicy.Mutation, Id() ),
			( context, arguments ) => Task.FromResult( Claim( context, arguments ) ) );

		registration.RegisterCommand( Command( "reports.close", "Close a report with an outcome and an optional note", HandlePermission,
			CommandExecutionPolicy.Mutation, Id(),
			new CommandArgumentDescriptor( "outcome", "resolved, rejected or duplicate", CommandArgumentKind.Enum, choices: Outcomes,
				audit: CommandArgumentAudit.SafeValue ),
			new CommandArgumentDescriptor( "note", "What you did, for the record", CommandArgumentKind.String, isOptional: true, isRemainder: true ) ),
			( context, arguments ) => Task.FromResult( Close( context, arguments ) ) );

		registration.RegisterCommand( Command( "reports.clear", "Delete closed reports, or all of them", ManagePermission,
			CommandExecutionPolicy.Mutation,
			new CommandArgumentDescriptor( "which", "closed or all", CommandArgumentKind.Enum, choices: new[] { "closed", "all" },
				audit: CommandArgumentAudit.SafeValue ) ),
			( context, arguments ) => Task.FromResult( Clear( arguments ) ) );
	}

	private CommandResult File( CommandContext context, CommandArguments arguments )
	{
		var target = arguments.Get<PlayerReference>( "user" ).SteamId;
		if ( target == context.Actor.SubjectId ) return new CommandResult( CommandStatus.InvalidArguments, "You can't report yourself." );
		var reason = arguments.Get<string>( "reason" ).Trim();
		if ( reason.Length == 0 || reason.Length > 200 ) return new CommandResult( CommandStatus.InvalidArguments, "Say what happened, in up to 200 characters." );

		// Old closed reports make room first, so the list never grows without limit.
		while ( _reports.Count >= MaximumReports && _reports.FindIndex( report => report.Closed ) is var closed and >= 0 ) _reports.RemoveAt( closed );
		if ( _reports.Count >= MaximumReports ) return new CommandResult( CommandStatus.Conflict, "Too many open reports; staff need to close some first." );

		var report = new Report( _nextId++, context.Actor.SubjectId, target, reason, DateTimeOffset.UtcNow );
		_reports.Add( report );
		return new CommandResult( CommandStatus.Success, $"Report #{report.Id} filed. Staff will look at it." );
	}

	private CommandResult Mine( CommandContext context )
	{
		var mine = _reports.Where( report => report.Reporter == context.Actor.SubjectId ).OrderByDescending( report => report.Id ).Take( PageSize ).ToArray();
		if ( mine.Length == 0 ) return new CommandResult( CommandStatus.Success, "You haven't filed any reports." );
		return new CommandResult( CommandStatus.Success, $"Your latest {mine.Length} reports.", Rows( mine, report =>
			$"#{report.Id} about {report.Target}: {report.State}{(report.Note.Length > 0 ? $" ({report.Note})" : "")}" ) );
	}

	private CommandResult List( CommandArguments arguments )
	{
		var status = arguments.Contains( "status" ) ? arguments.Get<string>( "status" ) : "open";
		var page = arguments.Contains( "page" ) ? (int)arguments.Get<long>( "page" ) : 1;
		var matching = _reports.Where( report => status == "all" || report.Status == status )
			.OrderBy( report => report.Closed ).ThenBy( report => report.Id ).ToArray();
		var shown = matching.Skip( (page - 1) * PageSize ).Take( PageSize ).ToArray();
		var kind = status == "all" ? "" : status + " ";
		if ( shown.Length == 0 ) return new CommandResult( CommandStatus.Success, matching.Length == 0 ? $"No {kind}reports." : "No reports on that page." );

		var data = Rows( shown, report => $"#{report.Id} {report.State}: {report.Target}, reported by {report.Reporter} {Ago( report.Filed )} - {report.Reason}" );
		data[ "page" ] = page.ToString( CultureInfo.InvariantCulture );
		data[ "more" ] = (matching.Length > page * PageSize).ToString();
		return new CommandResult( CommandStatus.Success, $"{matching.Length} {kind}report{(matching.Length == 1 ? "" : "s")}.", data );
	}

	private CommandResult Claim( CommandContext context, CommandArguments arguments )
	{
		if ( Find( arguments ) is not { } report ) return Missing( arguments );
		if ( report.Closed ) return new CommandResult( CommandStatus.Conflict, $"Report #{report.Id} is already closed." );
		if ( report.Handler == context.Actor.SubjectId ) return new CommandResult( CommandStatus.NoChange, $"You already have report #{report.Id}." );
		if ( report.Handler is not null ) return new CommandResult( CommandStatus.Conflict, $"Report #{report.Id} is claimed by {report.Handler}." );
		report.Handler = context.Actor.SubjectId;
		return new CommandResult( CommandStatus.Success, $"You claimed report #{report.Id}." );
	}

	private CommandResult Close( CommandContext context, CommandArguments arguments )
	{
		if ( Find( arguments ) is not { } report ) return Missing( arguments );
		if ( report.Closed ) return new CommandResult( CommandStatus.NoChange, $"Report #{report.Id} is already closed." );
		if ( report.Handler is not null && report.Handler != context.Actor.SubjectId )
			return new CommandResult( CommandStatus.Conflict, $"Report #{report.Id} is claimed by {report.Handler}; only they can close it." );
		report.Handler = context.Actor.SubjectId;
		report.Outcome = arguments.Get<string>( "outcome" );
		report.Note = arguments.Contains( "note" ) ? arguments.Get<string>( "note" ).Trim() : "";
		return new CommandResult( CommandStatus.Success, $"Report #{report.Id} closed as {report.Outcome}." );
	}

	private CommandResult Clear( CommandArguments arguments )
	{
		var all = arguments.Get<string>( "which" ) == "all";
		var removed = _reports.RemoveAll( report => all || report.Closed );
		return new CommandResult( removed == 0 ? CommandStatus.NoChange : CommandStatus.Success,
			removed == 0 ? "There was nothing to delete." : $"Deleted {removed} report{(removed == 1 ? "" : "s")}." );
	}

	private Report Find( CommandArguments arguments )
	{
		var id = (int)arguments.Get<long>( "id" );
		return _reports.FirstOrDefault( report => report.Id == id );
	}

	private static CommandResult Missing( CommandArguments arguments ) =>
		new( CommandStatus.NotFound, $"There is no report #{arguments.Get<long>( "id" )}." );

	private static Dictionary<string, string> Rows( IEnumerable<Report> reports, Func<Report, string> describe ) =>
		reports.Select( ( report, index ) => (Key: "row" + (index + 1), Value: describe( report )) ).ToDictionary( row => row.Key, row => row.Value );

	private static string Ago( DateTimeOffset when )
	{
		var elapsed = DateTimeOffset.UtcNow - when;
		return elapsed.TotalMinutes < 1 ? "just now" : elapsed.TotalHours < 1 ? $"{(int)elapsed.TotalMinutes} min ago" : $"{(int)elapsed.TotalHours} h ago";
	}

	private CommandDescriptor Command( string name, string description, string permission, CommandExecutionPolicy policy,
		params CommandArgumentDescriptor[] arguments ) => new( name, description, permission, Descriptor.Id, arguments, executionPolicy: policy );

	private static CommandArgumentDescriptor Id() => new( "id", "The report number, such as 3", CommandArgumentKind.Integer,
		minimum: 1, maximum: 100000, audit: CommandArgumentAudit.SafeValue );

	// Each viewer sees only what their permissions allow: players get "My reports", staff get "Reports" too.
	private static void RegisterMenuWindows( IModuleRegistration registration )
	{
		registration.AddMenuWindow( "Reports", window => window
			.Text( "Player reports waiting for staff. Claim one so others know you have it, then close it with an outcome." )
			.Heading( "Open" )
			.Live( "reports.list", "open" )
			.Heading( "Claimed" )
			.Live( "reports.list", "claimed" )
			.Heading( "Handle a report" )
			.CommandForm( "reports.claim" )
			.CommandForm( "reports.close" )
			.Separator()
			.Button( "Delete closed reports", "reports.clear", "closed" ) );

		registration.AddMenuWindow( "My reports", window => window
			.Text( "Report a player who is breaking the rules. Staff see it straight away; you can follow it here." )
			.CommandForm( "report" )
			.Heading( "Your reports" )
			.Live( "reports.mine" ) );
	}

	private static void RegisterDocumentation( IModuleRegistration registration )
	{
		registration.AddDocumentation( "Overview", """
			# Reports

			Players report other players; staff claim the report, deal with it and close it with an outcome.
			Every step is an ordinary command, so it is checked against permissions and recorded in Activity.

			## How a report moves

			1. A player files it with `report <user> <reason>`. It is **open**.
			2. A staff member claims it with `reports claim <id>`. It is **claimed**, and only they can close it.
			3. They close it with `reports close <id> <outcome> [note]`. It is **closed** as resolved, rejected or duplicate.
			4. The player sees the outcome with `reports mine`.

			## Commands

			| Command | Needs | What it does |
			|---|---|---|
			| `report <user> <reason>` | `example.reports.file` | File a report |
			| `reports mine` | `example.reports.file` | Your reports and their outcomes |
			| `reports list [status] [page]` | `example.reports.read` | Every report, 8 per page |
			| `reports claim <id>` | `example.reports.handle` | Take an open report |
			| `reports close <id> <outcome> [note]` | `example.reports.handle` | Close a report |
			| `reports clear <closed\|all>` | `example.reports.manage` | Delete reports |

			In the menu, staff use **Modules › Reports** and players **Modules › My reports**.
			""" );

		registration.AddDocumentation( "Reporting a player", """
			# Reporting a player

			Use the player's exact Steam ID or their unique name, then say what happened:

			```text
			admin report Alex keeps blocking the spawn
			admin report 76561198000000003 spamming slurs in chat
			```

			You can't report yourself, and the reason can be up to 200 characters. Then follow your report:

			```text
			admin reports mine
			```

			Or open **Modules › My reports**, which has the same form and shows your reports with their outcome.
			""" );

		registration.AddDocumentation( "Handling reports", """
			# Handling reports

			1. Open **Modules › Reports**, or run `admin reports list`. Open reports come first.
			2. Claim the one you are taking: `admin reports claim 3`. Others see it as claimed.
			3. Deal with it, for example with a warning from the Players window.
			4. Close it with the outcome that fits, and a note for the record:

			```text
			admin reports close 3 resolved warned them about spawn blocking
			```

			| Outcome | Use it when |
			|---|---|
			| `resolved` | You took action, or the problem stopped |
			| `rejected` | Nothing against the rules happened |
			| `duplicate` | Another report already covers it |

			A report someone else claimed can only be closed by them. `reports list claimed` shows who has what.
			""" );

		registration.AddDocumentation( "Setting up", """
			# Setting up

			Nobody has these permissions until you grant them. A typical setup:

			| Group | Permissions |
			|---|---|
			| `player` | `example.reports.file` |
			| `moderator` | `example.reports.read`, `example.reports.handle` |
			| `admin` | `example.reports.manage` |

			```text
			admin group player permission allow example.reports.file
			admin group moderator permission allow example.reports.read
			admin group moderator permission allow example.reports.handle
			admin group admin permission allow example.reports.manage
			```

			Groups inherit from their parents, so admins can also do everything moderators can. Owners can do
			everything without any grant. Reports are kept in memory and cleared when the server restarts.
			""" );
	}

	private sealed class Report
	{
		internal Report( int id, string reporter, string target, string reason, DateTimeOffset filed )
		{
			Id = id;
			Reporter = reporter;
			Target = target;
			Reason = reason;
			Filed = filed;
		}

		internal int Id { get; }
		internal string Reporter { get; }
		internal string Target { get; }
		internal string Reason { get; }
		internal DateTimeOffset Filed { get; }
		internal string Handler { get; set; }
		internal string Outcome { get; set; }
		internal string Note { get; set; } = "";
		internal bool Closed => Outcome is not null;
		internal string Status => Closed ? "closed" : Handler is null ? "open" : "claimed";
		internal string State => Closed ? $"closed as {Outcome}" : Handler is null ? "open" : $"claimed by {Handler}";
	}
}
