using Sandbox;

namespace SboxSuperuser.Engine.Compatibility;

internal static class ProbeRuntime
{
	internal static bool IsSupported( bool allowEditorTesting )
	{
		return Networking.IsHost
			&& (Application.IsDedicatedServer || (Game.IsEditor && allowEditorTesting));
	}

	internal static string Describe( bool allowEditorTesting )
	{
		var mode = Application.IsDedicatedServer ? "DedicatedServer" : "EditorTest";
		if ( !IsSupported( allowEditorTesting ) )
		{
			mode = "Unsupported";
		}

		return $"mode={mode}; engine={Application.Version}; "
			+ $"dedicated={Application.IsDedicatedServer}; editor={Game.IsEditor}; "
			+ $"host={Networking.IsHost}; editorOptIn={allowEditorTesting}";
	}
}
