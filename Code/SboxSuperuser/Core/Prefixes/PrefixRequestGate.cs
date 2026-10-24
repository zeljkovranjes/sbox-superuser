namespace SboxSuperuser.Core.Prefixes;

/// <summary>Bounds client snapshot retries so one connection cannot amplify them into repeated large replies.</summary>
internal sealed class PrefixRequestGate
{
	private static readonly TimeSpan Interval = TimeSpan.FromSeconds( 2 );
	private static readonly TimeSpan Retention = TimeSpan.FromSeconds( 10 );
	private const int MaximumClients = 1024;
	private readonly Dictionary<Guid, DateTimeOffset> _requests = new();

	internal bool TryEnter( Guid connection, DateTimeOffset now )
	{
		foreach ( var id in _requests.Where( pair => now - pair.Value >= Retention ).Select( pair => pair.Key ).ToArray() )
		{
			_requests.Remove( id );
		}

		if ( _requests.TryGetValue( connection, out var previous ) && now - previous < Interval ) return false;
		if ( !_requests.ContainsKey( connection ) && _requests.Count >= MaximumClients ) return false;
		_requests[ connection ] = now;
		return true;
	}
}
