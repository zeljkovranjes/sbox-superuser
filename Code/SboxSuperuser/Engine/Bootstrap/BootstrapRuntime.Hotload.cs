using Sandbox;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed partial class BootstrapRuntime : IHotloadManaged
{
	void IHotloadManaged.Destroyed( Dictionary<string, object> state ) => Dispose();
	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state ) => _disposed = true;
	void IHotloadManaged.Persisted() => Dispose();
	void IHotloadManaged.Failed() => Dispose();
}
