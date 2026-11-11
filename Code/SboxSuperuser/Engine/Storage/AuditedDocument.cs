namespace SboxSuperuser.Engine.Storage;

/// <summary>
/// One small host setting kept in the store, such as message wording or content bans: loads its state, and saves
/// changes together with an audit intent at the expected revision, the way moderation history is kept. Until it
/// loads, or after a failed read, callers keep using their built-in state.
/// </summary>
internal sealed class AuditedDocument
{
	private readonly IAdminStore _store;
	private readonly StoreKey _key;
	private readonly Func<bool> _available;
	private readonly Action<string> _apply;
	private readonly Action<Exception> _report;
	private readonly Func<DateTimeOffset> _clock;
	private readonly SemaphoreSlim _gate = new( 1, 1 );
	private long _revision;

	/// <param name="apply">Takes the stored state's JSON; throws FormatException for anything invalid.</param>
	internal AuditedDocument( IAdminStore store, StoreKey key, Func<bool> available, Action<string> apply, Action<Exception> report,
		Func<DateTimeOffset> clock = null )
	{
		_store = store;
		_key = key;
		_available = available;
		_apply = apply;
		_report = report;
		_clock = clock ?? (() => DateTimeOffset.UtcNow);
		Initialization = ReloadAsync();
	}

	internal Task Initialization { get; }
	internal StoreReadStatus Status { get; private set; } = StoreReadStatus.Unknown;
	internal bool IsUsable => _available() && Status is StoreReadStatus.Found or StoreReadStatus.Missing;

	internal void OnStorageChanged( StoreKey key )
	{
		if ( key.ModuleId != _key.ModuleId || key.DocumentId != _key.DocumentId ) return;
		Status = StoreReadStatus.Unknown;
		_ = ReloadAsync();
	}

	private async Task ReloadAsync()
	{
		await _gate.WaitAsync();
		try
		{
			var stored = await _store.ReadAsync( _key, 1, CancellationToken.None );
			if ( stored.Status == StoreReadStatus.Found )
			{
				using var json = System.Text.Json.JsonDocument.Parse( stored.Document.PayloadJson );
				_apply( json.RootElement.GetProperty( "state" ).GetRawText() );
				_revision = stored.Document.Revision;
			}
			Status = stored.Status;
		}
		catch ( Exception error )
		{
			Status = error is Core.Storage.UnsupportedStoreSchemaException ? StoreReadStatus.UnsupportedSchema : StoreReadStatus.Corrupt;
			_report( error );
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <summary>Saves new state with an audit intent for the command; applies it only once the store confirms.</summary>
	internal Task<CommandResult> SaveAsync( CommandContext context, string command, string state, string success ) =>
		WriteAsync( new[] { new AuditIntent( Guid.NewGuid(), context.RequestId, context.Actor.Kind, context.Actor.SubjectId,
			context.Origin, command, _clock(), targets: null ) }, state, success, context.Cancellation );

	/// <summary>Saves state no one asked for, such as automatic records of who connected, audited as Superuser's own work.</summary>
	internal Task<CommandResult> SaveAutomaticAsync( string state, string command ) =>
		WriteAsync( new[] { new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.InternalService, "superuser", CommandOrigin.InternalService,
			command, _clock(), targets: null ) }, state, "Saved.", CancellationToken.None );

	private async Task<CommandResult> WriteAsync( AuditIntent[] intents, string state, string success, CancellationToken cancellation )
	{
		await Initialization;
		await _gate.WaitAsync( cancellation );
		try
		{
			if ( !IsUsable ) return new CommandResult( CommandStatus.StorageUnavailable, "This setting's storage is unavailable." );
			var result = await _store.WriteAsync( new PolicyTransaction( state, intents ).CreateWriteRequest( _key, _revision ), cancellation );
			if ( result.Status != StoreWriteStatus.Committed )
			{
				if ( result.Status is StoreWriteStatus.Unknown or StoreWriteStatus.Conflict or StoreWriteStatus.Corrupt ) Status = StoreReadStatus.Unknown;
				return new CommandResult( result.Status == StoreWriteStatus.Unknown ? CommandStatus.Unknown : CommandStatus.StorageUnavailable,
					"The change was not confirmed." );
			}
			_apply( state );
			_revision++;
			Status = StoreReadStatus.Found;
			return new CommandResult( CommandStatus.Success, success );
		}
		finally
		{
			_gate.Release();
		}
	}
}
