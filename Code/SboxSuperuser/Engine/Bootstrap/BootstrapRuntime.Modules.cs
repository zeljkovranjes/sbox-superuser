using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed partial class BootstrapRuntime
{
	/// <summary>Module types this runtime created itself; RegisterModulesAsync skips them instead of failing on the duplicate.</summary>
	internal HashSet<Type> AutoLoadedTypes { get; } = new();

	/// <summary>Installs the shipped modules and every [AdminModule] in the game's code, then starts them together.</summary>
	internal void StartBuiltInModules()
	{
		var modules = new List<IAdminModule>();
		if ( Prefixes is not null ) modules.Add( Prefixes );
		modules.AddRange( DiscoverModules() );
		if ( modules.Count == 0 ) return;

		foreach ( var problem in Engine.Modules.ModuleAutoLoader.Install( Modules, modules ) )
			Log.Warning( $"[Superuser] A module was not loaded. {problem}" );
		_ = Modules.StartAllAsync( CancellationToken.None );
	}

	private IReadOnlyList<IAdminModule> DiscoverModules()
	{
		var found = new List<IAdminModule>();
		foreach ( var (type, _) in TypeLibrary.GetTypesWithAttribute<AdminModuleAttribute>( false ) )
		{
			if ( type.IsAbstract || type.IsGenericType ) continue;
			try
			{
				if ( type.Create<IAdminModule>() is not { } module )
				{
					Log.Warning( $"[Superuser] {type.Name} has [AdminModule] but is not an IAdminModule with a public parameterless constructor." );
					continue;
				}
				found.Add( module );
				AutoLoadedTypes.Add( type.TargetType );
			}
			catch ( Exception error )
			{
				Log.Warning( $"[Superuser] {type.Name} could not be created; type={error.GetType().Name}." );
			}
		}
		return found.OrderBy( module => module.Descriptor?.Id, StringComparer.Ordinal ).ToArray();
	}
}
