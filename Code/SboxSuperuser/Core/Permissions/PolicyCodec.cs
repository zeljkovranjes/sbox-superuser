using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Permissions;

internal static class PolicyCodec
{
	internal static string WriteState( PolicyGraph policy ) => JsonSerializer.Serialize( new
	{
		policySchema = 1,
		groups = policy.Groups.Values.OrderBy( group => group.Id, StringComparer.Ordinal ).Select( group => new
		{
			id = group.Id, parents = group.Parents, permissions = WriteEntries( group.Entries ), protectionLevel = group.ProtectionLevel
		} ),
		users = policy.Users.Values.OrderBy( user => user.SteamId, StringComparer.Ordinal ).Select( user => new
		{
			steamId = user.SteamId, groups = user.Groups, permissions = WriteEntries( user.Entries )
		} )
	} );

	internal static PolicyGraph ReadPayload( string payload, long revision )
	{
		using var document = JsonDocument.Parse( payload );
		var state = document.RootElement.GetProperty( "state" );
		StoreFormat.RequireFields( state, "policySchema", "groups", "users" );
		if ( state.GetProperty( "policySchema" ).GetInt32() != 1 )
		{
			throw new UnsupportedStoreSchemaException();
		}

		var groups = new List<GroupDefinition>();
		foreach ( var group in state.GetProperty( "groups" ).EnumerateArray() )
		{
			StoreFormat.RequireFields( group, "id", "parents", "permissions", "protectionLevel" );
			groups.Add( new GroupDefinition( group.GetProperty( "id" ).GetString(), Strings( group.GetProperty( "parents" ) ),
				ReadEntries( group.GetProperty( "permissions" ) ), group.GetProperty( "protectionLevel" ).GetInt32() ) );
		}

		var users = new List<UserDefinition>();
		foreach ( var user in state.GetProperty( "users" ).EnumerateArray() )
		{
			StoreFormat.RequireFields( user, "steamId", "groups", "permissions" );
			users.Add( new UserDefinition( user.GetProperty( "steamId" ).GetString(), Strings( user.GetProperty( "groups" ) ),
				ReadEntries( user.GetProperty( "permissions" ) ) ) );
		}

		return new PolicyGraph( revision, groups, users );
	}

	internal static string WriteOwners( OwnerSet owners ) => JsonSerializer.Serialize( new { ownerSchema = 1, owners = owners.Ids } );

	internal static OwnerSet ReadOwners( string payload )
	{
		using var document = JsonDocument.Parse( payload );
		var state = document.RootElement.GetProperty( "state" );
		StoreFormat.RequireFields( state, "ownerSchema", "owners" );
		if ( state.GetProperty( "ownerSchema" ).GetInt32() != 1 )
		{
			throw new UnsupportedStoreSchemaException();
		}

		return new OwnerSet( Strings( state.GetProperty( "owners" ) ) );
	}

	private static IEnumerable<string> Strings( JsonElement array ) => array.EnumerateArray().Select( value => value.GetString() ).ToArray();

	private static IReadOnlyDictionary<string, string> WriteEntries( IReadOnlyDictionary<string, int> entries )
	{
		return entries.OrderBy( entry => entry.Key, StringComparer.Ordinal )
			.ToDictionary( entry => entry.Key, entry => entry.Value == 2 ? "Allow" : "Deny", StringComparer.Ordinal );
	}

	private static IEnumerable<KeyValuePair<string, int>> ReadEntries( JsonElement entries )
	{
		return entries.EnumerateObject().Select( entry => new KeyValuePair<string, int>( entry.Name, entry.Value.GetString() switch
		{
			"Allow" => 2,
			"Deny" => 1,
			_ => throw new FormatException( "Stored policy entries must be Allow or Deny; Unset is removal." )
		} ) ).ToArray();
	}
}
