using Sandbox;
using SboxSuperuser.Engine.Compatibility;

namespace SboxSuperuser.Components;

public sealed partial class CompatibilityProbe : IChatEvent
{
	void IChatEvent.OnChatMessage( ChatMessageEvent message )
	{
		if ( CanRun && FindActiveProbe() == this )
		{
			ProbeChat.Observe( message );
		}
	}
}
