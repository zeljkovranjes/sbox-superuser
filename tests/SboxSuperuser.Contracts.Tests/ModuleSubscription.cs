namespace SboxSuperuser.Contracts.Tests;

internal sealed class ModuleSubscription : IDisposable
{
	private readonly Action _dispose;
	internal ModuleSubscription( Action dispose ) => _dispose = dispose;
	public void Dispose() => _dispose();
}
