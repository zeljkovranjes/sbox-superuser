using System.Text;
using System.Text.Json;
using SboxSuperuser.Core.Identity;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Prefixes;

/// <summary>Reads and writes the stored prefix catalog and the snapshot sent to clients. Both are schema 1.</summary>
internal static class PrefixCodec
{
	internal const int Schema = 1;
	internal const int MaximumStateBytes = 48 * 1024;
	internal const int MaximumSnapshotBytes = 64 * 1024;
	internal const int MaximumSnapshotPlayers = 256;

	internal static string WriteState( PrefixCatalog catalog )
	{
		var state = JsonSerializer.Serialize( new
		{
			schema = Schema,
			prefixes = catalog.Prefixes.Select( Prefix ).ToArray(),
			groups = catalog.Groups,
			users = catalog.Users
		} );
		if ( Encoding.UTF8.GetByteCount( state ) > MaximumStateBytes )
			throw new InvalidOperationException( $"Prefixes are limited to {MaximumStateBytes / 1024} KB in total; shorten or delete some effects." );
		return state;
	}

	internal static PrefixCatalog ReadState( JsonElement state )
	{
		StoreFormat.RequireFields( state, "schema", "prefixes", "groups", "users" );
		RequireSchema( state );
		var users = Map( state.GetProperty( "users" ) );
		foreach ( var steamId in users.Keys ) SteamIdentity.Validate( steamId, "users" );
		return new PrefixCatalog( Prefixes( state.GetProperty( "prefixes" ) ), Map( state.GetProperty( "groups" ) ), users );
	}

	internal static string WriteSnapshot( PrefixSnapshot snapshot )
	{
		var text = JsonSerializer.Serialize( new
		{
			schema = Schema,
			enabled = snapshot.Enabled,
			chat = snapshot.Chat,
			prefixes = snapshot.Prefixes.Select( Prefix ).ToArray(),
			players = snapshot.Players
		} );
		if ( Encoding.UTF8.GetByteCount( text ) > MaximumSnapshotBytes ) throw new InvalidOperationException( "The prefix snapshot is too large to send." );
		return text;
	}

	internal static PrefixSnapshot ReadSnapshot( string text )
	{
		if ( text is null || text.Length > MaximumSnapshotBytes ) throw new FormatException( "The prefix snapshot is too large." );
		using var json = JsonDocument.Parse( text );
		var root = json.RootElement;
		StoreFormat.RequireFields( root, "schema", "enabled", "chat", "prefixes", "players" );
		RequireSchema( root );
		var players = Map( root.GetProperty( "players" ) );
		if ( players.Count > MaximumSnapshotPlayers ) throw new FormatException( "Too many players in the prefix snapshot." );
		var prefixes = Prefixes( root.GetProperty( "prefixes" ) );
		if ( prefixes.Length > PrefixCatalog.MaximumPrefixes ) throw new FormatException( "Too many prefixes." );
		return new PrefixSnapshot( root.GetProperty( "enabled" ).GetBoolean(), root.GetProperty( "chat" ).GetBoolean(), prefixes, players );
	}

	/// <summary>Whether two definitions would store identically.</summary>
	internal static bool Same( PrefixDefinition first, PrefixDefinition second ) =>
		JsonSerializer.Serialize( Prefix( first ) ) == JsonSerializer.Serialize( Prefix( second ) );

	private static object Prefix( PrefixDefinition prefix ) => new
	{
		id = prefix.Id,
		name = prefix.Name,
		tag = prefix.Tag,
		template = prefix.Template,
		tagStyle = Style( prefix.TagStyle ),
		nameStyle = Style( prefix.NameStyle ),
		palette = prefix.Palette
	};

	private static object Style( PrefixStyle style ) => new { kind = style.Kind.ToString(), value = style.Value };

	private static PrefixDefinition[] Prefixes( JsonElement array )
	{
		if ( array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > PrefixCatalog.MaximumPrefixes )
			throw new FormatException( "Expected at most 64 prefixes." );
		return array.EnumerateArray().Select( ReadPrefix ).ToArray();
	}

	private static PrefixDefinition ReadPrefix( JsonElement prefix )
	{
		StoreFormat.RequireFields( prefix, "id", "name", "tag", "template", "tagStyle", "nameStyle", "palette" );
		var palette = prefix.GetProperty( "palette" );
		if ( palette.ValueKind != JsonValueKind.Array || palette.GetArrayLength() > PrefixDefinition.MaximumPaletteColors )
			throw new FormatException( "Invalid palette." );
		return PrefixDefinition.Create( prefix.GetProperty( "id" ).GetString(), prefix.GetProperty( "name" ).GetString(),
			prefix.GetProperty( "tag" ).GetString(), prefix.GetProperty( "template" ).GetString(),
			ReadStyle( prefix.GetProperty( "tagStyle" ) ), ReadStyle( prefix.GetProperty( "nameStyle" ) ),
			palette.EnumerateArray().Select( color => color.GetString() ).ToArray() );
	}

	private static PrefixStyle ReadStyle( JsonElement style )
	{
		StoreFormat.RequireFields( style, "kind", "value" );
		var name = style.GetProperty( "kind" ).GetString();
		if ( !Enum.TryParse<PrefixStyleKind>( name, out var kind ) || kind.ToString() != name ) throw new FormatException( "Unknown style kind." );
		var value = style.GetProperty( "value" ).GetString();
		// A preset removed by a later version draws plain instead of making the whole catalog unreadable.
		if ( kind == PrefixStyleKind.Preset && Effects.EffectPresets.Find( value ) is null ) return PrefixStyle.Plain;
		return PrefixStyle.Create( kind, value );
	}

	private static Dictionary<string, string> Map( JsonElement value )
	{
		if ( value.ValueKind != JsonValueKind.Object ) throw new FormatException( "Expected an object." );
		var map = new Dictionary<string, string>( StringComparer.Ordinal );
		foreach ( var property in value.EnumerateObject() )
		{
			if ( !map.TryAdd( property.Name, property.Value.GetString() ?? throw new FormatException( "Expected text." ) ) )
				throw new FormatException( "Duplicate assignment." );
		}
		return map;
	}

	private static void RequireSchema( JsonElement value )
	{
		if ( value.GetProperty( "schema" ).GetInt32() != Schema ) throw new UnsupportedStoreSchemaException();
	}
}
