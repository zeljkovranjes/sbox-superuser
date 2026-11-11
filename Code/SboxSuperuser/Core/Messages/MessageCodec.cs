using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Messages;

/// <summary>Stores only the templates a server has changed, as {"schema":1,"templates":{key:template}}.</summary>
internal static class MessageCodec
{
	/// <summary>Reads the state object itself, without the stored document's envelope.</summary>
	internal static IReadOnlyDictionary<string, string> ReadState( string stateJson )
	{
		using var json = JsonDocument.Parse( stateJson );
		var state = json.RootElement;
		StoreFormat.RequireFields( state, "schema", "templates" );
		if ( state.GetProperty( "schema" ).GetInt32() != 1 ) throw new UnsupportedStoreSchemaException();
		var templates = new Dictionary<string, string>( StringComparer.Ordinal );
		foreach ( var entry in state.GetProperty( "templates" ).EnumerateObject() ) templates[ entry.Name ] = entry.Value.GetString();
		return templates;
	}

	internal static string WriteState( IReadOnlyDictionary<string, string> templates ) =>
		JsonSerializer.Serialize( new { schema = 1, templates } );
}
