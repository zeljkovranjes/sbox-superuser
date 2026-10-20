namespace SboxSuperuser.Core.Bootstrap;

/// <summary>What the startup log says about one module: who made it, what it added, or why it didn't load.</summary>
internal sealed record ModuleReport( string Id, string Version, string Author, bool Loaded, string Problem,
	int Commands, int Permissions, int Pages, int Windows );
