namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Installs trusted in-process modules as one validated catalog and starts them in dependency order; host setup code only.</summary>
	public static async Task<CommandResult> RegisterModulesAsync( IReadOnlyList<IAdminModule> modules, CancellationToken cancellation = default )
	{
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		runtime.Modules.Install( modules );
		return await runtime.Modules.StartAllAsync( cancellation );
	}

	/// <summary>Gets host-local module state; use permission-filtered modules.list for player requests.</summary>
	public static IReadOnlyList<ModuleStatus> GetModuleStatuses()
		=> FindAuthorityRuntime()?.Modules.Statuses ?? Array.Empty<ModuleStatus>();
}
