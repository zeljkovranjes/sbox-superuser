namespace SboxSuperuser.Engine.Admin;

internal sealed class AdminRequestGate
{
	private sealed class Budget
	{
		internal DateTimeOffset Window;
		internal int Count;
		internal int Active;
	}

	private readonly Dictionary<Guid, Budget> _budgets = new();
	private int _active;

	internal bool TryEnter( Guid connection, DateTimeOffset now )
	{
		foreach ( var id in _budgets.Where( pair => pair.Value.Active == 0 && now - pair.Value.Window > TimeSpan.FromSeconds( 10 ) )
			.Select( pair => pair.Key ).ToArray() ) _budgets.Remove( id );
		if ( !_budgets.TryGetValue( connection, out var budget ) )
		{
			if ( _budgets.Count >= 1024 ) return false;
			_budgets.Add( connection, budget = new Budget { Window = now } );
		}
		if ( now - budget.Window >= TimeSpan.FromSeconds( 2 ) )
		{
			budget.Window = now;
			budget.Count = 0;
		}
		if ( _active >= 32 || budget.Active >= 2 || budget.Count >= 8 ) return false;
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
