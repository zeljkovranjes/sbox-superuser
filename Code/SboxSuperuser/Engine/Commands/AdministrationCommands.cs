using SboxSuperuser.Engine.Audit;
using SboxSuperuser.Engine.Authority;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands : IDisposable
{
	private const string Module = "superuser.administration";
	private readonly HostPolicyService _policy;
	private readonly HostAuditService _audit;
	private readonly CommandRegistry _registry;
	private readonly HostCommandDispatcher _dispatcher;
	private readonly Func<CommandResult> _diagnostics;
	private readonly CommandRegistrationScope _scope;

	internal AdministrationCommands( HostPolicyService policy, HostAuditService audit, CommandRegistry registry,
		HostCommandDispatcher dispatcher, Func<CommandResult> diagnostics )
	{
		_policy = policy;
		_audit = audit;
		_registry = registry;
		_dispatcher = dispatcher;
		_diagnostics = diagnostics;
		_scope = new CommandRegistrationScope( registry, Module );
		RegisterDiscovery();
		RegisterAccess();
		_scope.Commit();
	}

	private void Register( string name, string description, string permission, CommandArgumentDescriptor[] arguments,
		CommandHandler handler, bool mutation = false, bool auditQuery = false )
	{
		var descriptor = new CommandDescriptor( name, description, permission, Module, arguments,
			executionPolicy: mutation ? CommandExecutionPolicy.Mutation : CommandExecutionPolicy.ReadOnly );
		if ( auditQuery )
		{
			_scope.RegisterAuditQuery( descriptor, handler );
		}
		else
		{
			_scope.RegisterCommand( descriptor, handler );
		}
	}

	private static CommandArgumentDescriptor Text( string name, string help, bool optional = false )
		=> new( name, help, CommandArgumentKind.String, isOptional: optional );

	private static CommandArgumentDescriptor Player() => new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player,
		targetPolicy: CommandTargetPolicy.AnyPlayer, audit: CommandArgumentAudit.SafeValue );

	private static CommandArgumentDescriptor Page() => new( "page", "One-based page number", CommandArgumentKind.Integer,
		isOptional: true, minimum: 1, maximum: 10000, audit: CommandArgumentAudit.SafeValue );

	private static int PageNumber( CommandArguments arguments ) => arguments.Contains( "page" ) ? (int)arguments.Get<long>( "page" ) : 1;

	private static ActorIdentity Authenticate( CommandContext context ) => context.Services is CommandExecutionServices services
		? services.Authenticate() : null;

	private bool IsOwner( CommandContext context )
	{
		var actor = Authenticate( context );
		return actor is not null && _policy.IsUsable && (actor.Kind == ActorKind.DedicatedConsole
			|| actor.Kind == ActorKind.Player && _policy.IsOwner( actor.SubjectId ));
	}

	private static CommandResult Denied() => new( CommandStatus.Denied, "Current owner or dedicated local console authority is required." );

	private static CommandResult Rows( IEnumerable<string> rows, int page, string label )
	{
		var selected = rows.Skip( (page - 1) * 8 ).Take( 9 ).ToArray();
		var fields = selected.Take( 8 ).Select( (row, index) => new KeyValuePair<string, string>( "row" + (index + 1), Clip( row, 880 ) ) )
			.ToDictionary( pair => pair.Key, pair => pair.Value );
		fields[ "page" ] = page.ToString( System.Globalization.CultureInfo.InvariantCulture );
		fields[ "more" ] = (selected.Length > 8).ToString();
		return new CommandResult( CommandStatus.Success, label, fields );
	}

	private static string Clip( string text, int maximum ) => text.Length <= maximum ? text : text.Substring( 0, maximum - 1 ) + "…";

	public void Dispose() => _scope.Dispose();
}
