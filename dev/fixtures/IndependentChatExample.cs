using Sandbox;
using SboxSuperuser;
using SuperuserCompatibilityLab.Engine.Chat;

namespace SuperuserCompatibilityLab.Components;

/// <summary>Minimal in-process RPC chat backend; never forwards messages into platform Chat.Say.</summary>
public sealed class IndependentChatExample : Component
{
	private readonly ExampleChatGate _gate = new();

	/// <summary>Submits one message ID and text; the server derives identity from Rpc.Caller.</summary>
	public void Submit( Guid requestId, string text ) => Receive( requestId, text );

	[Rpc.Host]
	private void Receive( Guid requestId, string text )
	{
		var sender = Rpc.Caller;
		if ( !Networking.IsHost || !Superuser.TryGetPlayerIdentity( sender, out _ ) || !_gate.Accept( sender, requestId, text ) )
		{
			return;
		}
		// One ingress, before channel routing and before ordinary-message mute checks.
		if ( Superuser.TryHandleChat( sender, text, requestId ) )
		{
			return;
		}
		var decision = Superuser.CheckOrdinaryChat( sender );
		if ( decision.Status != CommandStatus.Success )
		{
			using ( Rpc.FilterInclude( sender ) )
			{
				Deliver( "System", decision.Message );
			}
			return;
		}
		Deliver( sender.DisplayName, text );
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private void Deliver( string name, string text )
	{
		if ( Application.IsDedicatedServer || Rpc.Caller is null || !Rpc.Caller.IsHost
			|| name is null || name.Length > 256 || text is null || text.Length > 1024 )
		{
			return;
		}
		// Each recipient retains their Steam filtering preferences. No platform-chat network submission.
		var safeName = Utility.Steam.FilterName( name );
		var safeText = Utility.Steam.FilterChat( text );
		Scene.RunEvent<IExampleChatMessageEvent>( listener => listener.OnExampleChatMessage( safeName, safeText ) );
	}
}
