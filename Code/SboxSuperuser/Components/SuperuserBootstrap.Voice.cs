using Sandbox;

namespace SboxSuperuser.Components;

public sealed partial class SuperuserBootstrap : IHostVoiceEvent
{
	// Runs on the host for every relayed voice packet, so it stays allocation-free.
	void IHostVoiceEvent.OnHostVoice( HostVoiceEvent message )
	{
		if ( message.Suppress || _session?.GetRuntime() is not { } runtime ) return;
		if ( !runtime.AllowsVoice( message.Sender ) ) message.Suppress = true;
	}
}
