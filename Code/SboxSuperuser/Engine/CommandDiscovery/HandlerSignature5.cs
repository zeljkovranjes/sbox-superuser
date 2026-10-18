namespace SboxSuperuser.Engine.CommandDiscovery;

internal sealed class HandlerSignature5<T1, T2, T3, T4, T5> : IHandlerSignature
{
	/// <summary>Checks closed delegate binding without executing the handler.</summary>
	public void Validate( Sandbox.MethodDescription method )
	{
		if ( method.ReturnType == typeof( CommandResult ) )
		{
			_ = method.CreateDelegate<Func<T1, T2, T3, T4, T5, CommandResult>>();
		}
		else
		{
			_ = method.CreateDelegate<Func<T1, T2, T3, T4, T5, Task<CommandResult>>>();
		}
	}
}
