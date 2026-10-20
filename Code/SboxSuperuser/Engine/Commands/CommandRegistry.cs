using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Engine.Commands;

internal sealed class CommandRegistry
{
	private Dictionary<string, RegisteredCommand> _commands = new( StringComparer.Ordinal );
	private Dictionary<string, string> _permissions = new( StringComparer.Ordinal );
	private Dictionary<string, Guid> _modules = new( StringComparer.Ordinal );
	private Dictionary<string, ModuleDocumentation> _documentation = new( StringComparer.Ordinal );
	private readonly Action<IEnumerable<string>> _publishPermissions;
	private readonly Func<bool> _available;
	private readonly HashSet<string> _managed = new( StringComparer.Ordinal );
	internal bool HasModule( string id ) => _modules.ContainsKey( id ) || _managed.Contains( id );
	internal void ReserveModule( string id ) => _managed.Add( id );

	/// <summary>Reads a module's Markdown file for AddDocumentationFile; null where no file system is available.</summary>
	internal Func<string, string> ReadDocumentationFile { get; init; }

	/// <summary>IDs of every module with a committed registration, sorted.</summary>
	internal IReadOnlyList<string> RegisteredModules => _modules.Keys.Order( StringComparer.Ordinal ).ToArray();

	internal ModuleDocumentation DocumentationOf( string moduleId ) =>
		_documentation.TryGetValue( moduleId, out var documentation ) ? documentation
			: new ModuleDocumentation( Array.Empty<Core.Documentation.DocumentationPage>(), Array.Empty<PermissionDescriptor>() );

	internal CommandRegistry( IEnumerable<string> reservedPermissions, Action<IEnumerable<string>> publishPermissions, Func<bool> available )
	{
		_publishPermissions = publishPermissions;
		_available = available;
		foreach ( var key in reservedPermissions )
		{
			_permissions.Add( key, "superuser.core" );
		}
	}

	internal RegisteredCommand Find( string name ) => _commands.TryGetValue( name, out var command ) ? command : null;

	internal IReadOnlyList<CommandDescriptor> Descriptors => Array.AsReadOnly( _commands.Values
		.Distinct().Select( value => value.Descriptor ).OrderBy( value => value.Name, StringComparer.Ordinal ).ToArray() );

	internal void Replace( string moduleId, Guid token, IReadOnlyList<RegisteredCommand> commands, IReadOnlyList<PermissionDescriptor> permissions,
		bool managed = false, IReadOnlyList<Core.Documentation.DocumentationPage> pages = null )
	{
		EnsureAvailable( moduleId );
		if ( _managed.Contains( moduleId ) && !managed )
		{
			throw new InvalidOperationException( $"Module '{moduleId}' is owned by the lifecycle manager." );
		}
		var nextPermissions = _permissions.Where( pair => pair.Value != moduleId ).ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		foreach ( var permission in permissions )
		{
			if ( permission.Key.Length > 96 || permission.Description.Length > 2048 || !nextPermissions.TryAdd( permission.Key, moduleId ) )
			{
				throw new ArgumentException( $"Module '{moduleId}': permission '{permission.Key}' is duplicate, reserved or oversized." );
			}
		}

		var nextCommands = _commands.Where( pair => pair.Value.Descriptor.ModuleId != moduleId )
			.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		foreach ( var command in commands )
		{
			CommandMetadata.Validate( command.Descriptor );
			if ( command.Descriptor.ModuleId != moduleId )
			{
				throw new ArgumentException( $"Module '{moduleId}' cannot register commands for another module." );
			}

			foreach ( var name in new[] { command.Descriptor.Name }.Concat( command.Descriptor.Aliases ) )
			{
				if ( !nextCommands.TryAdd( name, command ) )
				{
					throw new ArgumentException( $"Module '{moduleId}': command or alias '{name}' is already registered." );
				}
			}
		}

		foreach ( var command in nextCommands.Values )
		{
			if ( !nextPermissions.ContainsKey( command.Descriptor.Permission ) )
			{
				throw new ArgumentException( $"Command '{command.Descriptor.Name}' requires unregistered permission '{command.Descriptor.Permission}'." );
			}
		}

		var modules = new Dictionary<string, Guid>( _modules, StringComparer.Ordinal ) { [ moduleId ] = token };
		if ( modules.Count > 128 || nextCommands.Values.Distinct().Count() > 512 || nextCommands.Count > 2048 || nextPermissions.Count > 4096 )
		{
			throw new ArgumentException( "Registration capacity exceeded." );
		}

		// Publish only after every candidate validates. A failed replacement keeps the prior catalog intact.
		_publishPermissions( nextPermissions.Keys );
		_permissions = nextPermissions;
		_commands = nextCommands;
		_modules = modules;
		_documentation = new Dictionary<string, ModuleDocumentation>( _documentation, StringComparer.Ordinal )
		{
			[ moduleId ] = new ModuleDocumentation( (pages ?? Array.Empty<Core.Documentation.DocumentationPage>()).ToArray(), permissions.ToArray() )
		};
	}

	internal void Remove( string moduleId, Guid token )
	{
		if ( !_available() )
		{
			return;
		}

		if ( !_modules.TryGetValue( moduleId, out var current ) || current != token )
		{
			return;
		}

		var removedPermissions = _permissions.Where( pair => pair.Value == moduleId ).Select( pair => pair.Key ).ToHashSet( StringComparer.Ordinal );
		var permissions = _permissions.Where( pair => pair.Value != moduleId ).ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		// Commands in dependent modules also lose their entry points when their permission owner unloads.
		var commands = _commands.Where( pair => pair.Value.Descriptor.ModuleId != moduleId
			&& !removedPermissions.Contains( pair.Value.Descriptor.Permission ) ).ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		_publishPermissions( permissions.Keys );
		_permissions = permissions;
		_commands = commands;
		_modules.Remove( moduleId );
		_documentation.Remove( moduleId );
	}

	private void EnsureAvailable( string moduleId )
	{
		ContractGuard.Key( moduleId, nameof( moduleId ) );
		if ( moduleId == "superuser.core" || moduleId.Length > 96 )
		{
			throw new ArgumentException( "The module ID is reserved or oversized.", nameof( moduleId ) );
		}

		if ( !_available() )
		{
			throw new InvalidOperationException( "Registration requires a current authoritative runtime on the main thread." );
		}
	}
}
