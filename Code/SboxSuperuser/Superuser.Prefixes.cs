using Sandbox;
using SboxSuperuser.Systems;

#pragma warning disable SB3006 // Dedicated administration deliberately does not survive host migration.

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>The prefix a player wears on this machine, or null (no prefix, or the Prefix module is off).</summary>
	public static PlayerPrefix GetPrefix( string steamId ) => Prefixes()?.PrefixOf( steamId );

	/// <summary>
	/// The player's name with their prefix, styled for this frame. Pass time to freeze an animation (UTC milliseconds);
	/// a player without a prefix gets their plain name.
	/// </summary>
	public static StyledLine RenderPrefix( string steamId, string name, double? time = null )
	{
		var system = Prefixes() ?? throw new InvalidOperationException( "Use the active scene's main thread." );
		return system.RenderLine( steamId, name ?? "", time ?? PrefixSystem.Now );
	}

	/// <summary>Adds a C# effect that prefixes can use as code:&lt;id&gt;. Register it on every machine, host and clients.</summary>
	public static void RegisterTagEffect( ITagEffect effect )
	{
		var system = Prefixes() ?? throw new InvalidOperationException( "Use the active scene's main thread." );
		system.Effects.Register( effect );
	}

	private static PrefixSystem Prefixes() => ThreadSafe.IsMainThread && Game.ActiveScene.IsValid()
		? Game.ActiveScene.GetSystem<PrefixSystem>() : null;

	internal static void BroadcastPrefixes( string json ) => ReceivePrefixes( json );

	internal static void AskForPrefixes() => ForwardPrefixRequest();

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void ReceivePrefixes( string json )
	{
		if ( Rpc.Caller is not { IsHost: true } || json is null ) return;
		Prefixes()?.Receive( json );
	}

	[Rpc.Host]
	private static void ForwardPrefixRequest()
	{
		var json = Prefixes()?.Published;
		if ( json is null || Rpc.Caller is not { IsActive: true } caller ) return;
		using ( Rpc.FilterInclude( caller ) ) ReceivePrefixes( json );
	}

	/// <summary>The "For game code" page of the Prefix module's documentation.</summary>
	internal const string PrefixApiGuide = """
		# For game code

		Everything goes through the `Superuser` class and works on every machine once the host has sent its
		prefixes.

		```csharp
		// The prefix a player wears, or null.
		PlayerPrefix prefix = Superuser.GetPrefix( steamId );
		string plain = prefix?.Format( name ) ?? name;

		// The styled line for this frame; ToMarkup() is for a rich Label, ToPlain() for logs.
		StyledLine line = Superuser.RenderPrefix( steamId, name );
		```

		`PrefixLabel` is a ready-made label: add it to your UI, set `SteamId` and `PlayerName`, and it animates
		itself. Implement `IPrefixChangedEvent` on a component to hear when a player's prefix changes.

		## C# effects

		For effects the script language can't express, implement `ITagEffect` and register it on every machine:

		```csharp
		public sealed class FireEffect : ITagEffect
		{
			public string Id => "game.fire";
			public string Title => "Fire";
			public bool IsAnimated => true;

			public TagLetterStyle Style( in TagLetter letter )
			{
				var heat = MathF.Sin( (float)(letter.Time / 120 + letter.Index) ) * 0.5f + 0.5f;
				return TagLetterStyle.Of( Color.Lerp( Color.Red, Color.Yellow, heat ) );
			}
		}

		Superuser.RegisterTagEffect( new FireEffect() );
		```

		It then shows up as `code:game.fire` in the effect pickers. A machine that hasn't registered it draws
		that part plain.
		""";
}
