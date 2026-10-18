namespace SboxSuperuser.Engine.CommandDiscovery;

internal sealed class HandlerSignature1<T1> : IHandlerSignature
{
	/// <summary>Checks closed delegate binding without executing the handler.</summary>
	public void Validate( Sandbox.MethodDescription method )
	{
		if ( method.ReturnType == typeof( CommandResult ) )
		{
			_ = method.CreateDelegate<Func<T1, CommandResult>>();
		}
		else
		{
			_ = method.CreateDelegate<Func<T1, Task<CommandResult>>>();
		}
	}
}
