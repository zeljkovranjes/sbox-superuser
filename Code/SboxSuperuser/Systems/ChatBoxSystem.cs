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

	internal void Observe( ChatMessageEvent message )
	{
		if ( Active && message is not null && !message.Suppress ) _pending.Add( message );
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
			_box.AddPlayer( sender, message.Sender.DisplayName, message.Message );
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
		_box = new ChatBoxRenderer( Scene, OpenPlayer, () => Scene.GetSystem<AdminMenuSystem>()?.IsAvailable == true );
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
