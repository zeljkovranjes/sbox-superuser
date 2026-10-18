using SboxSuperuser.Engine.CommandDiscovery;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Starts a host-main-thread module batch; commit to activate and retain the scope until module shutdown.</summary>
	public static CommandRegistrationScope BeginCommandRegistration( string moduleId )
	{
		var runtime = FindAuthorityRuntime() ?? throw new InvalidOperationException( "An eligible authoritative runtime is required." );
		return new CommandRegistrationScope( runtime.Commands, moduleId, AttributeCommandDiscovery.Discover );
	}

	/// <summary>Gets local registered command metadata; this host-only catalog is not permission-filtered client help.</summary>
	public static IReadOnlyList<CommandDescriptor> GetCommandDescriptors()
	{
		return FindAuthorityRuntime()?.Commands.Descriptors ?? Array.Empty<CommandDescriptor>();
	}
}
