namespace SboxSuperuser.Engine.ChatIntegration;

internal interface IChatCommandIngress
{
	bool Owns( string message );
	Task<CommandResult> ExecuteAsync( Func<ActorIdentity> authenticate, string message, Guid requestId, CancellationToken cancellation );
}
