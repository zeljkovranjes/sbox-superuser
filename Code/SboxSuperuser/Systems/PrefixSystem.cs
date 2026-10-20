using Sandbox;
using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Prefixes;

namespace SboxSuperuser.Systems;

/// <summary>
/// Prefixes on this machine. The host sends a snapshot whenever the catalog or an online player's prefix changes;
/// clients keep the latest one, ask for it when they have none, and render names from it locally.
/// </summary>
internal sealed class PrefixSystem : GameObjectSystem<PrefixSystem>
{
	private const float PublishInterval = 1;
	private const float RequestInterval = 3;

	private readonly PrefixRenderer _renderer = new();
	private readonly List<LineRun> _runs = new();
	private RealTimeSince _sincePublish = PublishInterval;
	private RealTimeSince _sinceRequest = RequestInterval;
	private bool _received;
	private Connection _host;

	public PrefixSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -70, Update, nameof( PrefixSystem ) );
	}

	internal EffectLibrary Effects { get; } = new();

	internal PrefixSnapshot Snapshot { get; private set; } = PrefixSnapshot.Disabled;

	/// <summary>The snapshot the host last sent, for players who ask for it.</summary>
	internal string Published { get; private set; }

	/// <summary>Whether Superuser's chat box replaces the built-in one.</summary>
	internal bool ChatActive => Snapshot.Enabled && Snapshot.Chat;

	/// <summary>UTC milliseconds: the clock every machine animates from.</summary>
	internal static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private void Update()
	{
		if ( !Networking.IsActive )
		{
			if ( _received ) Receive( null );
			return;
		}
		if ( Networking.IsHost ) Publish();
		else RequestWhenMissing();
	}

	// Once a second the host rebuilds the snapshot (group changes and joins move prefixes too) and sends it if it changed.
	private void Publish()
	{
		if ( _sincePublish < PublishInterval ) return;
		_sincePublish = 0;
		var module = Scene.GetSystem<SuperuserSession>()?.GetRuntime()?.Prefixes;
		string json;
		try
		{
			json = PrefixCodec.WriteSnapshot( module?.Snapshot() ?? PrefixSnapshot.Disabled );
		}
		catch ( InvalidOperationException error )
		{
			Log.Warning( $"[Superuser] Prefixes were not sent: {error.Message}" );
			return;
		}
		if ( json == Published ) return;
		Published = json;
		Superuser.BroadcastPrefixes( json );
	}

	private void RequestWhenMissing()
	{
		if ( !ReferenceEquals( _host, Connection.Host ) )
		{
			_host = Connection.Host;
			_received = false;
		}
		if ( _received || _sinceRequest < RequestInterval ) return;
		_sinceRequest = 0;
		Superuser.AskForPrefixes();
	}

	/// <summary>Applies a snapshot from the host (null clears it) and tells components whose prefix changed.</summary>
	internal void Receive( string json )
	{
		PrefixSnapshot next;
		try
		{
			next = json is null ? PrefixSnapshot.Disabled : PrefixCodec.ReadSnapshot( json );
		}
		catch ( Exception error ) when ( error is FormatException or ArgumentException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException )
		{
			Log.Warning( $"[Superuser] Ignored an invalid prefix snapshot; type={error.GetType().Name}." );
			return;
		}

		var previous = Snapshot;
		Snapshot = next;
		_received = json is not null;
		var players = previous.Players.Keys.Union( next.Players.Keys ).ToArray();
		foreach ( var steamId in players )
		{
			var before = previous.For( steamId );
			var after = next.For( steamId );
			if ( before is not null && after is not null && PrefixCodec.Same( before, after ) ) continue;
			if ( before is null && after is null ) continue;
			var prefix = PrefixOf( steamId );
			Scene.RunEvent<IPrefixChangedEvent>( listener =>
			{
				try { listener.OnPrefixChanged( steamId, prefix ); }
				catch ( Exception error ) { Log.Warning( $"[Superuser] Prefix listener failed; type={error.GetType().Name}." ); }
			} );
		}
	}

	internal PlayerPrefix PrefixOf( string steamId )
	{
		var prefix = Snapshot.For( steamId );
		if ( prefix is null ) return null;
		var animated = Effects.Resolve( prefix.TagStyle )?.IsAnimated == true || Effects.Resolve( prefix.NameStyle )?.IsAnimated == true;
		return new PlayerPrefix( prefix.Id, prefix.Name, prefix.Tag, prefix.Template, animated );
	}

	/// <summary>Renders a player's name with their prefix into runs; a player without one gets their plain name.</summary>
	internal void Render( string steamId, string name, double time, List<LineRun> runs )
	{
		var prefix = Snapshot.For( steamId );
		if ( prefix is null )
		{
			runs.Add( new LineRun( name ?? "", EffectColor.White, 0, false, false ) );
			return;
		}
		Render( prefix, Effects.Resolve( prefix.TagStyle ), Effects.Resolve( prefix.NameStyle ), name, steamId, time, runs );
	}

	/// <summary>Renders any definition with explicit effects, for previews of unsaved edits.</summary>
	internal void Render( PrefixDefinition prefix, IGlyphEffect tag, IGlyphEffect name, string playerName, string steamId, double time, List<LineRun> runs )
		=> _renderer.Render( prefix, tag, name, playerName, time, PrefixSeed.Of( steamId ), runs );

	internal StyledLine RenderLine( string steamId, string name, double time )
	{
		_runs.Clear();
		Render( steamId, name, time, _runs );
		return new StyledLine( _runs.Select( run => new StyledRun( run.Text, EffectColors.ToColor( run.Color ), run.Gap, run.Bold, run.Italic ) ).ToArray() );
	}
}
