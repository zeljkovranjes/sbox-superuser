namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Creates the optional settings module for RegisterModulesAsync; call once per authoritative runtime from trusted game setup.</summary>
	public static IAdminModule CreateNativeBindingsModule( IEnumerable<NativeSettingDescriptor> settings,
		IEnumerable<NativeCommandBindingDescriptor> commands = null )
	{
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		return new Engine.NativeSettings.NativeBindingsModule( runtime, settings, commands );
	}
}
