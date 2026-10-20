using SboxSuperuser.Core.Documentation;

namespace SboxSuperuser.Engine.Commands;

/// <summary>What a registered module says about itself: its written pages and the permissions it owns.</summary>
internal sealed record ModuleDocumentation( IReadOnlyList<DocumentationPage> Pages, IReadOnlyList<PermissionDescriptor> Permissions );
