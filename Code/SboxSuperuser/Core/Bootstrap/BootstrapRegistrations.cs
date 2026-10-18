namespace SboxSuperuser.Core.Bootstrap;

internal sealed class BootstrapRegistrations
{
	private readonly Dictionary<Guid, bool> _entries = new();

	internal int Count => _entries.Count;

	internal bool AllowsEditorTesting
	{
		get
		{
			if ( Count != 1 )
			{
				return false;
			}

			foreach ( var entry in _entries )
			{
				return entry.Value;
			}

			return false;
		}
	}

	internal void Attach( Guid id, bool allowEditorTesting )
	{
		if ( id == Guid.Empty )
		{
			throw new ArgumentException( "Bootstrap identity must be explicit.", nameof( id ) );
		}

		_entries[ id ] = allowEditorTesting;
	}

	internal void Detach( Guid id ) => _entries.Remove( id );

	internal void Clear() => _entries.Clear();
}
