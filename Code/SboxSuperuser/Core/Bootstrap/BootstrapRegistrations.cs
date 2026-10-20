namespace SboxSuperuser.Core.Bootstrap;

internal sealed class BootstrapRegistrations
{
	private readonly Dictionary<Guid, (bool Editor, string Root, bool Prefixes, bool Chat)> _entries = new();

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

	/// <summary>Whether the bootstrap installs the shipped Prefix module.</summary>
	internal bool Prefixes => Count != 1 || _entries.Values.First().Prefixes;

	/// <summary>Whether players see Superuser's chat box (with prefixes) instead of the built-in one.</summary>
	internal bool PrefixedChat => Count != 1 || _entries.Values.First().Chat;

	internal void Attach( Guid id, bool allowEditorTesting, string commandRoot = "admin", bool prefixes = true, bool prefixedChat = true )
	{
		if ( id == Guid.Empty )
		{
			throw new ArgumentException( "Bootstrap identity must be explicit.", nameof( id ) );
		}

		_entries[ id ] = (allowEditorTesting, commandRoot, prefixes, prefixedChat);
	}

	internal void Detach( Guid id ) => _entries.Remove( id );

	internal void Clear() => _entries.Clear();
}
