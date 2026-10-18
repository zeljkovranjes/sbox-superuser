using Sandbox;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeDiscovery
{
	[ProbeMethod]
	/// <summary>Returns a fixed value so discovery can verify invocation without side effects.</summary>
	public static string Sentinel()
	{
		return "T01-discovery-ok";
	}

	internal static void Run()
	{
		var count = 0;
		foreach ( var entry in TypeLibrary.GetMethodsWithAttribute<ProbeMethodAttribute>() )
		{
			var result = entry.Method.InvokeWithReturn<string>( null );
			if ( result != "T01-discovery-ok" )
			{
				throw new InvalidOperationException( "Probe attribute invocation returned an unexpected result." );
			}

			count++;
		}

		Log.Info( $"[Superuser T01] discovery: {(count == 1 ? "PASS" : "FAIL")}; matches={count}; expected=1" );
	}
}
