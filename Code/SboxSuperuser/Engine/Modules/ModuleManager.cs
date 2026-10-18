using SboxSuperuser.Core.Modules;
using SboxSuperuser.Engine.Authority;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.Modules;

internal sealed partial class ModuleManager : IModuleCatalog, IDisposable
{
	private readonly Dictionary<string, ModuleEntry> _entries = new( StringComparer.Ordinal );
	private readonly CommandRegistry _registry;
	private readonly HostPolicyService _policy;
	private readonly Func<bool> _available;
	private readonly Action<Exception> _report;
	private readonly Action<CommandRegistrationScope> _discover;
	private readonly TimeSpan _deadline;
	private IReadOnlyList<string> _order = Array.Empty<string>();
	private bool _busy;
	private bool _disposed;

	internal ModuleManager( CommandRegistry registry, HostPolicyService policy, Func<bool> available, Action<Exception> report,
		Action<CommandRegistrationScope> discover = null, TimeSpan? deadline = null )
	{
		_registry = registry;
		_policy = policy;
		_available = available;
		_report = report;
		_discover = discover;
		_deadline = deadline ?? TimeSpan.FromSeconds( 5 );
		foreach ( var id in new[] { "superuser.core", "superuser.administration" } )
		{
			_entries.Add( id, new ModuleEntry( null, new ModuleDescriptor( id, "Required framework security service", 1, isRequiredSecurityModule: true ) ) );
			_registry.ReserveModule( id );
		}
		_order = _entries.Keys.ToArray();
	}

	public IReadOnlyList<ModuleStatus> Statuses
	{
		get
		{
			foreach ( var entry in _entries.Values )
			{
				Refresh( entry );
			}
			return Array.AsReadOnly( _entries.Values.OrderBy( entry => entry.Descriptor.Id, StringComparer.Ordinal )
				.Select( entry => new ModuleStatus( entry.Descriptor, entry.State, entry.Message ) ).ToArray() );
		}
	}

	internal void Install( IReadOnlyList<IAdminModule> modules )
	{
		if ( _disposed || !_available() || _busy )
		{
			throw new InvalidOperationException( "Module installation requires an idle authoritative runtime on its main thread." );
		}
		if ( modules is null || modules.Count > 126 || modules.Any( module => module is null ) )
		{
			throw new ArgumentException( "Supply at most 126 non-null modules.", nameof( modules ) );
		}
		var additions = modules.Select( module => new ModuleEntry( module, module.Descriptor
			?? throw new ArgumentException( "A module descriptor is required." ) ) ).ToArray();
		foreach ( var entry in additions )
		{
			if ( entry.Descriptor.Id.Length > 96 || entry.Descriptor.Description.Length > 2048
				|| entry.Descriptor.Dependencies.Count > 128 || _registry.HasModule( entry.Descriptor.Id ) )
			{
				throw new ArgumentException( $"Module '{entry.Descriptor.Id}' is already owned or its metadata exceeds limits." );
			}
		}
		var candidate = _entries.Values.Concat( additions ).ToArray();
		var order = ModuleGraph.Order( candidate.Select( entry => new ModuleNode( entry.Descriptor.Id, entry.Descriptor.ApiVersion,
			entry.Descriptor.Dependencies.Select( dependency => new ModuleRequirement( dependency.ModuleId, dependency.RequiredApiVersion, dependency.IsOptional ) ).ToArray() ) ).ToArray() );
		// The complete candidate validates before catalog ownership changes.
		foreach ( var entry in additions )
		{
			_entries.Add( entry.Descriptor.Id, entry );
			_registry.ReserveModule( entry.Descriptor.Id );
		}
		_order = order;
	}

	internal Task<CommandResult> StartAllAsync( CancellationToken cancellation ) => ChangeAsync( () => true, null, true, cancellation );

	internal Task<CommandResult> SetEnabledAsync( Func<ActorIdentity> authenticate, string id, bool enabled, CancellationToken cancellation )
		=> ChangeAsync( () => CanManage( authenticate() ), id, enabled, cancellation );

	private bool CanManage( ActorIdentity actor ) => _policy.IsUsable && actor is not null
		&& (actor.Kind == ActorKind.DedicatedConsole || actor.Kind == ActorKind.Player && _policy.IsOwner( actor.SubjectId ));

	private async Task<CommandResult> ChangeAsync( Func<bool> authorized, string id, bool enabled, CancellationToken cancellation )
	{
		if ( _disposed || !_available() )
		{
			return Result( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
		}
		if ( !authorized() )
		{
			return Result( CommandStatus.Denied, "Current owner or dedicated local console authority is required." );
		}
		if ( _busy )
		{
			return Result( CommandStatus.Conflict, "A module lifecycle operation is already in progress." );
		}
		if ( id is not null && !_entries.ContainsKey( id ) )
		{
			return Result( CommandStatus.NotFound, "Module is not installed." );
		}
		_busy = true;
		try
		{
			if ( !enabled )
			{
				return await DisableAsync( _entries[ id ], cancellation );
			}
			var selected = id is null ? _order : RequiredOrder( id );
			foreach ( var key in selected )
			{
				var result = await EnableAsync( _entries[ key ], authorized, cancellation );
				if ( result.Status != CommandStatus.Success )
				{
					return result;
				}
			}
			return Result( CommandStatus.Success, "Modules enabled." );
		}
		finally
		{
			_busy = false;
		}
	}

	private IReadOnlyList<string> RequiredOrder( string id )
	{
		var wanted = new HashSet<string>( StringComparer.Ordinal ) { id };
		foreach ( var key in _order.Reverse() )
		{
			if ( wanted.Contains( key ) )
			{
				foreach ( var dependency in _entries[ key ].Descriptor.Dependencies.Where( dependency => !dependency.IsOptional ) )
				{
					wanted.Add( dependency.ModuleId );
				}
			}
		}
		return _order.Where( wanted.Contains ).ToArray();
	}

	private static CommandResult Result( CommandStatus status, string message ) => new( status, message );
}
