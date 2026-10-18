using SboxSuperuser.Core.Permissions;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	private readonly IAdminStore _store;
	private readonly Func<bool> _available;
	private readonly Action<Exception> _report;
	private readonly OwnerSet _bootstrapOwners;
	private readonly StoreReadStatus _bootstrapOwnerStatus;
	private readonly SemaphoreSlim _gate = new( 1, 1 );
	private readonly PolicyResolver _resolver;
	private OwnerSet _owners;
	private long _ownerRevision;
	private bool _managedOwners;

	internal Task Initialization { get; }
	internal StoreReadStatus PolicyStatus { get; private set; } = StoreReadStatus.Unknown;
	internal StoreReadStatus OwnerStatus { get; private set; } = StoreReadStatus.Unknown;
	internal StoreWriteStatus LastWriteStatus { get; private set; } = StoreWriteStatus.Unknown;
	internal long PolicyRevision => _resolver.Policy.Revision;
	internal long OwnerRevision => _ownerRevision;
	internal bool IsUsable => _available() && (PolicyStatus is StoreReadStatus.Found or StoreReadStatus.Missing)
		&& (OwnerStatus is StoreReadStatus.Found or StoreReadStatus.Missing);
	internal int OwnerCount => _owners?.Ids.Count ?? 0;
	internal string OwnerSource => _managedOwners ? "managed" : "bootstrap";
	internal static StoreKey PolicyKey { get; } = new( "security", "policy" );
	internal static StoreKey OwnersKey { get; } = new( "security", "owners" );

	internal HostPolicyService( IAdminStore store, IEnumerable<string> bootstrapOwners,
		StoreReadStatus bootstrapOwnerStatus, Func<bool> available, Action<Exception> report )
	{
		_store = store;
		_available = available;
		_report = report;
		_bootstrapOwners = new OwnerSet( bootstrapOwners );
		_bootstrapOwnerStatus = bootstrapOwnerStatus;
		_owners = _bootstrapOwners;
		var defaults = PolicySnapshot.CreateDefault().Graph;
		var permissions = defaults.Groups.Values.SelectMany( group => group.Entries.Keys )
			.Concat( new[] { "admin.permissions.explain", "admin.security.manage", "admin.audit.read" } )
			.Distinct( StringComparer.Ordinal );
		_resolver = new PolicyResolver( defaults, permissions );
		Initialization = LoadAsync( CancellationToken.None );
	}

	internal async Task<CommandResult> ReloadAsync( CancellationToken cancellation )
	{
		return await SerializedAsync( async () =>
		{
			await LoadAsync( cancellation );
			return new CommandResult( IsUsable ? CommandStatus.Success : CommandStatus.StorageUnavailable,
				IsUsable ? "Validated authority state reloaded." : "Authority storage is degraded; existing files preserved." );
		}, cancellation );
	}

	private async Task LoadAsync( CancellationToken cancellation )
	{
		var policy = await _store.ReadAsync( PolicyKey, 1, cancellation );
		PolicyStatus = policy.Status;
		try
		{
			if ( policy.Status == StoreReadStatus.Found )
			{
				var graph = PolicyCodec.ReadPayload( policy.Document.PayloadJson, policy.Document.Revision );
				if ( graph.Revision > _resolver.Policy.Revision )
				{
					_resolver.ReplacePolicy( graph );
				}
				else if ( graph.Revision != _resolver.Policy.Revision
					|| PolicyCodec.WriteState( graph ) != PolicyCodec.WriteState( _resolver.Policy ) )
				{
					PolicyStatus = StoreReadStatus.Corrupt;
				}
			}
			else if ( policy.Status == StoreReadStatus.Missing && _resolver.Policy.Revision > 0 )
			{
				PolicyStatus = StoreReadStatus.Corrupt;
			}
		}
		catch ( Exception error )
		{
			PolicyStatus = error is UnsupportedStoreSchemaException ? StoreReadStatus.UnsupportedSchema : StoreReadStatus.Corrupt;
			_report( error );
		}

		var owners = await _store.ReadAsync( OwnersKey, 1, cancellation );
		OwnerStatus = owners.Status;
		try
		{
			if ( owners.Status == StoreReadStatus.Found )
			{
				var candidate = PolicyCodec.ReadOwners( owners.Document.PayloadJson );
				if ( owners.Document.Revision < _ownerRevision
					|| owners.Document.Revision == _ownerRevision && !_owners.Ids.SequenceEqual( candidate.Ids ) )
				{
					OwnerStatus = StoreReadStatus.Corrupt;
					return;
				}

				_owners = candidate;
				_ownerRevision = owners.Document.Revision;
				_managedOwners = true;
			}
			else if ( owners.Status == StoreReadStatus.Missing && !_managedOwners )
			{
				_owners = _bootstrapOwners;
				OwnerStatus = _bootstrapOwnerStatus;
			}
			else
			{
				_managedOwners = true;
				OwnerStatus = owners.Status == StoreReadStatus.Missing ? StoreReadStatus.Corrupt : owners.Status;
			}
		}
		catch ( Exception error )
		{
			_managedOwners = true;
			OwnerStatus = error is UnsupportedStoreSchemaException ? StoreReadStatus.UnsupportedSchema : StoreReadStatus.Corrupt;
			_report( error );
		}
	}

	private async Task<CommandResult> SerializedAsync( Func<Task<CommandResult>> action, CancellationToken cancellation )
	{
		var entered = false;
		try
		{
			await Initialization;
			await _gate.WaitAsync( cancellation );
			entered = true;
			cancellation.ThrowIfCancellationRequested();
			if ( !_available() )
			{
				return new CommandResult( CommandStatus.Unsupported, "Authority runtime is unavailable." );
			}

			return await action();
		}
		catch ( OperationCanceledException )
		{
			return new CommandResult( CommandStatus.Cancelled, "Request cancelled before a confirmed change." );
		}
		catch ( ArgumentException error )
		{
			_report( error );
			return new CommandResult( CommandStatus.InvalidArguments, "Invalid policy edit; no candidate was activated." );
		}
		catch ( Exception error )
		{
			_report( error );
			return new CommandResult( CommandStatus.Unknown, "Authority operation failed without a confirmed outcome; inspect storage." );
		}
		finally
		{
			if ( entered )
			{
				_gate.Release();
			}
		}
	}
}
