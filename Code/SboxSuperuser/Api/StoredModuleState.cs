using System.Text.Json;

namespace SboxSuperuser;

/// <summary>A host module's validated state and audit intent, persisted together using expected revisions.</summary>
public sealed class StoredModuleState<T> where T : class
{
	readonly IAdminStore _store;
	readonly StoreKey _key;
	readonly Action<T> _validate;
	long _revision;
	public T Value { get; private set; }
	public bool Ready { get; private set; }

	/// <summary>Creates an adapter; load it before allowing actions. Validation must reject malformed state.</summary>
	public StoredModuleState( IAdminStore store, StoreKey key, Action<T> validate )
	{
		_store = store ?? throw new ArgumentNullException( nameof(store) );
		_key = key;
		_validate = validate ?? throw new ArgumentNullException(nameof(validate));
	}

	/// <summary>Loads existing state or explicit defaults. Corruption throws and never becomes an empty policy.</summary>
	public async Task LoadAsync( T defaults, CancellationToken cancellation )
	{
		Ready = false;
		var read = await _store.ReadAsync( _key, 1, cancellation );
		if ( read.Status == StoreReadStatus.Missing ) { _validate(defaults); Value = defaults; _revision = 0; }
		else if ( read.Status == StoreReadStatus.Found )
		{
			using var json = JsonDocument.Parse( read.Document.PayloadJson );
			var state = json.RootElement.GetProperty("state").GetProperty("value").Deserialize<T>();
			_validate(state);
			Value = state;
			_revision = read.Document.Revision;
		}
		else throw new InvalidOperationException( $"Cannot load {_key.ModuleId}/{_key.DocumentId}: {read.Status}." );
		Ready = true;
	}

	/// <summary>Commits validated replacement state after rechecking authority; uncertain writes disable this adapter.</summary>
	public async Task<CommandResult> SaveAsync( T candidate, CommandContext context, string command, Func<bool> authorized )
	{
		if ( !Ready ) return new(CommandStatus.StorageUnavailable,"Module storage is unavailable.");
		_validate(candidate);
		if ( !authorized() ) return new(CommandStatus.Denied,"Current permission is required.");
		var revision = _revision;
		var intent = new AuditIntent(Guid.NewGuid(),context.RequestId,context.Actor.Kind,context.Actor.SubjectId,context.Origin,command,DateTimeOffset.UtcNow);
		var request = new PolicyTransaction(JsonSerializer.Serialize(new { value = candidate }),new[]{intent}).CreateWriteRequest(_key,revision);
		Ready = false;
		var write = await _store.WriteAsync(request,context.Cancellation);
		if ( write.Status != StoreWriteStatus.Committed )
		{
			Ready = false;
			return new(CommandStatus.StorageUnavailable,$"Module write was not confirmed ({write.Status}); reload the module after checking storage.");
		}
		Ready = true;
		Value = candidate;
		_revision = revision + 1;
		return new(CommandStatus.Success,"Change saved.");
	}
}
