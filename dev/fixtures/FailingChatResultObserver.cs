using Sandbox;
using SboxSuperuser;

namespace SuperuserCompatibilityLab.Components;

/// <summary>Development-only observer used to verify safe default display after a view failure.</summary>
public sealed class FailingChatResultObserver : Component, IAdminChatResultEvent
{
	/// <summary>Gets attempted deliveries to this observer.</summary>
	public int Calls { get; private set; }

	/// <summary>Intentionally fails after suppressing display; the library must restore fallback behavior.</summary>
	public void OnAdminChatResult( AdminChatResultEvent message )
	{
		Calls++;
		message.SuppressDefaultDisplay = true;
		throw new InvalidOperationException( "Intentional custom view failure." );
	}
}
