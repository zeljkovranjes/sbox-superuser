namespace SboxSuperuser.Core.Prefixes;

/// <summary>A number fixed per player (FNV-1a of the Steam ID), so effects can differ between players.</summary>
internal static class PrefixSeed
{
	internal static int Of( string steamId )
	{
		unchecked
		{
			var hash = 2166136261u;
			foreach ( var character in steamId ?? "" ) hash = (hash ^ character) * 16777619u;
			return (int)(hash & 0x7FFFFFFF);
		}
	}
}
