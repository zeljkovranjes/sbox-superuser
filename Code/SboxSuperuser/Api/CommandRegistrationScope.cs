using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser;

/// <summary>Stages one module's catalog and atomically replaces it on commit; retain until the module stops.</summary>
public sealed class CommandRegistrationScope : IModuleRegistration, IDisposable
{
	private readonly CommandRegistry _registry;
	private readonly Action<CommandRegistrationScope> _discover;
	private readonly List<RegisteredCommand> _commands = new();
	private readonly List<PermissionDescriptor> _permissions = new();
	private readonly Dictionary<Type, object> _services = new();
	private readonly Guid _token = Guid.NewGuid();
	private bool _committed;
	private bool _disposed;

	/// <summary>Gets the module that owns this registration batch.</summary>
	public string ModuleId { get; }

	internal CommandRegistrationScope( CommandRegistry registry, string moduleId, Action<CommandRegistrationScope> discover = null )
	{
		_registry = registry;
		ModuleId = ContractGuard.Key( moduleId, nameof( moduleId ) );
		_discover = discover;
	}

	/// <summary>Stages immutable metadata and an explicit typed handler.</summary>
	public void RegisterCommand( CommandDescriptor descriptor, CommandHandler handler )
	{
		EnsureStaging();
		ArgumentNullException.ThrowIfNull( descriptor );
		ArgumentNullException.ThrowIfNull( handler );
		if ( _commands.Count >= 512 )
		{
			throw new ArgumentException( "A registration batch supports at most 512 commands." );
		}

		_commands.Add( new RegisteredCommand( descriptor, handler, _token ) );
	}

	internal void RegisterAuditQuery( CommandDescriptor descriptor, CommandHandler handler )
	{
		if ( descriptor.ExecutionPolicy != CommandExecutionPolicy.ReadOnly || ModuleId != "superuser.administration"
			|| descriptor.Name is not ("audit.query" or "results") )
		{
			throw new ArgumentException( "Only built-in read-only audit queries may avoid self-invalidating journal writes." );
		}

		RegisterCommand( descriptor, handler );
		_commands[ _commands.Count - 1 ] = _commands[ _commands.Count - 1 ] with { RecordsAudit = false };
	}

	/// <summary>Stages an exact permission key without granting it to any actor.</summary>
	public void RegisterPermission( PermissionDescriptor descriptor )
	{
		EnsureStaging();
		ArgumentNullException.ThrowIfNull( descriptor );
		if ( _permissions.Count >= 4096 )
		{
			throw new ArgumentException( "A registration batch supports at most 4096 permissions." );
		}

		_permissions.Add( descriptor );
	}

	/// <summary>Stages this module's attributed methods through the engine's TypeLibrary.</summary>
	public void DiscoverCommands()
	{
		EnsureStaging();
		if ( _discover is null )
		{
			throw new NotSupportedException( "Discovery is unavailable; register descriptors and handlers explicitly." );
		}

		var count = _commands.Count;
		try
		{
			_discover( this );
		}
		catch
		{
			_commands.RemoveRange( count, _commands.Count - count );
			throw;
		}
	}

	/// <summary>Validates the complete candidate and replaces the previous module registration atomically.</summary>
	public void Commit()
	{
		EnsureStaging();
		var capabilities = new CommandCapabilities( _services );
		_registry.Replace( ModuleId, _token, _commands.Select( command => command with { Capabilities = capabilities } ).ToArray(), _permissions );
		_committed = true;
	}

	/// <summary>Approves one exact game capability type for this module's handlers; no fallback service is constructed.</summary>
	public void RegisterService<T>( T service ) where T : class
	{
		EnsureStaging();
		ArgumentNullException.ThrowIfNull( service );
		if ( _services.Count >= 32 || !_services.TryAdd( typeof( T ), service ) )
		{
			throw new ArgumentException( "Capability types must be unique, with at most 32 per module." );
		}
	}

	/// <summary>Removes this scope's active entries; disposing an older replaced scope has no effect.</summary>
	public void Dispose()
	{
		if ( _disposed )
		{
			return;
		}

		if ( _committed )
		{
			_registry.Remove( ModuleId, _token );
		}

		_disposed = true;
	}

	private void EnsureStaging()
	{
		if ( _disposed || _committed )
		{
			throw new InvalidOperationException( "This registration scope is already committed or disposed." );
		}
	}
}
