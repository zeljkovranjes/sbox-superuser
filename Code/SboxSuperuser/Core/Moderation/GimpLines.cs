namespace SboxSuperuser.Core.Moderation;

/// <summary>What a gimped player says instead of their own message.</summary>
internal static class GimpLines
{
	internal static readonly IReadOnlyList<string> Lines = new[]
	{
		"I love this server so much.",
		"The admins here are the best.",
		"I'm a little teapot, short and stout.",
		"Has anyone seen my keyboard?",
		"I promise to behave from now on.",
		"Can someone teach me how to jump?",
		"I think I left the oven on."
	};

	/// <summary>The line said in place of this message; the same message always gives the same line.</summary>
	internal static string For( string message ) => Lines[ (int)((uint)StableHash( message ?? "" ) % Lines.Count) ];

	// string.GetHashCode differs per process; this one doesn't, so tests and replays agree.
	private static int StableHash( string text )
	{
		unchecked
		{
			var hash = 17;
			foreach ( var character in text ) hash = hash * 31 + character;
			return hash;
		}
	}
}
