using SboxSuperuser.Core.Alts;
using SboxSuperuser.Engine.Storage;

namespace SboxSuperuser.Engine.Alts;

/// <summary>
/// Remembers how each player connected, including their IP address history, and links accounts that share a
/// family-share copy, IP address or party. Joins are saved at most every few seconds rather than one write per player.
/// </summary>
internal sealed class HostAltService
{
	internal static StoreKey Key { get; } = new( "alts", "ledger" );
	private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds( 10 );

	private readonly AuditedDocument _document;
	private readonly Func<TimeSpan, Task> _wait;
	private readonly Func<DateTimeOffset> _clock;
	private readonly Action<Exception> _report;
	private bool _saving;

	internal AltLedger Ledger { get; } = new();
	internal Task Initialization => _document.Initialization;

	internal HostAltService( IAdminStore store, Func<bool> available, Action<Exception> report, Func<TimeSpan, Task> wait = null, Func<DateTimeOffset> clock = null )
	{
		_wait = wait ?? Task.Delay;
		_report = report;
		_clock = clock ?? (() => DateTimeOffset.UtcNow);
		_document = new AuditedDocument( store, Key, available, Load, report, clock );
	}

	internal void OnStorageChanged( StoreKey key ) => _document.OnStorageChanged( key );

	/// <param name="owner">The family-share owner's Steam ID; the player's own when the game is theirs.</param>
	/// <param name="address">An IP address for direct connections; anything else, such as a relay, is ignored.</param>
	/// <param name="party">The Steam party they joined with, or empty.</param>
	internal void Record( string steamId, string name, string owner, string address, string party )
	{
		Ledger.Record( steamId, name, owner, IpAddress.Normalize( address ), party, _clock() );
		_ = SaveSoonAsync();
	}

	private void Load( string state ) => Ledger.Load( AltCodec.ReadState( state ) );

	// One write per burst of joins, a few seconds after the first.
	private async Task SaveSoonAsync()
	{
		if ( _saving ) return;
		_saving = true;
		try
		{
			await _wait( SaveDelay );
			var saved = await _document.SaveAutomaticAsync( AltCodec.WriteState( Ledger.Identities ), "alts.record" );
			if ( saved.Status != CommandStatus.Success ) _report( new InvalidOperationException( $"Alt-account records were not saved: {saved.Status}." ) );
		}
		finally
		{
			_saving = false;
		}
	}
}
