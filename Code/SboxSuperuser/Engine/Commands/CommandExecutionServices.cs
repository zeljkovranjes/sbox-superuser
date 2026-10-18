namespace SboxSuperuser.Engine.Commands;

internal sealed class CommandExecutionServices : ICommandServices
{
	internal HostCommandDispatcher Issuer { get; }
	internal Func<ActorIdentity> Authenticate { get; }
	internal CommandService Service { get; }
	private readonly CommandCapabilities _capabilities;

	internal CommandExecutionServices( HostCommandDispatcher issuer, Func<ActorIdentity> authenticate, CommandService service,
		CommandCapabilities capabilities = null )
	{
		Issuer = issuer;
		Authenticate = authenticate;
		Service = service;
		_capabilities = capabilities ?? CommandCapabilities.Empty;
	}

	public T GetService<T>() where T : class => _capabilities.GetService<T>();
}
