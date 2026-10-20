using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	private const string BuiltInPrefix = "superuser.";

	// Owners see every module. Others see a module when they may run at least one of its commands, so the list never
	// reveals more than the Commands window would. Superuser's own module is always listed: its help is public.
	private AdminReply ReadDocumentation( Guid id, AdminQuery query, ActorIdentity actor, Func<string, bool> can )
	{
		if ( _registry is null ) return Failure( id, CommandStatus.Unsupported, "Documentation is unavailable on this host." );
		var owner = IsOwner( actor );
		var commands = _registry.Descriptors;
		bool Usable( CommandDescriptor command ) => can( command.Permission ) && (command.Permission != SecurityPermission || owner);
		bool Visible( string module ) => owner || module == "superuser.administration"
			|| commands.Any( command => command.ModuleId == module && Usable( command ) );

		var statuses = _modules();
		var modules = _registry.RegisteredModules.Concat( owner ? statuses.Select( status => status.Descriptor.Id ) : Array.Empty<string>() )
			.Distinct( StringComparer.Ordinal ).Where( Visible ).ToArray();

		if ( query.Search.Length == 0 )
		{
			return new AdminReply
			{
				RequestId = id, Status = CommandStatus.Success, Message = "Documented modules.", ModulesAllowed = owner,
				Modules = modules.Select( module => Describe( module, statuses, commands ) )
					.OrderBy( row => row.Id.StartsWith( BuiltInPrefix, StringComparison.Ordinal ) ? 0 : 1 ).ThenBy( row => row.Id, StringComparer.Ordinal )
					.ToArray()
			};
		}

		var pages = modules.Contains( query.Search ) ? _registry.DocumentationOf( query.Search ).Pages : null;
		if ( pages is null || query.Page >= pages.Count ) return Failure( id, CommandStatus.NotFound, "That documentation page does not exist." );
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Documentation page.",
			DocumentTitle = pages[ query.Page ].Title, Document = pages[ query.Page ].Markdown
		};
	}

	private AdminModuleRow Describe( string module, IReadOnlyList<ModuleStatus> statuses, IReadOnlyList<CommandDescriptor> commands )
	{
		var status = statuses.FirstOrDefault( entry => entry.Descriptor.Id == module );
		var descriptor = status?.Descriptor;
		var documentation = _registry.DocumentationOf( module );
		var builtIn = module.StartsWith( BuiltInPrefix, StringComparison.Ordinal );

		// A module's permissions are the ones it registers plus any its commands need, so nothing a command uses is missing.
		var described = documentation.Permissions.ToDictionary( permission => permission.Key, permission => permission.Description, StringComparer.Ordinal );
		var permissions = described.Keys.Concat( commands.Where( command => command.ModuleId == module ).Select( command => command.Permission ) )
			.Distinct( StringComparer.Ordinal ).Order( StringComparer.Ordinal )
			.Select( key => new AdminPermissionRow { Key = key, Description = described.GetValueOrDefault( key, "" ) } ).ToArray();

		return new AdminModuleRow
		{
			Id = module,
			ApiVersion = descriptor?.ApiVersion ?? 1,
			State = status?.State.ToString() ?? "Enabled",
			Message = status is null ? "Registered directly by game code." : status.Message,
			Description = builtIn ? BuiltInDescription( module ) : descriptor?.Description ?? "",
			Version = builtIn ? ProductInfo.Version : descriptor?.Version ?? "",
			Author = builtIn ? ProductInfo.Author : descriptor?.Author ?? "",
			Website = builtIn ? ProductInfo.Website : descriptor?.Website ?? "",
			Pages = documentation.Pages.Select( page => page.Title ).ToArray(),
			Permissions = permissions,
			Dependencies = descriptor?.Dependencies.Select( dependency =>
				$"{dependency.ModuleId} (API {dependency.RequiredApiVersion}{(dependency.IsOptional ? ", optional" : "")})" ).ToArray() ?? Array.Empty<string>()
		};
	}

	private static string BuiltInDescription( string module ) => module switch
	{
		"superuser.administration" => "Superuser's built-in commands: help, access management, moderation, audit and modules.",
		"superuser.core" => "Superuser's security core: identity, permissions, audit and storage.",
		_ => "A module shipped with Superuser."
	};
}
