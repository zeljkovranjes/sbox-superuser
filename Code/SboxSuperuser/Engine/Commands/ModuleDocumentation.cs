using SboxSuperuser.Core.Documentation;

namespace SboxSuperuser.Engine.Commands;

/// <summary>What a registered module says about itself: its written pages, the permissions it owns and its menu windows.</summary>
internal sealed record ModuleDocumentation( IReadOnlyList<DocumentationPage> Pages, IReadOnlyList<PermissionDescriptor> Permissions,
	IReadOnlyList<AdminMenuWindowRow> Windows = null )
{
	internal IReadOnlyList<AdminMenuWindowRow> Windows { get; init; } = Windows ?? Array.Empty<AdminMenuWindowRow>();
}
