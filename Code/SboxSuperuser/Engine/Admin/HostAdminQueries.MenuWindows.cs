namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	// Each viewer gets only the elements whose commands they may run, and a window only when something in it is
	// theirs to use. Text-only windows follow the module: visible to anyone who may run one of its commands.
	private AdminReply ReadMenuWindows( Guid id, ActorIdentity actor, Func<string, bool> can )
	{
		if ( _registry is null ) return Failure( id, CommandStatus.Unsupported, "Module windows are unavailable on this host." );
		var owner = IsOwner( actor );
		var commands = _registry.Descriptors.ToDictionary( command => command.Name, StringComparer.Ordinal );
		bool Usable( string name ) => commands.TryGetValue( name, out var command ) && can( command.Permission )
			&& (command.Permission != SecurityPermission || owner);

		var windows = new List<AdminMenuWindowRow>();
		foreach ( var module in _registry.RegisteredModules )
		{
			var moduleVisible = owner || commands.Values.Any( command => command.ModuleId == module && Usable( command.Name ) );
			if ( !moduleVisible ) continue;
			foreach ( var window in _registry.DocumentationOf( module ).Windows )
			{
				var elements = window.Elements.Where( element => element.Command.Length == 0 || Usable( element.Command ) ).ToArray();
				var usesCommands = window.Elements.Any( element => element.Command.Length > 0 );
				if ( usesCommands && elements.All( element => element.Command.Length == 0 ) ) continue;
				windows.Add( new AdminMenuWindowRow { ModuleId = window.ModuleId, Title = window.Title, Elements = elements } );
			}
		}
		return new AdminReply
		{
			RequestId = id, Status = CommandStatus.Success, Message = "Module windows.",
			MenuWindows = windows.OrderBy( window => window.Title, StringComparer.OrdinalIgnoreCase ).ToArray()
		};
	}
}
