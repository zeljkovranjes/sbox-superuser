using Sandbox;
using SboxSuperuser.Core.Chat;
using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

/// <summary>How a chat line is drawn: wrapped text with mentions, the avatar, the announcement tag and shadows.</summary>
internal sealed partial class ChatBoxRenderer
{
	// The shadow is the same text, wrapped the same way, one pixel down and right.
	/// <summary>
	/// Draws the message from the cursor, wrapping later lines back to the left edge instead of under the name or
	/// tag, with @mentions in gold. The wrap and its coloured pieces are worked out once per width and kept on the
	/// entry, so drawing each frame doesn't allocate.
	/// </summary>
	private static void FlowText( ChatEntry line, Vector4 color, float left, float right, bool shadow )
	{
		var first = right - ImGui.GetCursorScreenPos().x;
		var rest = right - left;
		if ( line.Wrap is not { } wrap || wrap.First != first || wrap.Rest != rest )
			line.Wrap = wrap = new WrappedText( first, rest, Segment( line, WrapWords( line.Message, first, rest ) ) );
		foreach ( var row in wrap.Rows )
		{
			for ( var index = 0; index < row.Length; index++ )
			{
				if ( index > 0 ) ImGui.SameLine( 0, 0 );
				ShadowedText( row[ index ].Text, row[ index ].Mention ? MentionText : color, right, shadow );
			}
		}
	}

	/// <summary>Greedy word wrap as (start, end) ranges of the message: the first row fits in first pixels, the rest in rest.</summary>
	private static List<(int Start, int End)> WrapWords( string text, float first, float rest )
	{
		var rows = new List<(int Start, int End)>();
		float Limit() => rows.Count == 0 ? first : rest;
		bool Fits( int start, int end ) => ImGui.CalcTextSize( text[ start..end ] ).x <= Limit();
		int rowStart = 0, rowEnd = 0, position = 0;
		foreach ( var word in text.Split( ' ' ) )
		{
			var wordStart = position;
			var wordEnd = position + word.Length;
			position = wordEnd + 1;
			if ( Fits( rowStart, wordEnd ) ) { rowEnd = wordEnd; continue; }
			if ( rowEnd > rowStart ) rows.Add( (rowStart, rowEnd) );
			rowStart = wordStart;
			rowEnd = wordEnd;
			// A word longer than a whole row breaks wherever it has to.
			while ( rowEnd - rowStart > 1 && !Fits( rowStart, rowEnd ) )
			{
				var fit = rowEnd - 1;
				while ( fit > rowStart + 1 && !Fits( rowStart, fit ) ) fit--;
				rows.Add( (rowStart, fit) );
				rowStart = fit;
			}
		}
		rows.Add( (rowStart, rowEnd) );
		return rows;
	}

	/// <summary>Cuts each row into plain and mention pieces.</summary>
	private static Piece[][] Segment( ChatEntry line, List<(int Start, int End)> rows )
	{
		var result = new Piece[ rows.Count ][];
		for ( var index = 0; index < rows.Count; index++ )
		{
			var (start, end) = rows[ index ];
			var pieces = new List<Piece>();
			var cursor = start;
			foreach ( var mention in line.Mentions )
			{
				var mentionStart = Math.Max( mention.Start, start );
				var mentionEnd = Math.Min( mention.Start + mention.Length, end );
				if ( mentionStart >= mentionEnd ) continue;
				if ( mentionStart > cursor ) pieces.Add( new Piece( line.Message[ cursor..mentionStart ], false ) );
				pieces.Add( new Piece( line.Message[ mentionStart..mentionEnd ], true ) );
				cursor = mentionEnd;
			}
			if ( end > cursor || pieces.Count == 0 ) pieces.Add( new Piece( line.Message[ cursor..end ], false ) );
			result[ index ] = pieces.ToArray();
		}
		return result;
	}

	private readonly record struct Piece( string Text, bool Mention );

	private sealed record WrappedText( float First, float Rest, Piece[][] Rows );

	// Avatars load once per player; the engine fills them in when Steam answers.
	private readonly Dictionary<string, Texture> _avatars = new();
	private const float AvatarRounding = 3;

	/// <summary>The speaker's Steam avatar, one text line tall, in front of their prefix.</summary>
	private void DrawAvatar( string steamId )
	{
		if ( !_avatars.TryGetValue( steamId, out var avatar ) ) _avatars[ steamId ] = avatar = Texture.Load( $"avatarsmall:{steamId}", false );
		var size = ImGui.GetTextLineHeight();
		var position = ImGui.GetCursorScreenPos();
		if ( avatar is not null )
			ImGui.GetWindowDrawList().AddImageRounded( avatar, position, position + new Vector2( size, size ), Vector2.Zero, Vector2.One,
				ImGui.GetColorU32( Vector4.One ), AvatarRounding );
		ImGui.Dummy( new Vector2( size, size ) );
	}

	/// <summary>The gold wash behind a line that mentions this player, sized from the rows it wrapped to last frame.</summary>
	private static void DrawMentionWash( ChatEntry line, float left, float right )
	{
		var top = ImGui.GetCursorScreenPos();
		var height = (line.Wrap?.Rows.Length ?? 1) * ImGui.GetTextLineHeightWithSpacing();
		var draw = ImGui.GetWindowDrawList();
		draw.AddRectFilled( new Vector2( left - 4, top.y - 1 ), new Vector2( right, top.y + height - 1 ), ImGui.GetColorU32( MentionWash ) );
		draw.AddRectFilled( new Vector2( left - 4, top.y - 1 ), new Vector2( left - 4 + MentionEdgeWidth, top.y + height - 1 ), ImGui.GetColorU32( MentionEdge ) );
	}

	/// <summary>The announcement tag in grey, thickened to look bold.</summary>
	private static void DrawTag( string tag, bool shadow )
	{
		var position = ImGui.GetCursorScreenPos();
		var draw = ImGui.GetWindowDrawList();
		if ( shadow ) draw.AddText( ImGui.GetFontSize(), position + Vector2.One, ImGui.GetColorU32( Shadow ), tag );
		// Drawing the text twice, a pixel apart, thickens it where the font has no bold weight.
		draw.AddText( ImGui.GetFontSize(), position + new Vector2( 1, 0 ), ImGui.GetColorU32( TagText ), tag );
		ImGui.TextColored( TagText, "{0}", tag );
	}

	private static void ShadowedText( string text, Vector4 color, float right, bool shadow )
	{
		if ( shadow )
		{
			var position = ImGui.GetCursorScreenPos();
			ImGui.GetWindowDrawList().AddText( ImGui.GetFontSize(), position + Vector2.One, ImGui.GetColorU32( Shadow ), text,
				MathF.Max( 1, right - position.x ) );
		}
		ImGui.TextColored( color, "{0}", text );
	}
}
