using Sandbox;
using SboxSuperuser.Engine.Compatibility;

namespace SboxSuperuser.Components;

public sealed partial class CompatibilityProbe
{
	/// <summary>Records native caller and token forwarding without executing an administration command.</summary>
	[ConCmd( "superuser_probe", ConVarFlags.Server )]
	public static void ObserveConsole( Connection caller, params string[] arguments )
	{
		var probe = FindActiveProbe();
		if ( probe is null )
		{
			Log.Warning( "[Superuser T01] Unsupported or inactive. Enable one probe on a dedicated server, or opt into editor testing." );
			return;
		}

		Log.Info( $"[Superuser T01] {ProbeRuntime.Describe( probe.AllowEditorTesting )}" );
		ProbeConsole.Observe( caller, arguments );
	}
}
