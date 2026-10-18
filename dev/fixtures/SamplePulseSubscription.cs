namespace SuperuserCompatibilityLab.Engine.Modules;

internal sealed class SamplePulseSubscription : IDisposable
{
	private Action _release;
	internal SamplePulseSubscription( Action release ) => _release = release;
	public void Dispose()
	{
		var release = _release;
		_release = null;
		release?.Invoke();
	}
}
