using System.Text.Json;
using SboxSuperuser.Core.Storage;

namespace SboxSuperuser.Core.Props;

/// <summary>Stores the cleanup delay as {"schema":1,"delaySeconds":120}, with null delaySeconds to keep props.</summary>
internal static class PropCleanupCodec
{
	internal static PropCleanup ReadState( string stateJson )
	{
		using var json = JsonDocument.Parse( stateJson );
		var state = json.RootElement;
		StoreFormat.RequireFields( state, "schema", "delaySeconds" );
		if ( state.GetProperty( "schema" ).GetInt32() != 1 ) throw new UnsupportedStoreSchemaException();
		var seconds = state.GetProperty( "delaySeconds" );
		var delay = seconds.ValueKind == JsonValueKind.Null ? (TimeSpan?)null : TimeSpan.FromSeconds( seconds.GetInt64() );
		if ( PropCleanup.Problem( delay ) is { } problem ) throw new FormatException( problem );
		return new PropCleanup( delay );
	}

	internal static string WriteState( PropCleanup cleanup ) =>
		JsonSerializer.Serialize( new { schema = 1, delaySeconds = cleanup.Delay is { } delay ? (long?)(long)delay.TotalSeconds : null } );
}
