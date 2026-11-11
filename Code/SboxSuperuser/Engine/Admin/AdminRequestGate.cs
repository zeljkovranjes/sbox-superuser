namespace SboxSuperuser.Engine.Admin;

/// <summary>Per-connection and server-wide limits on admin requests; generous enough that staff working quickly never hit them.</summary>
internal sealed class AdminRequestGate
{
	internal const int BurstPerWindow = 40;
	internal const int ConcurrentPerConnection = 6;
	internal const int ConcurrentServerWide = 128;
	internal static readonly TimeSpan Window = TimeSpan.FromSeconds( 2 );

	private sealed class Budget
	{
		internal DateTimeOffset Start;
		internal int Count;
		internal int Active;
	}

	private readonly Dictionary<Guid, Budget> _budgets = new();
	private int _active;

	internal bool TryEnter( Guid connection, DateTimeOffset now )
	{
		foreach ( var id in _budgets.Where( pair => pair.Value.Active == 0 && now - pair.Value.Start > TimeSpan.FromSeconds( 10 ) )
			.Select( pair => pair.Key ).ToArray() ) _budgets.Remove( id );
		if ( !_budgets.TryGetValue( connection, out var budget ) )
		{
			if ( _budgets.Count >= 1024 ) return false;
			_budgets.Add( connection, budget = new Budget { Start = now } );
		}
		if ( now - budget.Start >= Window )
		{
			budget.Start = now;
			budget.Count = 0;
		}
		if ( _active >= ConcurrentServerWide || budget.Active >= ConcurrentPerConnection || budget.Count >= BurstPerWindow ) return false;
		budget.Active++;
		budget.Count++;
		_active++;
		return true;
	}

	internal void Exit( Guid connection )
	{
		if ( !_budgets.TryGetValue( connection, out var budget ) || budget.Active == 0 ) return;
		budget.Active--;
		_active--;
	}
}
