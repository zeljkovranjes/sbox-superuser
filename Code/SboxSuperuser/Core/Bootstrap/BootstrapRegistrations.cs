namespace SboxSuperuser.Core.Bootstrap;

internal sealed class BootstrapRegistrations
{
	private readonly Dictionary<Guid, bool> _entries = new();

	internal int Count => _entries.Count;

	internal bool AllowsEditorTesting => Count == 1 && _entries.Values.First();

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
