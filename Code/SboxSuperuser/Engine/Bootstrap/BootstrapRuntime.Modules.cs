using Sandbox;
using SboxSuperuser.Core.Bootstrap;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed partial class BootstrapRuntime
{
	/// <summary>Module types this runtime created itself; RegisterModulesAsync skips them instead of failing on the duplicate.</summary>
	internal HashSet<Type> AutoLoadedTypes { get; } = new();

	/// <summary>
	/// Prints the startup banner, installs the shipped modules and every [AdminModule] in the game's code, starts them
	/// together and then reports each one in the console.
	/// </summary>
	internal void StartBuiltInModules()
	{
		Log.Info( StartupBanner.Header( Core.Contracts.ProductInfo.Version, Core.Contracts.ProductInfo.Author,
			Owners.Configuration?.Count ?? 0, Owners.Path ) );

		var modules = new List<IAdminModule>();
		if ( Prefixes is not null ) modules.Add( Prefixes );
		modules.AddRange( DiscoverModules() );
		var problems = Engine.Modules.ModuleAutoLoader.Install( Modules, modules );
		foreach ( var problem in problems ) Log.Warning( $"[Superuser] A module was not loaded. {problem}" );
		_ = ReportWhenStartedAsync( modules, problems );
	}

	private async Task ReportWhenStartedAsync( IReadOnlyList<IAdminModule> modules, IReadOnlyList<string> notInstalled )
	{
		await Modules.StartAllAsync( CancellationToken.None );
		if ( IsUsable ) ReportModules( modules.Select( module => module.Descriptor?.Id ), notInstalled );
	}

	/// <summary>One console line per module, then the ready line; used at startup and after RegisterModulesAsync.</summary>
	internal void ReportModules( IEnumerable<string> ids, IEnumerable<string> notInstalled = null )
	{
		var statuses = Modules.Statuses;
		var reports = new List<ModuleReport>();
		foreach ( var id in ids.Where( id => id is not null ).Distinct( StringComparer.Ordinal ) )
		{
			var status = statuses.FirstOrDefault( entry => entry.Descriptor.Id == id );
			if ( status is null ) continue;
			var loaded = status.State == ModuleState.Enabled;
			var contributions = Commands.DocumentationOf( id );
			var commands = Commands.Descriptors.Where( command => command.ModuleId == id ).ToArray();
			// Like the module's Permissions page: what it registers plus what its commands need, even framework keys.
			var permissions = contributions.Permissions.Select( permission => permission.Key )
				.Concat( commands.Select( command => command.Permission ) ).Distinct( StringComparer.Ordinal ).Count();
			reports.Add( new ModuleReport( id, status.Descriptor.Version, status.Descriptor.Author, loaded, loaded ? "" : status.Message,
				commands.Length, permissions, contributions.Pages.Count, contributions.Windows.Count ) );
		}

		// Install problems read "id: why", as the loader reports them.
		foreach ( var problem in notInstalled ?? Array.Empty<string>() )
		{
			var split = problem.IndexOf( ": ", StringComparison.Ordinal );
			reports.Add( new ModuleReport( split < 0 ? problem : problem[ ..split ], "", "", false,
				split < 0 ? "could not be installed" : problem[ (split + 2).. ], 0, 0, 0, 0 ) );
		}

		var lines = reports.Select( StartupBanner.Module ).Append( StartupBanner.Ready( reports, ConsoleRoot.Name ) );
		Log.Info( "\n" + string.Join( "\n", lines ) );
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
