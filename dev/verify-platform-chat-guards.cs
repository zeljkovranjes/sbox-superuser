// Synthetic scene-event checks supplement the real two-client platform submissions.
if ( !Networking.IsHost || !Networking.IsActive ) throw new InvalidOperationException( "Isolated host required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var sender = Connection.All.Single( connection => connection.SteamId == 90071996842377217 );
var listener = (IChatEvent)bootstrap;
var release = new TaskCompletionSource<bool>();
var completed = new TaskCompletionSource<bool>();
var calls = 0;
using var scope = SboxSuperuser.Superuser.BeginCommandRegistration( "t13.events" );
scope.RegisterCommand( new SboxSuperuser.CommandDescriptor( "t13.once", "Development event guard check", "admin.help", "t13.events" ),
	async ( context, arguments ) =>
	{
		calls++;
		await release.Task;
		completed.TrySetResult( true );
		return new SboxSuperuser.CommandResult( SboxSuperuser.CommandStatus.Success, "T13 event guard passed." );
	} );
scope.Commit();
try
{
	var system = new ChatMessageEvent { Message = "/admin t13.once", Sender = null };
	listener.OnChatMessage( system );
	if ( system.Suppress ) throw new InvalidOperationException( "System message was consumed." );
	var suppressed = new ChatMessageEvent { Message = "/admin t13.once", Sender = sender, Suppress = true };
	listener.OnChatMessage( suppressed );
	var ordinary = new ChatMessageEvent { Message = "T13 normal event", Sender = sender };
	listener.OnChatMessage( ordinary );
	if ( ordinary.Suppress || calls != 0 ) throw new InvalidOperationException( "Non-command or suppressed input executed." );
	var command = new ChatMessageEvent { Message = "/admin t13.once", Sender = sender };
	listener.OnChatMessage( command );
	if ( !command.Suppress || completed.Task.IsCompleted ) throw new InvalidOperationException( "Original was not suppressed before completion." );
	listener.OnChatMessage( command );
	for ( var attempt = 0; calls == 0 && attempt < 100; attempt++ ) await Task.Delay( 10, cancellationToken );
	if ( calls != 1 ) throw new InvalidOperationException( "Duplicate callback did not execute exactly once." );
	release.TrySetResult( true );
	await completed.Task;
	await Task.Delay( 100, cancellationToken );
	if ( calls != 1 ) throw new InvalidOperationException( "A repeated callback queued another execution." );
	return new { Pass = true, Calls = calls, SynchronousSuppression = command.Suppress, SystemIgnored = !system.Suppress };
}
finally
{
	release.TrySetResult( true );
}
