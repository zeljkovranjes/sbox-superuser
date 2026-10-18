// Run as a method body with sbox-cli run_csharp, after starting a scene with one probe.
// This exercises engine entry points; inspect the matching T01 console observations.
var scene = Game.ActiveScene;
if ( !Game.IsEditor || !Game.IsPlaying || scene is null )
{
	throw new InvalidOperationException( "Start the isolated editor test scene first." );
}

var probe = scene.GetAllComponents<SboxSuperuser.Components.CompatibilityProbe>().Single();
var originalOptIn = probe.AllowEditorTesting;
var originalEnabled = probe.Enabled;
GameObject duplicateObject = null;

try
{
	probe.Enabled = true;
	probe.AllowEditorTesting = false;
	ConsoleSystem.Run( "superuser_probe default-must-refuse" );
	ConsoleSystem.Run( "superuser_probe_local" );

	probe.AllowEditorTesting = true;
	ConsoleSystem.Run( "superuser_probe_local" );
	ConsoleSystem.Run( "superuser_probe \"quoted reason\" tail" );
	ConsoleSystem.Run( "superuser_probe", "quoted reason", "tail" );
	ConsoleSystem.Run( "superuser_probe" );
	ConsoleSystem.Run( "superuser_probe 1 2 3 4 5 6 7 8 9" );

	probe.Enabled = false;
	ConsoleSystem.Run( "superuser_probe disabled-must-refuse" );
	probe.Enabled = true;

	duplicateObject = new GameObject( scene );
	var duplicate = duplicateObject.Components.Create<SboxSuperuser.Components.CompatibilityProbe>();
	duplicate.AllowEditorTesting = true;
	ConsoleSystem.Run( "superuser_probe duplicate-must-refuse" );
}
finally
{
	duplicateObject?.Destroy();
	probe.AllowEditorTesting = originalOptIn;
	probe.Enabled = originalEnabled;
}

return "Editor entry points exercised. Verify T01 logs; no dedicated-server claim is made.";
