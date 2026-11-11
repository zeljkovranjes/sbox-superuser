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
		_ingress = runtime.ConsoleRoot.IsAvailable ? new ChatCommandIngress( runtime.ConsoleRoot.Name, runtime.Dispatcher, runtime.Audit, Available ) : null;
	}

	private bool Available() => !_disposed && ThreadSafe.IsMainThread && _runtime.IsUsable;

	internal bool TryHandle( Connection sender, string message, Guid requestId )
	{
		message = Expand( message );
		if ( !Available() || _ingress?.Owns( message ) != true )
		{
			return false;
		}
		_ = ExecuteAsync( sender, message, requestId );
		return true;
	}

	internal void Observe( ChatMessageEvent message )
	{
		if ( message is null || message.Suppress || message.Sender is null || !Available() )
		{
			return;
		}
		message.Message = Expand( message.Message );
		if ( _ingress?.Owns( message.Message ) != true )
		{
			var result = _runtime.CheckOrdinaryChat( message.Sender );
			if ( result.Status != CommandStatus.Success )
			{
				message.Suppress = true;
				ChatReplies.Send( message.Sender, Guid.NewGuid(), result );
				return;
			}
			// Only lines every player is about to see are kept; commands and blocked messages never are.
			if ( _runtime.TryResolvePlayer( message.Sender, out var sender ) )
			{
				// A gimped player still talks, but everyone sees a silly line instead of what they wrote.
				if ( _runtime.Moderation.IsRestricted( sender.SubjectId, Core.Moderation.ModerationKind.Gimp ) )
					message.Message = Core.Moderation.GimpLines.For( message.Message );
				_runtime.RecentChat.Record( sender.SubjectId, message.Sender.DisplayName, message.Message, DateTimeOffset.UtcNow );
				// The stock chat shows only text, so the prefix rides at the front of the message everyone receives.
				if ( _runtime.Prefixes?.PlainTag( sender.SubjectId ) is { Length: > 0 } tag ) message.Message = tag + " " + message.Message;
			}
			return;
		}
		// Consumption must be visible to later listeners before any asynchronous handler starts.
		message.Suppress = true;
		_ = ExecuteAsync( message.Sender, message.Message, Guid.NewGuid() );
	}

	private async Task ExecuteAsync( Connection sender, string message, Guid requestId )
	{
		CommandResult result;
		var entered = false;
		try
		{
			if ( !_runtime.TryResolvePlayer( sender, out _ ) )
			{
				result = new CommandResult( CommandStatus.Denied, "A live authenticated player is required." );
			}
			else if ( !_runtime.AdminRequests.TryEnter( sender.Id, DateTimeOffset.UtcNow ) )
			{
				result = new CommandResult( CommandStatus.Conflict, "Too many administration requests; wait before trying again." );
			}
			else
			{
				entered = true;
				result = await _ingress.ExecuteAsync( () => _runtime.TryResolvePlayer( sender, out var actor ) ? actor : null,
					message, requestId, CancellationToken.None );
			}
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Chat request {requestId:N} failed; type={error.GetType().Name}." );
			result = new CommandResult( CommandStatus.Unknown, "The command outcome could not be determined. Contact the server owner." );
		}
		finally
		{
			if ( entered ) _runtime.AdminRequests.Exit( sender.Id );
		}
		if ( Available() )
		{
			ChatReplies.Send( sender, requestId, result );
		}
	}

	/// <summary>
	/// "/mute Bob" is shorthand for "/admin mute Bob" with any registered command, from any chat box. Slash text that
	/// isn't one of Superuser's commands stays ordinary chat, so a game's own slash commands keep working.
	/// </summary>
	private string Expand( string message )
	{
		if ( _ingress is null || message is not { Length: > 1 } || message[ 0 ] != '/' ) return message;
		var commands = _runtime.Commands.Descriptors.Select( descriptor => new Core.Commands.CompletionCommand(
			descriptor.Name.Replace( '.', ' ' ), Array.Empty<Core.Commands.CompletionArgument>() ) ).ToArray();
		return Core.Commands.CommandCompletion.Expand( message, _runtime.ConsoleRoot.Name, commands );
	}

	public void Dispose() => _disposed = true;
}
