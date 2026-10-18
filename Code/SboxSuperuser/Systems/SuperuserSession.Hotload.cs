using Sandbox;

namespace SboxSuperuser.Systems;

internal sealed partial class SuperuserSession : IHotloadManaged
{
	private const string RestartStateKey = "restartRequired";

	private void RetireForSourceReload()
	{
		_restartRequired |= _runtime is not null;
		if ( !_restartRequired ) return;
		Stop( BootstrapState.RestartRequired );
		Log.Warning( "[Superuser] Source reloaded; start a fresh play session or dedicated process before continuing administration." );
	}

	void IHotloadManaged.Destroyed( Dictionary<string, object> state )
	{
		RetireForSourceReload();
		state[ RestartStateKey ] = _restartRequired;
	}

	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state )
	{
		_restartRequired |= _runtime is not null || state.TryGetValue( RestartStateKey, out var value ) && value is true;
		_runtime = null;
		_stopping = false;
		if ( _restartRequired ) SetState( BootstrapState.RestartRequired );
	}

	void IHotloadManaged.Persisted() => RetireForSourceReload();
	void IHotloadManaged.Failed() => RetireForSourceReload();
}
