namespace SboxSuperuser.Core.Modules;

internal sealed record ModuleNode( string Id, int Version, IReadOnlyList<ModuleRequirement> Dependencies );
