using Sandbox;
using SboxSuperuser;

namespace SuperuserExamples.Components;

/// <summary>Minimal replacement-view receiver; connect its methods and reply to your own widgets.</summary>
public sealed class ReplacementAdminExample : Component, IAdminResultEvent
{
	/// <summary>Gets the most recent private reply for this component's request.</summary>
	public AdminReply LastReply { get; private set; }

	/// <summary>Gets the pending request ID.</summary>
	public Guid RequestId { get; private set; }

	/// <summary>Requests a name or Steam ID search.</summary>
	public void Search( string text ) => RequestId = Superuser.RequestAdminQuery( new AdminQuery( AdminQueryKind.Players, text ) );

	/// <summary>Requests an exact online or offline player's details.</summary>
	public void Inspect( string steamId ) => RequestId = Superuser.RequestAdminQuery( new AdminQuery( AdminQueryKind.Player, steamId ) );

	/// <summary>Requests only the activity this player may read.</summary>
	public void Activity() => RequestId = Superuser.RequestAdminQuery( new AdminQuery( AdminQueryKind.Activity ) );

	/// <summary>Submits input after your view has displayed and confirmed it.</summary>
	public void ConfirmedAction( AdminActionRequest request ) => RequestId = Superuser.RequestAdminAction( request );

	/// <summary>Accepts only the matching private result.</summary>
	public void OnAdminResult( AdminReply reply )
	{
		if ( reply.RequestId == RequestId ) LastReply = reply;
	}
}
