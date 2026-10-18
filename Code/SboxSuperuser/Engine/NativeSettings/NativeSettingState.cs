using System.Text.Json;

namespace SboxSuperuser.Engine.NativeSettings;

internal sealed record NativeSettingState( long Revision, string Desired )
{
	internal static NativeSettingState Read( NativeSettingDescriptor binding, StoreReadResult result )
	{
		if ( result.Status == StoreReadStatus.Missing ) return new( 0, null );
		if ( result.Status != StoreReadStatus.Found ) throw new InvalidOperationException( "Setting storage is unavailable." );
		using var json = JsonDocument.Parse( result.Document.PayloadJson );
		var state = json.RootElement.GetProperty( "state" );
		var names = state.EnumerateObject().Select( property => property.Name ).ToArray();
		if ( names.Length != 6 || names.Distinct( StringComparer.Ordinal ).Count() != 6
			|| state.GetProperty( "schema" ).GetInt32() != 1
			|| state.GetProperty( "module" ).GetString() != "superuser.settings"
			|| state.GetProperty( "native" ).GetString() != binding.NativeName
			|| state.GetProperty( "kind" ).GetString() != binding.Value.Kind.ToString()
			|| state.GetProperty( "restart" ).GetBoolean() != binding.RequiresRestart )
			throw new InvalidOperationException( "Stored binding contract changed; explicit recovery is required." );
		var desired = NativeValue.Normalize( binding, state.GetProperty( "desired" ).GetString() );
		if ( desired is null ) throw new InvalidOperationException( "Stored setting violates the current constraints." );
		return new( result.Document.Revision, desired );
	}

	internal static string Serialize( NativeSettingDescriptor binding, string desired ) => JsonSerializer.Serialize( new
	{
		schema = 1, module = "superuser.settings", native = binding.NativeName,
		kind = binding.Value.Kind.ToString(), restart = binding.RequiresRestart, desired
	} );
}
