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
		runtime.Modules.Install( modules.Where( module => module is null || !runtime.AutoLoadedTypes.Contains( module.GetType() ) ).ToArray() );
		return await runtime.Modules.StartAllAsync( cancellation );
	}

	/// <summary>Gets host-local module state; use permission-filtered modules.list for player requests.</summary>
	public static IReadOnlyList<ModuleStatus> GetModuleStatuses()
		=> FindAuthorityRuntime()?.Modules.Statuses ?? Array.Empty<ModuleStatus>();
}
