namespace SboxSuperuser.Core.Prefixes;

/// <summary>Splits a display template such as "%prefix% %name%" into literal text and the two placeholders.</summary>
internal static class PrefixTemplate
{
	internal const string Default = "%prefix% %name%";
	internal const string Prefix = "%prefix%";
	internal const string Name = "%name%";
	internal const int MaximumLength = 64;

	internal static string Validate( string template )
	{
		template = string.IsNullOrWhiteSpace( template ) ? Default : template;
		if ( template.Length > MaximumLength || template.Any( char.IsControl ) )
			throw new ArgumentException( $"Templates are one line of at most {MaximumLength} characters.", nameof( template ) );
		if ( Count( template, Name ) != 1 || Count( template, Prefix ) > 1 )
			throw new ArgumentException( "A template needs %name% exactly once and %prefix% at most once.", nameof( template ) );
		return template;
	}

	/// <summary>Literal text, "%prefix%" and "%name%" pieces in order; empty literals are left out.</summary>
	internal static IReadOnlyList<string> Split( string template )
	{
		var pieces = new List<string>();
		var rest = template;
		while ( rest.Length > 0 )
		{
			var prefix = rest.IndexOf( Prefix, StringComparison.Ordinal );
			var name = rest.IndexOf( Name, StringComparison.Ordinal );
			var next = prefix < 0 ? name : name < 0 ? prefix : Math.Min( prefix, name );
			if ( next < 0 )
			{
				pieces.Add( rest );
				break;
			}
			if ( next > 0 ) pieces.Add( rest.Substring( 0, next ) );
			var token = next == prefix ? Prefix : Name;
			pieces.Add( token );
			rest = rest.Substring( next + token.Length );
		}
		return pieces;
	}

	/// <summary>The template filled in with plain text.</summary>
	internal static string Format( string template, string tag, string name ) =>
		string.Concat( Split( template ).Select( piece => piece == Prefix ? tag : piece == Name ? name : piece ) );

	private static int Count( string text, string token )
	{
		var count = 0;
		for ( var index = text.IndexOf( token, StringComparison.Ordinal ); index >= 0; index = text.IndexOf( token, index + token.Length, StringComparison.Ordinal ) )
			count++;
		return count;
	}
}
