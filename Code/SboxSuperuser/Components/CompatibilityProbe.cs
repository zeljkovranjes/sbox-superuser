using Sandbox;
using SboxSuperuser.Engine.Compatibility;

namespace SboxSuperuser.Components;

/// <summary>Opt-in T01 integration checks; this is not an administration bootstrap.</summary>
[Title( "Superuser Compatibility Probe" ), Category( "Superuser" )]
public sealed partial class CompatibilityProbe : Component
{
	/// <summary>Allows checks on an editor host only; never enables a player-hosted release.</summary>
	[Property, Group( "Testing" )]
	public bool AllowEditorTesting { get; set; }

	/// <summary>Writes only an isolated T01 marker in host data storage when the scene starts.</summary>
	[Property, Group( "Testing" )]
	public bool ProbeStorageOnStart { get; set; }

	private bool CanRun => Enabled && ProbeRuntime.IsSupported( AllowEditorTesting );

	protected override void OnStart()
	{
		Log.Info( $"[Superuser T01] {ProbeRuntime.Describe( AllowEditorTesting )}" );
		if ( CanRun && FindActiveProbe() == this )
		{
			ProbeDiscovery.Run();
			if ( ProbeStorageOnStart )
			{
				ProbeStorage.Run();
			}
		}
	}

	private static CompatibilityProbe FindActiveProbe()
	{
		var scene = Game.ActiveScene;
		if ( scene is null )
		{
			return null;
		}

		CompatibilityProbe result = null;
		foreach ( var probe in scene.GetAllComponents<CompatibilityProbe>() )
		{
			if ( !probe.CanRun )
			{
				continue;
			}

			if ( result is not null )
			{
				Log.Warning( "[Superuser T01] Duplicate probes: keep one enabled component per scene." );
				return null;
			}

			result = probe;
		}

		return result;
	}
}
