using SboxSuperuser.Core.Moderation;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Engine.Moderation;

internal sealed partial class HostModerationService
{
	private readonly IAdminStore _store;
	private readonly Func<bool> _available;
	private readonly Func<CommandContext, string, bool> _canUse;
	private readonly Func<DateTimeOffset> _clock;
	private readonly Action<Exception> _report;
	private readonly SemaphoreSlim _gate = new( 1, 1 );
	private ModerationLedger _ledger = new( 0, Array.Empty<ModerationEntry>() );
	internal static StoreKey Key { get; } = new( "moderation", "history" );
	internal Task Initialization { get; }
	internal StoreReadStatus Status { get; private set; } = StoreReadStatus.Unknown;
	internal bool IsUsable => _available() && Status is StoreReadStatus.Found or StoreReadStatus.Missing;

	internal HostModerationService( IAdminStore store, Func<bool> available, Func<CommandContext, string, bool> canUse,
		Action<Exception> report, Func<DateTimeOffset> clock = null )
	{
		_store = store;
		_available = available;
		_canUse = canUse;
		_report = report;
		_clock = clock ?? (() => DateTimeOffset.UtcNow);
		Initialization = ReloadAsync();
	}

	internal void OnStorageChanged( StoreKey key )
	{
		if ( key.ModuleId != Key.ModuleId || key.DocumentId != Key.DocumentId ) return;
		Status = StoreReadStatus.Unknown;
		_ = ReloadAsync();
	}

	private async Task ReloadAsync()
	{
		await _gate.WaitAsync();
		try
		{
			var stored = await _store.ReadAsync( Key, 1, CancellationToken.None );
			if ( stored.Status == StoreReadStatus.Missing && _ledger.Revision > 0 )
			{
				Status = StoreReadStatus.Corrupt;
				return;
			}
			if ( stored.Status == StoreReadStatus.Found )
			{
				var ledger = ModerationCodec.Read( stored.Document.PayloadJson, stored.Document.Revision );
				if ( ledger.Revision < _ledger.Revision ) throw new FormatException( "Moderation revision moved backward." );
				_ledger = ledger;
			}
			Status = stored.Status;
		}
		catch ( Exception error )
		{
			Status = error is UnsupportedStoreSchemaException ? StoreReadStatus.UnsupportedSchema : StoreReadStatus.Corrupt;
			_report( error );
		}
		finally
		{
			_gate.Release();
		}
	}

	internal CommandResult CheckRestriction( string target, ModerationKind kind )
	{
		if ( !IsUsable ) return Unavailable();
		return _ledger.Active( target, kind, _clock() ) is null
			? new CommandResult( CommandStatus.Success, "No active restriction." )
			: new CommandResult( CommandStatus.Denied, kind == ModerationKind.Ban ? "Connection denied by server moderation." : "Text chat is muted by server moderation." );
	}

	internal CommandResult History( CommandContext context, string target, int page )
	{
		if ( !_canUse( context, "admin.players.history" ) ) return Denied();
		if ( !IsUsable ) return Unavailable();
		var now = _clock();
		var rows = _ledger.Entries.Where( entry => entry.Target == target ).Reverse().Skip( (page - 1) * 8 ).Take( 9 ).ToArray();
		var data = rows.Take( 8 ).Select( (entry, index) => new KeyValuePair<string, string>( "row" + (index + 1),
			$"{entry.Id} {entry.Kind} {entry.CreatedUtc:O} actor={entry.ActorKind}:{entry.ActorSubject} expires={entry.ExpiresUtc?.ToString( "O" ) ?? "none"} revokes={entry.Revokes?.ToString() ?? "none"} active={_ledger.Active( target, entry.Kind, now )?.Id == entry.Id} reason={entry.Reason}" ) )
			.ToDictionary( pair => pair.Key, pair => pair.Value );
		data[ "page" ] = page.ToString( System.Globalization.CultureInfo.InvariantCulture );
		data[ "more" ] = (rows.Length > 8).ToString();
		return new CommandResult( CommandStatus.Success, "Moderation records; kick intents require the correlated audit outcome to confirm execution.", data );
	}

	private static CommandResult Unavailable() => new( CommandStatus.StorageUnavailable, "Moderation storage is unavailable; access fails closed. Inspect host recovery diagnostics." );
	private static CommandResult Denied() => new( CommandStatus.Denied, "Current moderation permission is required." );
}
