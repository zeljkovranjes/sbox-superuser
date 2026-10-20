namespace SboxSuperuser.Engine.Modules;

/// <summary>
/// Installs modules the bootstrap found by itself. Each is installed on its own, in passes, so a module listed
/// before one it depends on still gets in, and one broken module never keeps the others out.
/// </summary>
internal static class ModuleAutoLoader
{
	/// <summary>Installs what it can and returns one line per module it could not install, saying why.</summary>
	internal static IReadOnlyList<string> Install( ModuleManager manager, IReadOnlyList<IAdminModule> modules )
	{
		var waiting = modules.ToList();
		var problems = new Dictionary<IAdminModule, string>();
		var progress = true;
		while ( waiting.Count > 0 && progress )
		{
			progress = false;
			foreach ( var module in waiting.ToArray() )
			{
				try
				{
					manager.Install( new[] { module } );
					waiting.Remove( module );
					progress = true;
				}
				catch ( Exception error ) when ( error is ArgumentException or InvalidOperationException )
				{
					problems[ module ] = error.Message;
				}
			}
		}
		return waiting.Select( module => $"{module.Descriptor?.Id ?? "module without a descriptor"}: {problems[ module ]}" ).ToArray();
	}
}
