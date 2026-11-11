using Sandbox;

namespace SboxSuperuser.UI;

/// <summary>
/// Draws the vote panel on the HUD in the chat box's colours: a card on the left with the question, one row per
/// option with its key, count and progress toward a majority, and a lit-up icon for every voter.
/// </summary>
internal sealed class VotePanelRenderer
{
	/// <summary>The key for each option, in order.</summary>
	internal static readonly string[] Keys = { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9" };

	private const float Width = 380;
	private const float Padding = 14;
	private const float RowHeight = 30;
	private const float IconSize = 22;
	private const float IconGap = 4;
	private const int IconsPerRow = 10;
	private const int MaximumIcons = 30;
	private const float SlideSeconds = 0.3f;
	private const float LingerSeconds = 4;

	private static readonly Color Card = new( 0.102f, 0.102f, 0.102f, 0.94f );
	private static readonly Color Row = new( 0.141f, 0.141f, 0.141f, 1 );
	private static readonly Color Text = new( 0.91f, 0.91f, 0.91f );
	private static readonly Color Muted = new( 0.55f, 0.55f, 0.55f );
	private static readonly Color Yes = new( 0.29f, 0.87f, 0.5f );
	private static readonly Color No = new( 0.97f, 0.44f, 0.44f );
	private static readonly Color Other = new( 0.38f, 0.65f, 0.98f );
	private static readonly Color Pending = new( 0.3f, 0.3f, 0.3f );

	private readonly Dictionary<string, Texture> _avatars = new();
	private readonly List<(Rect Area, int Option)> _rows = new();

	/// <summary>Draws the vote; returns the option clicked this frame, if the cursor is free and one was.</summary>
	internal int? Draw( CameraComponent camera, VoteSnapshot vote, float sinceStarted, float sinceFinished )
	{
		var scale = camera.ScreenRect.Size.y / 1080f;
		// Slide in from the left as it starts; slide back out a few seconds after the result.
		var shown = vote.Finished ? 1 - Math.Clamp( (sinceFinished - LingerSeconds) / SlideSeconds, 0, 1 ) : Math.Clamp( sinceStarted / SlideSeconds, 0, 1 );
		if ( shown <= 0 ) return null;
		var ease = 1 - MathF.Pow( 1 - shown, 3 );
		var width = Width * scale;
		var x = 24 * scale - (1 - ease) * (width + 24 * scale);
		var y = camera.ScreenRect.Size.y * 0.3f;
		var height = Height( vote ) * scale;
		var mine = MyBallot( vote );
		_rows.Clear();

		using var hud = camera.BeginHud();
		hud.Fill = Card;
		hud.Rect( new Rect( x, y, width, height ), 8 * scale );
		var inner = x + Padding * scale;
		var innerWidth = width - Padding * 2 * scale;
		var cursor = y + Padding * scale;

		Label( hud, vote.Finished ? "VOTE ENDED" : "VOTE", 12 * scale, Muted, new Rect( inner, cursor, innerWidth, 16 * scale ), TextFlag.LeftCenter );
		if ( !vote.Finished )
		{
			var seconds = Math.Max( 0, (int)Math.Ceiling( (vote.EndsUtc - DateTimeOffset.UtcNow).TotalSeconds ) );
			Label( hud, $"{seconds}s", 12 * scale, Muted, new Rect( inner, cursor, innerWidth, 16 * scale ), TextFlag.RightCenter );
		}
		cursor += 20 * scale;
		Label( hud, vote.Question, 15 * scale, Text, new Rect( inner, cursor, innerWidth, 22 * scale ), TextFlag.LeftCenter );
		cursor += 30 * scale;

		var yesNo = vote.Options.Length == 2 && vote.Options[ 0 ] == "Yes" && vote.Options[ 1 ] == "No";
		for ( var option = 0; option < vote.Options.Length; option++ )
		{
			var row = new Rect( inner, cursor, innerWidth, (RowHeight - 4) * scale );
			_rows.Add( (row, option) );
			var tone = yesNo ? (option == 0 ? Yes : No) : Other;
			hud.Fill = mine == option ? tone.WithAlpha( 0.25f ) : Row;
			hud.Rect( row, 5 * scale );
			// Progress toward the votes needed to win.
			var progress = Math.Clamp( vote.Counts[ option ] / (float)Math.Max( 1, vote.Needed ), 0, 1 );
			if ( progress > 0 )
			{
				hud.Fill = tone.WithAlpha( 0.35f );
				hud.Rect( new Rect( row.Left, row.Top, row.Width * progress, row.Height ), 5 * scale );
			}
			Label( hud, Keys[ option ], 11 * scale, Muted, new Rect( row.Left + 8 * scale, row.Top, 26 * scale, row.Height ), TextFlag.LeftCenter );
			Label( hud, vote.Options[ option ], 14 * scale, Text, new Rect( row.Left + 38 * scale, row.Top, row.Width - 80 * scale, row.Height ), TextFlag.LeftCenter );
			Label( hud, vote.Counts[ option ].ToString(), 14 * scale, Text, new Rect( row.Left, row.Top, row.Width - 10 * scale, row.Height ), TextFlag.RightCenter );
			cursor += RowHeight * scale;
		}

		var caption = vote.Finished ? vote.Result : $"Needs {vote.Needed} of {vote.Voters.Length}";
		Label( hud, caption, 12 * scale, vote.Finished ? Text : Muted, new Rect( inner, cursor, innerWidth, 18 * scale ), TextFlag.LeftCenter );
		cursor += 24 * scale;
		DrawVoters( hud, vote, inner, cursor, scale, yesNo );
		return Clicked();
	}

	/// <summary>One avatar per voter, ringed in the colour of their choice, grey until they vote.</summary>
	private void DrawVoters( Painter hud, VoteSnapshot vote, float left, float top, float scale, bool yesNo )
	{
		var size = IconSize * scale;
		var gap = IconGap * scale;
		var shown = Math.Min( vote.Voters.Length, MaximumIcons );
		for ( var index = 0; index < shown; index++ )
		{
			var column = index % IconsPerRow;
			var row = index / IconsPerRow;
			var area = new Rect( left + column * (size + gap), top + row * (size + gap), size, size );
			var ballot = vote.Ballots[ index ];
			hud.Fill = ballot < 0 ? Pending : yesNo ? (ballot == 0 ? Yes : No) : Other;
			hud.Rect( area, size / 2 );
			if ( Avatar( vote.Voters[ index ] ) is { } avatar )
				hud.Texture( avatar, area.Shrink( 2 * scale ), ballot < 0 ? Color.White.WithAlpha( 0.35f ) : Color.White );
		}
		if ( vote.Voters.Length > MaximumIcons )
			Label( hud, $"+{vote.Voters.Length - MaximumIcons}", 12 * scale, Muted,
				new Rect( left + IconsPerRow * (size + gap), top + 2 * (size + gap), 40 * scale, size ), TextFlag.LeftCenter );
	}

	private static float Height( VoteSnapshot vote )
	{
		var iconRows = Math.Max( 1, (int)Math.Ceiling( Math.Min( vote.Voters.Length, MaximumIcons ) / (float)IconsPerRow ) );
		return Padding * 2 + 20 + 30 + vote.Options.Length * RowHeight + 24 + iconRows * (IconSize + IconGap);
	}

	private static int MyBallot( VoteSnapshot vote )
	{
		if ( Connection.Local is not { } local ) return -1;
		var me = Array.IndexOf( vote.Voters, local.SteamId.ToString() );
		return me < 0 ? -1 : vote.Ballots[ me ];
	}

	private int? Clicked()
	{
		if ( Mouse.Visibility != MouseVisibility.Visible || !Input.Keyboard.Pressed( "mouse1" ) ) return null;
		foreach ( var (area, option) in _rows )
			if ( area.IsInside( Mouse.Position ) ) return option;
		return null;
	}

	private Texture Avatar( string steamId )
	{
		if ( !_avatars.TryGetValue( steamId, out var avatar ) ) _avatars[ steamId ] = avatar = Texture.Load( $"avatarsmall:{steamId}", false );
		return avatar;
	}

	private static void Label( Painter hud, string text, float size, Color color, Rect area, TextFlag alignment )
	{
		hud.TextStyle = TextStyle.Default.WithSize( size ).WithColor( color ).WithAlignment( alignment );
		hud.Text( text, area );
	}
}
