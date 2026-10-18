namespace SboxSuperuser.Core.Bootstrap;

internal sealed class BootstrapRegistrations
{
	private readonly Dictionary<Guid, (bool Editor, string Root)> _entries = new();

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
				return entry.Value.Editor;
			}

			return false;
		}
	}

	internal string CommandRoot => Count == 1 ? _entries.Values.First().Root : "admin";

	internal void Attach( Guid id, bool allowEditorTesting, string commandRoot = "admin" )
	{
		if ( id == Guid.Empty )
		{
			throw new ArgumentException( "Bootstrap identity must be explicit.", nameof( id ) );
		}

		_entries[ id ] = (allowEditorTesting, commandRoot);
	}

	internal void Detach( Guid id ) => _entries.Remove( id );

	internal void Clear() => _entries.Clear();
}
