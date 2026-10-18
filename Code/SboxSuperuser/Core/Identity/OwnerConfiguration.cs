using System.Text.Json;

namespace SboxSuperuser.Core.Identity;

internal sealed class OwnerConfiguration
{
	internal const int MaximumCharacters = 65536;
	private readonly HashSet<string> _owners;

	internal int Count => _owners.Count;

	private OwnerConfiguration( HashSet<string> owners )
	{
		_owners = owners;
	}

	internal bool Contains( string steamId ) => _owners.Contains( steamId );

	internal static OwnerConfiguration Parse( string json )
	{
		if ( string.IsNullOrEmpty( json ) || json.Length > MaximumCharacters )
		{
			throw new ArgumentException( "Owner configuration is empty or too large." );
		}

		using var document = JsonDocument.Parse( json );
		if ( document.RootElement.ValueKind != JsonValueKind.Object )
		{
			throw new ArgumentException( "Owner configuration must be an object." );
		}

		var names = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var property in document.RootElement.EnumerateObject() )
		{
			if ( !names.Add( property.Name ) || property.Name is not ("schemaVersion" or "owners") )
			{
				throw new ArgumentException( "Owner configuration contains duplicate or unknown fields." );
			}
		}

		if ( !document.RootElement.TryGetProperty( "schemaVersion", out var schema )
			|| schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32( out var version ) || version != 1
			|| !document.RootElement.TryGetProperty( "owners", out var owners )
			|| owners.ValueKind != JsonValueKind.Array || owners.GetArrayLength() > 128 )
		{
			throw new ArgumentException( "Expected schemaVersion 1 and at most 128 explicit owner IDs." );
		}

		var ids = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var owner in owners.EnumerateArray() )
		{
			if ( owner.ValueKind != JsonValueKind.String )
			{
				throw new ArgumentException( "Owner IDs must be decimal strings." );
			}

			var id = SteamIdentity.Validate( owner.GetString(), "owners" );
			if ( !ids.Add( id ) )
			{
				throw new ArgumentException( "Owner IDs must be unique." );
			}
		}

		return new OwnerConfiguration( ids );
	}
}
