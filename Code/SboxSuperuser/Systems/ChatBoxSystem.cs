using Sandbox;
using SboxSuperuser.UI;

namespace SboxSuperuser.Systems;

/// <summary>
/// Swaps the built-in chat box for Superuser's while the host has prefixes and Prefixed Chat on, and restores it
/// when they go off. Messages arrive through the bootstrap's chat events and show a frame later, after every other
/// listener has had the chance to suppress them.
/// </summary>
internal sealed class ChatBoxSystem : GameObjectSystem<ChatBoxSystem>
{
	private readonly List<ChatMessageEvent> _pending = new();
	private ChatBoxRenderer _box;
	private bool _builtInShown;
	private (string Sender, string Text, float At) _last;

	public ChatBoxSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -60, Update, nameof( ChatBoxSystem ) );
		Listen( Stage.FinishUpdate, 90, Render, nameof( ChatBoxSystem ) );
	}

	internal bool Active => _box is not null;

	internal bool IsOpen => _box?.IsOpen == true;

	internal void Observe( ChatMessageEvent message )
	{
		if ( Active && message is not null && !message.Suppress ) _pending.Add( message );
	}

	/// <summary>Shows a tagged public announcement; returns false when the built-in chat should show it.</summary>
	internal bool ShowAnnouncement( string tag, string message )
	{
		if ( !Active ) return false;
		_box.AddAnnouncement( tag, message );
		return true;
	}

	/// <summary>Shows Superuser's own private replies; returns false when the built-in chat should show them.</summary>
	internal bool ShowPrivate( IEnumerable<string> lines )
	{
		if ( !Active ) return false;
		foreach ( var line in lines ) _box.AddSystem( line );
		return true;
	}

	private void Update()
	{
		var wanted = !Application.IsDedicatedServer && Game.IsPlaying && Networking.IsActive
			&& Scene.GetSystem<PrefixSystem>()?.ChatActive == true;
		if ( wanted != Active )
		{
			if ( wanted ) Start();
			else Stop();
		}
		if ( !Active ) return;

		Deliver();
		var menuOpen = Scene.GetSystem<AdminMenuSystem>()?.IsOpen == true;
		if ( !_box.IsOpen && !menuOpen && Input.Pressed( "chat" ) ) _box.Open();
		// However the box opened, fetch the commands this player may run for autocomplete.
		if ( _box.IsOpen && !_wasOpen ) RequestCommands();
		_wasOpen = _box.IsOpen;
		if ( menuOpen ) _box.Close();
		_box.CaptureInput();
	}

	private void Deliver()
	{
		foreach ( var message in _pending )
		{
			if ( message.Suppress ) continue;
			if ( message.Sender is null )
			{
				_box.AddSystem( message.Message );
				continue;
			}
			// The host can see the same message twice (validation, then local delivery); show it once.
			var sender = message.Sender.SteamId.ToString();
			if ( _last.Sender == sender && _last.Text == message.Message && RealTime.Now - _last.At < 0.25f ) continue;
			_last = (sender, message.Message, RealTime.Now);
			var names = Connection.All.Select( connection => connection.DisplayName ).ToArray();
			var mentions = Core.Chat.Mentions.Find( message.Message, names );
			var mentionsMe = Connection.Local is { } local && mentions.Any( mention => string.Equals( mention.Name, local.DisplayName, StringComparison.OrdinalIgnoreCase ) );
			_box.AddPlayer( sender, message.Sender.DisplayName, message.Message, mentions, mentionsMe );
		}
		_pending.Clear();
	}

	private void Render()
	{
		try { _box?.Draw(); }
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Chat box failed; the built-in chat is back. type={error.GetType().Name}." );
			Stop();
		}
	}

	private void Start()
	{
		_builtInShown = ProjectSettings.Platform.ChatShowUI;
		ProjectSettings.Platform.ChatShowUI = false;
		_box = new ChatBoxRenderer( Scene, OpenPlayer, () => Scene.GetSystem<AdminMenuSystem>()?.IsAvailable == true, Complete );
	}

	// Commands this player may run, refreshed each time the chat opens since permissions can change.
	private IReadOnlyList<Core.Commands.CompletionCommand> _commands = Array.Empty<Core.Commands.CompletionCommand>();
	private Guid _commandsRequest;
	private bool _wasOpen;

	private void RequestCommands()
	{
		try { _commandsRequest = Superuser.RequestAdminQuery( new AdminQuery( AdminQueryKind.Commands ) ); }
		catch ( InvalidOperationException ) { _commandsRequest = Guid.Empty; }
	}

	/// <summary>Takes the command list for autocomplete; every other admin reply is someone else's.</summary>
	internal void Receive( AdminReply reply )
	{
		if ( reply.RequestId != _commandsRequest || reply.Status != CommandStatus.Success ) return;
		_commands = reply.Commands.Select( command => new Core.Commands.CompletionCommand( command.Name.Replace( '.', ' ' ),
			command.Arguments.Select( argument => new Core.Commands.CompletionArgument( argument.Name, Kind( argument ), argument.Optional,
				argument.Remainder, argument.Choices, argument.Description ) ).ToArray(), command.Description ) ).ToArray();
		_box?.RefreshCompletion();
	}

	private static Core.Commands.CompletionKind Kind( AdminCommandArgumentRow argument ) => argument.Kind switch
	{
		CommandArgumentKind.Player => Core.Commands.CompletionKind.Player,
		CommandArgumentKind.Duration => Core.Commands.CompletionKind.Duration,
		CommandArgumentKind.Boolean => Core.Commands.CompletionKind.Boolean,
		CommandArgumentKind.Integer or CommandArgumentKind.Number => Core.Commands.CompletionKind.Number,
		_ => argument.Choices.Length > 0 ? Core.Commands.CompletionKind.Choice : Core.Commands.CompletionKind.Text
	};

	private Core.Commands.Completion Complete( string line )
	{
		var players = Connection.All.Select( connection => connection.DisplayName ).ToArray();
		if ( !line.StartsWith( '/' ) ) return Core.Chat.Mentions.Complete( line, players );
		var root = Scene.GetAllComponents<Components.SuperuserBootstrap>().FirstOrDefault()?.CommandRoot ?? "admin";
		return Core.Commands.CommandCompletion.Complete( line, root, _commands, players );
	}

	private void OpenPlayer( string steamId )
	{
		_box?.Close();
		Scene.GetSystem<AdminMenuSystem>()?.ShowPlayer( steamId );
	}

	private void Stop()
	{
		_box?.Dispose();
		_box = null;
		_pending.Clear();
		ProjectSettings.Platform.ChatShowUI = _builtInShown;
	}

	public override void Dispose()
	{
		if ( Active ) Stop();
		base.Dispose();
	}
}
