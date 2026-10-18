using SboxSuperuser.Core.Audit;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Audit;

internal sealed partial class HostAuditService : IAuditWriter
{
	internal static StoreKey Key { get; } = new( "audit", "journal" );
	private readonly IAdminStore _store;
	private readonly IAuditRetention _retention;
	private readonly Func<bool> _available;
	private readonly Func<DateTimeOffset> _clock;
	private readonly Action<Exception> _report;
	private readonly SemaphoreSlim _gate = new( 1, 1 );
	private readonly HashSet<Guid> _active = new();
	private AuditJournal _journal = new();
	private long _revision;
	private int _pending;
	private bool _writeFailed;
	private bool _retentionFailed;
	private bool _retentionUnavailable;
	private bool _retentionPending;
	private StoreReadStatus _readStatus = StoreReadStatus.Unknown;
	private StoreWriteStatus _writeStatus = StoreWriteStatus.Unknown;

	internal Task Initialization { get; }
	internal bool IsDegraded => _readStatus is not (StoreReadStatus.Found or StoreReadStatus.Missing)
		|| _writeFailed || _retentionFailed || _retentionPending;

	internal HostAuditService( IAdminStore store, Func<bool> available, Action<Exception> report, Func<DateTimeOffset> clock = null )
	{
		_store = store;
		_retention = store as IAuditRetention;
		_available = available;
		_report = report;
		_clock = clock ?? (() => DateTimeOffset.UtcNow);
		Initialization = MaintainAutomaticAsync( CancellationToken.None );
	}

	internal AuditDiagnostics Diagnostics() => new( IsDegraded, _readStatus, _writeStatus, _retentionPending, _revision,
		_journal.Records.Count, new AuditRetentionOptions( _journal.MaximumRecords, _journal.RetentionDays ) );

	internal void OnStorageChanged( StoreKey key )
	{
		if ( key.ModuleId == Key.ModuleId && key.DocumentId == Key.DocumentId )
		{
			// Raw recovery invalidates observed health. Every query/write reloads; idle maintenance also validates it.
			_readStatus = StoreReadStatus.Unknown;
		}
	}

	private async Task<bool> LoadAsync( CancellationToken cancellation )
	{
		try
		{
			var stored = await _store.ReadAsync( Key, 1, cancellation );
			_readStatus = stored.Status;
			if ( stored.Status == StoreReadStatus.Missing )
			{
				if ( _revision != 0 )
				{
					_readStatus = StoreReadStatus.Corrupt;
				}
				return _revision == 0;
			}

			if ( stored.Status != StoreReadStatus.Found )
			{
				return false;
			}

			var candidate = AuditFormat.ReadPayload( stored.Document.PayloadJson );
			if ( stored.Document.Revision < _revision || stored.Document.Revision == _revision
				&& AuditFormat.WriteState( candidate ) != AuditFormat.WriteState( _journal ) )
			{
				_readStatus = StoreReadStatus.Corrupt;
				return false;
			}

			_journal = candidate;
			_revision = stored.Document.Revision;
			return true;
		}
		catch ( OperationCanceledException )
		{
			_readStatus = StoreReadStatus.Cancelled;
			return false;
		}
		catch ( Exception error )
		{
			_readStatus = error is UnsupportedStoreSchemaException ? StoreReadStatus.UnsupportedSchema
				: error is IOException or UnauthorizedAccessException ? StoreReadStatus.Unavailable : StoreReadStatus.Corrupt;
			_report( error );
			return false;
		}
	}

	private async Task<T> SerializedAsync<T>( Func<Task<T>> action, Func<CommandStatus, T> failed, CancellationToken cancellation, bool completion = false )
	{
		if ( !_available() )
		{
			return failed( CommandStatus.StorageUnavailable );
		}

		if ( Interlocked.Increment( ref _pending ) > (completion ? 128 : 64) )
		{
			Interlocked.Decrement( ref _pending );
			return failed( CommandStatus.Conflict );
		}

		var entered = false;
		try
		{
			await _gate.WaitAsync( cancellation );
			entered = true;
			cancellation.ThrowIfCancellationRequested();
			return !_available() ? failed( CommandStatus.StorageUnavailable ) : await action();
		}
		catch ( OperationCanceledException )
		{
			return failed( CommandStatus.Cancelled );
		}
		catch ( Exception error )
		{
			_writeFailed = true;
			_report( error );
			return failed( CommandStatus.StorageUnavailable );
		}
		finally
		{
			if ( entered )
			{
				_gate.Release();
			}
			Interlocked.Decrement( ref _pending );
		}
	}
}
