using Sandbox;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeNativeSettings
{
	[ConVar( "superuser_probe_value" )]
	private static int Value { get; set; } = 3;

	[ConVar( "superuser_probe_protected", ConVarFlags.Protected )]
	private static int ProtectedValue { get; set; } = 7;

	internal static void Run()
	{
		try
		{
			ConsoleSystem.SetValue( "superuser_probe_value", 8 );
			var readback = ConsoleSystem.GetValue( "superuser_probe_value" );
			Log.Info( $"[Superuser T01] native setting readback: {(Value == 8 && readback == "8" ? "PASS" : "FAIL")}" );
		}
		finally
		{
			ConsoleSystem.SetValue( "superuser_probe_value", 3 );
		}

		try
		{
			ConsoleSystem.SetValue( "superuser_probe_protected", 8 );
		}
		catch ( Exception exception )
		{
			Log.Info( $"[Superuser T01] native protected write rejected: {exception.Message}" );
		}

		Log.Info( $"[Superuser T01] native protected value preserved: {(ProtectedValue == 7 ? "PASS" : "FAIL")}" );
	}
}
