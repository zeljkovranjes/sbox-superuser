using System.Globalization;
using System.Net;
using System.Text;

namespace SboxSuperuser;

/// <summary>One frame of a prefixed name: styled runs, plus markup and plain text forms.</summary>
public sealed class StyledLine
{
	internal StyledLine( IReadOnlyList<StyledRun> runs ) => Runs = runs;

	/// <summary>The runs, in drawing order.</summary>
	public IReadOnlyList<StyledRun> Runs { get; }

	/// <summary>The line as drawn this frame, without styling. Use PlayerPrefix.Format for stable text in logs.</summary>
	public string ToPlain() => string.Concat( Runs.Select( run => new string( ' ', Spaces( run ) ) + run.Text ) );

	/// <summary>Inline HTML for a Label with IsRich set; every piece of text is escaped.</summary>
	public string ToMarkup()
	{
		var markup = new StringBuilder();
		foreach ( var run in Runs )
		{
			for ( var space = Spaces( run ); space > 0; space-- ) markup.Append( "&nbsp;" );
			markup.Append( "<span style=\"color: " ).Append( run.Color.WithAlpha( 1 ).Hex );
			if ( run.Color.a < 1 ) markup.Append( "; opacity: " ).Append( run.Color.a.ToString( "0.##", CultureInfo.InvariantCulture ) );
			if ( run.Bold ) markup.Append( "; font-weight: bold" );
			if ( run.Italic ) markup.Append( "; font-style: italic" );
			markup.Append( "\">" ).Append( WebUtility.HtmlEncode( run.Text ) ).Append( "</span>" );
		}
		return markup.ToString();
	}

	private static int Spaces( StyledRun run ) => (int)MathF.Round( run.Space );
}
