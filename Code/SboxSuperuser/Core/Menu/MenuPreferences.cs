using System.Text.Json;

namespace SboxSuperuser.Core.Menu;

/// <summary>Moderator conveniences kept on their own machine: recently used reasons and the last duration.</summary>
internal sealed class MenuPreferences
{
	internal const int SchemaVersion = 1;
	internal const int MaximumCharacters = 8192;
	internal const int MaximumReasons = 8;
	private const int MaximumReasonLength = 256;
	private const int MaximumDurationLength = 32;

	private readonly List<string> _reasons = new();

	internal IReadOnlyList<string> RecentReasons => _reasons;
	internal string LastDuration { get; private set; } = "";

	/// <summary>Raised by every change, so the caller knows the preferences need saving.</summary>
	internal int Revision { get; private set; }

	/// <summary>Records a confirmed action: its reason moves to the front of the list and its duration is kept.</summary>
	internal void Remember( string reason, string duration )
	{
		reason = reason?.Trim() ?? "";
		if ( IsValid( reason, MaximumReasonLength ) && (_reasons.Count == 0 || _reasons[ 0 ] != reason) )
		{
			_reasons.Remove( reason );
			_reasons.Insert( 0, reason );
			if ( _reasons.Count > MaximumReasons ) _reasons.RemoveAt( _reasons.Count - 1 );
			Revision++;
		}

		duration = duration?.Trim() ?? "";
		if ( IsValid( duration, MaximumDurationLength ) && duration != LastDuration )
		{
			LastDuration = duration;
			Revision++;
		}
	}

	internal string Write() => JsonSerializer.Serialize( new
	{
		schemaVersion = SchemaVersion,
		recentReasons = _reasons,
		lastDuration = LastDuration
	} );

	internal static MenuPreferences Parse( string json )
	{
		if ( string.IsNullOrEmpty( json ) || json.Length > MaximumCharacters )
			throw new ArgumentException( "Menu preferences are empty or too large." );

		using var document = JsonDocument.Parse( json );
		var root = document.RootElement;
		if ( root.ValueKind != JsonValueKind.Object
			|| !root.TryGetProperty( "schemaVersion", out var schema ) || !schema.TryGetInt32( out var version ) || version != SchemaVersion
			|| !root.TryGetProperty( "recentReasons", out var reasons ) || reasons.ValueKind != JsonValueKind.Array
			|| reasons.GetArrayLength() > MaximumReasons
			|| !root.TryGetProperty( "lastDuration", out var duration ) || duration.ValueKind != JsonValueKind.String )
		{
			throw new ArgumentException( $"Expected menu preferences schemaVersion {SchemaVersion}." );
		}

		var preferences = new MenuPreferences();
		foreach ( var reason in reasons.EnumerateArray() )
		{
			var text = reason.ValueKind == JsonValueKind.String ? reason.GetString() : null;
			if ( !IsValid( text, MaximumReasonLength ) || preferences._reasons.Contains( text ) )
				throw new ArgumentException( "Recent reasons must be distinct printable text." );
			preferences._reasons.Add( text );
		}

		var last = duration.GetString();
		if ( last.Length > 0 && !IsValid( last, MaximumDurationLength ) ) throw new ArgumentException( "Invalid last duration." );
		preferences.LastDuration = last;
		return preferences;
	}

	private static bool IsValid( string text, int maximumLength ) =>
		!string.IsNullOrEmpty( text ) && text.Length <= maximumLength && !text.Any( char.IsControl );
}
