using System.Text.Json;

namespace SboxSuperuser.Core.Menu;

/// <summary>
/// Which admin menu windows are open or pinned and where they sit. Pinned windows stay on screen while
/// the menu is closed. The layout belongs to the local player and is saved on their machine.
/// </summary>
internal sealed class MenuLayout
{
	internal const int SchemaVersion = 1;
	internal const int MaximumCharacters = 8192;
	private const int MaximumWindows = 16;
	private const int MaximumNameLength = 32;

	// A restored window keeps at least this much of itself on screen so it can still be grabbed.
	private const float VisibleEdge = 48;

	private readonly Dictionary<string, MenuWindowLayout> _windows = new( StringComparer.Ordinal );

	/// <summary>Raised by every change, so the caller knows the layout needs saving.</summary>
	internal int Revision { get; private set; }

	internal bool HasPinned => _windows.Values.Any( window => window.Pinned );

	internal MenuWindowLayout Window( string name )
	{
		if ( _windows.TryGetValue( name, out var window ) ) return window;
		ValidateName( name );
		if ( _windows.Count >= MaximumWindows ) throw new ArgumentException( "Too many menu windows." );
		window = new MenuWindowLayout();
		_windows[ name ] = window;
		return window;
	}

	/// <summary>Open windows show while the menu is open; only pinned windows show while it is closed.</summary>
	internal bool IsVisible( string name, bool menuOpen )
	{
		if ( !_windows.TryGetValue( name, out var window ) ) return false;
		return menuOpen ? window.Open : window.Pinned;
	}

	internal void SetOpen( string name, bool open )
	{
		var window = Window( name );
		if ( window.Open == open ) return;
		window.Open = open;
		// A closed window cannot stay pinned on screen.
		if ( !open ) window.Pinned = false;
		Revision++;
	}

	internal void TogglePin( string name )
	{
		var window = Window( name );
		window.Pinned = !window.Pinned;
		if ( window.Pinned ) window.Open = true;
		Revision++;
	}

	internal void SetBounds( string name, float x, float y, float width, float height )
	{
		var window = Window( name );
		if ( window.HasBounds && window.X == x && window.Y == y && window.Width == width && window.Height == height ) return;
		window.HasBounds = true;
		window.X = x;
		window.Y = y;
		window.Width = width;
		window.Height = height;
		Revision++;
	}

	/// <summary>Pulls saved windows back on screen after the resolution shrank.</summary>
	internal void FitTo( float screenWidth, float screenHeight )
	{
		foreach ( var window in _windows.Values.Where( window => window.HasBounds ) )
		{
			window.Width = Math.Min( window.Width, screenWidth );
			window.Height = Math.Min( window.Height, screenHeight );
			window.X = Math.Clamp( window.X, VisibleEdge - window.Width, Math.Max( 0, screenWidth - VisibleEdge ) );
			window.Y = Math.Clamp( window.Y, 0, Math.Max( 0, screenHeight - VisibleEdge ) );
		}
	}

	internal string Write() => JsonSerializer.Serialize( new
	{
		schemaVersion = SchemaVersion,
		windows = _windows.ToDictionary( entry => entry.Key, entry => new
		{
			open = entry.Value.Open,
			pinned = entry.Value.Pinned,
			bounds = entry.Value.HasBounds
				? new[] { entry.Value.X, entry.Value.Y, entry.Value.Width, entry.Value.Height }
				: null
		} )
	} );

	internal static MenuLayout Parse( string json )
	{
		if ( string.IsNullOrEmpty( json ) || json.Length > MaximumCharacters )
			throw new ArgumentException( "Menu layout is empty or too large." );

		using var document = JsonDocument.Parse( json );
		var root = document.RootElement;
		if ( root.ValueKind != JsonValueKind.Object
			|| !root.TryGetProperty( "schemaVersion", out var schema ) || !schema.TryGetInt32( out var version ) || version != SchemaVersion
			|| !root.TryGetProperty( "windows", out var windows ) || windows.ValueKind != JsonValueKind.Object )
		{
			throw new ArgumentException( $"Expected menu layout schemaVersion {SchemaVersion} with a windows object." );
		}

		var layout = new MenuLayout();
		foreach ( var entry in windows.EnumerateObject() )
		{
			var window = layout.Window( entry.Name );
			window.Open = ReadBool( entry.Value, "open" );
			window.Pinned = window.Open && ReadBool( entry.Value, "pinned" );
			if ( entry.Value.TryGetProperty( "bounds", out var bounds ) && bounds.ValueKind == JsonValueKind.Array )
				ReadBounds( window, bounds );
		}
		return layout;
	}

	private static bool ReadBool( JsonElement element, string name )
	{
		if ( !element.TryGetProperty( name, out var value ) ) return false;
		return value.ValueKind switch
		{
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			_ => throw new ArgumentException( $"Menu layout field {name} must be true or false." )
		};
	}

	private static void ReadBounds( MenuWindowLayout window, JsonElement bounds )
	{
		if ( bounds.GetArrayLength() != 4 ) throw new ArgumentException( "Menu window bounds need x, y, width and height." );
		var values = bounds.EnumerateArray().Select( value => value.GetSingle() ).ToArray();
		if ( values.Any( value => !float.IsFinite( value ) ) || values[ 2 ] <= 0 || values[ 3 ] <= 0 )
			throw new ArgumentException( "Menu window bounds must be finite with a positive size." );
		window.HasBounds = true;
		window.X = values[ 0 ];
		window.Y = values[ 1 ];
		window.Width = values[ 2 ];
		window.Height = values[ 3 ];
	}

	private static void ValidateName( string name )
	{
		if ( string.IsNullOrEmpty( name ) || name.Length > MaximumNameLength || !name.All( character => char.IsAsciiLetterLower( character ) ) )
			throw new ArgumentException( "Menu window names are short lowercase words." );
	}
}
