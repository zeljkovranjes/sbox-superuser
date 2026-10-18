namespace SboxSuperuser.Engine.CommandDiscovery;

internal sealed class HandlerSignature2<T1, T2> : IHandlerSignature
{
	/// <summary>Checks closed delegate binding without executing the handler.</summary>
	public void Validate( Sandbox.MethodDescription method )
	{
		if ( method.ReturnType == typeof( CommandResult ) )
		{
			_ = method.CreateDelegate<Func<T1, T2, CommandResult>>();
		}
		else
		{
			_ = method.CreateDelegate<Func<T1, T2, Task<CommandResult>>>();
		}
	}
}
