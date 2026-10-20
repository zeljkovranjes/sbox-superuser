using Editor;
using Sandbox;
using System.IO;

namespace SboxSuperuser.EditorTools;

/// <summary>
/// Gives every game that uses Superuser a Code/Superuser/Modules folder for drop-in modules, with a note saying what
/// goes there. It is created once per project; delete it and it stays deleted.
/// </summary>
internal static class ModulesFolder
{
	private const string NoteName = "DROP THIRD PARTY MODULES HERE.txt";

	private const string Note = """
		Drop third-party Superuser modules here, one folder per module, for example:

		    Code/Superuser/Modules/greeter/GreeterModule.cs

		A module marked [AdminModule] loads by itself when the Superuser Bootstrap starts:
		there is nothing to register. Restart Play after adding one. Its commands appear in
		the admin menu under Server > Commands, and its guides under Superuser > Documentation.

		Guide: https://github.com/zeljkovranjes/sbox-superuser/blob/master/docs/reference/modules.md
		""";

	[Event( "editor.created" )]
	private static void OnEditorCreated( EditorMainWindow window ) => Ensure();

	// Also runs right after Superuser is added to a project that is already open.
	[EditorEvent.Hotload]
	private static void OnHotload() => Ensure();

	private static void Ensure()
	{
		var project = Project.Current;
		if ( project?.Config?.Type != "game" || !project.HasCodePath() ) return;

		var cookie = $"superuser.modules-folder.{project.Config.FullIdent}";
		if ( EditorCookie.Get( cookie, false ) ) return;

		var folder = Path.Combine( project.GetCodePath(), "Superuser", "Modules" );
		Directory.CreateDirectory( folder );
		var note = Path.Combine( folder, NoteName );
		if ( !File.Exists( note ) ) File.WriteAllText( note, Note );
		EditorCookie.Set( cookie, true );
		Log.Info( $"[Superuser] Created {folder} for drop-in modules." );
	}
}
