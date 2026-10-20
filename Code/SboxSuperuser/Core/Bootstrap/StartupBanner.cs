using System.Text;

namespace SboxSuperuser.Core.Bootstrap;

/// <summary>The text Superuser prints to the console when it starts and as its modules load.</summary>
internal static class StartupBanner
{
	// ANSI Shadow, the block-letter style most server consoles render cleanly.
	private static readonly string[] Logo =
	{
		"███████╗██╗   ██╗██████╗ ███████╗██████╗ ██╗   ██╗███████╗███████╗██████╗ ",
		"██╔════╝██║   ██║██╔══██╗██╔════╝██╔══██╗██║   ██║██╔════╝██╔════╝██╔══██╗",
		"███████╗██║   ██║██████╔╝█████╗  ██████╔╝██║   ██║███████╗█████╗  ██████╔╝",
		"╚════██║██║   ██║██╔═══╝ ██╔══╝  ██╔══██╗██║   ██║╚════██║██╔══╝  ██╔══██╗",
		"███████║╚██████╔╝██║     ███████╗██║  ██║╚██████╔╝███████║███████╗██║  ██║",
		"╚══════╝ ╚═════╝ ╚═╝     ╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚══════╝╚═╝  ╚═╝"
	};

	/// <summary>The logo with the version line under it and where the owners come from.</summary>
	internal static string Header( string version, string author, int owners, string ownerFile )
	{
		var text = new StringBuilder();
		text.AppendLine();
		foreach ( var line in Logo ) text.AppendLine( "  " + line );
		text.AppendLine();
		text.AppendLine( $"  Server administration for s&box · v{version} by {author}" );
		text.Append( owners == 0
			? $"  No owners yet: add your Steam ID to {ownerFile}"
			: $"  {owners} owner{Plural( owners )} from {ownerFile}" );
		return text.ToString();
	}

	/// <summary>One line per module: a check and what it added, or a cross and why it didn't load.</summary>
	internal static string Module( ModuleReport module )
	{
		if ( !module.Loaded ) return $"  ✗ {module.Id}: {module.Problem}";
		var who = module.Version.Length > 0 ? $" {module.Version}" : "";
		if ( module.Author.Length > 0 ) who += $" by {module.Author}";
		var parts = new List<string>
		{
			Count( module.Commands, "command" ),
			Count( module.Permissions, "permission" )
		};
		if ( module.Pages > 0 ) parts.Add( Count( module.Pages, "guide page" ) );
		if ( module.Windows > 0 ) parts.Add( Count( module.Windows, "menu window" ) );
		return $"  ✓ {module.Id}{who} · {string.Join( " · ", parts )}";
	}

	/// <summary>The closing line once every module has had its turn.</summary>
	internal static string Ready( IReadOnlyCollection<ModuleReport> modules, string root )
	{
		var loaded = modules.Count( module => module.Loaded );
		var failed = modules.Count - loaded;
		var commands = modules.Where( module => module.Loaded ).Sum( module => module.Commands );
		var summary = $"Ready: {Count( loaded, "module" )}, {Count( commands, "command" )}";
		if ( failed > 0 ) summary += $", {failed} not loaded (see above)";
		return $"  {summary}. Type \"{root} help\" in the console or \"/{root} help\" in chat.";
	}

	private static string Count( int count, string noun ) => $"{count} {noun}{Plural( count )}";

	private static string Plural( int count ) => count == 1 ? "" : "s";
}
