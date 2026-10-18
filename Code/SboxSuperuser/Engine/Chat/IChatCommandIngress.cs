namespace SboxSuperuser.Engine.Chat;

// T13 boundary: sender must come from the host's authenticated transport, never message fields.
// True means consumed (including errors), not execution success; suppress before asynchronous work.
internal interface IChatCommandIngress
{
	bool TryConsume( ActorIdentity sender, string message, Guid requestId );
}
