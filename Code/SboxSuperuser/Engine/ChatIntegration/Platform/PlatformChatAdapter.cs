using Sandbox;
using SboxSuperuser.Engine.Bootstrap;

namespace SboxSuperuser.Engine.ChatIntegration.Platform;

internal sealed class PlatformChatAdapter : IDisposable
{
	private readonly BootstrapRuntime _runtime;
	private readonly ChatCommandIngress _ingress;
	private bool _disposed;

	internal PlatformChatAdapter( BootstrapRuntime runtime )
	{
		_runtime = runtime;
		_ingress = new ChatCommandIngress( runtime.ConsoleRoot.Name, runtime.Dispatcher, runtime.Audit, Available );
	}

	private bool Available() => !_disposed && ThreadSafe.IsMainThread && _runtime.IsUsable && _runtime.ConsoleRoot.IsAvailable;

	internal bool TryHandle( Connection sender, string message, Guid requestId )
	{
		if ( !Available() || !_ingress.Owns( message ) )
		{
			return false;
		}
		_ = ExecuteAsync( sender, message, requestId );
		return true;
	}

	internal void Observe( ChatMessageEvent message )
	{
		if ( message is null || message.Suppress || message.Sender is null || !Available() || !_ingress.Owns( message.Message ) )
		{
			return;
		}
		// Consumption must be visible to later listeners before any asynchronous handler starts.
		message.Suppress = true;
		_ = ExecuteAsync( message.Sender, message.Message, Guid.NewGuid() );
	}

	private async Task ExecuteAsync( Connection sender, string message, Guid requestId )
	{
		CommandResult result;
		try
		{
			result = await _ingress.ExecuteAsync( () => _runtime.TryResolvePlayer( sender, out var actor ) ? actor : null,
				message, requestId, CancellationToken.None );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Chat request {requestId:N} failed; type={error.GetType().Name}." );
			result = new CommandResult( CommandStatus.Unknown, "The command outcome could not be determined. Contact the server owner." );
		}
		if ( Available() )
		{
			ChatReplies.Send( sender, requestId, result );
		}
	}

	public void Dispose() => _disposed = true;
}
