namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>
	/// Installs trusted in-process modules as one validated catalog and starts them in dependency order; host setup code
	/// only. Modules marked [AdminModule] load by themselves and are skipped here.
	/// </summary>
	public static async Task<CommandResult> RegisterModulesAsync( IReadOnlyList<IAdminModule> modules, CancellationToken cancellation = default )
	{
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		// The shipped and automatic modules may still be starting; wait for them instead of refusing the game's modules.
		while ( runtime.Modules.IsBusy ) await runtime.Modules.Idle;
		ArgumentNullException.ThrowIfNull( modules );
		var added = modules.Where( module => module is null || !runtime.AutoLoadedTypes.Contains( module.GetType() ) ).ToArray();
		runtime.Modules.Install( added );
		var result = await runtime.Modules.StartAllAsync( cancellation );
		if ( runtime.IsUsable && added.Length > 0 ) runtime.ReportModules( added.Select( module => module.Descriptor.Id ) );
		return result;
	}

	/// <summary>Gets host-local module state; use permission-filtered modules.list for player requests.</summary>
	public static IReadOnlyList<ModuleStatus> GetModuleStatuses()
		=> FindAuthorityRuntime()?.Modules.Statuses ?? Array.Empty<ModuleStatus>();
}
