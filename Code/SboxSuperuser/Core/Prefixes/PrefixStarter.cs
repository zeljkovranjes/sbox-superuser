namespace SboxSuperuser.Core.Prefixes;

/// <summary>
/// Prefixes a new server starts with, so the feature shows up straight away. They live in memory until the first
/// change is saved; delete them like any other prefix.
/// </summary>
internal static class PrefixStarter
{
	internal static PrefixCatalog Catalog() => PrefixCatalog.Empty
		.Save( PrefixDefinition.Create( "owner", "Owner", "[OWNER]", tagStyle: Preset( "rain" ) ) )
		.Save( PrefixDefinition.Create( "admin", "Admin", "[ADMIN]", tagStyle: Preset( "cyln" ) ) )
		.Save( PrefixDefinition.Create( "mod", "Moderator", "[MOD]", tagStyle: Preset( "cyan" ) ) )
		.AssignGroup( "owner", "owner" )
		.AssignGroup( "admin", "admin" )
		.AssignGroup( "moderator", "mod" );

	private static PrefixStyle Preset( string id ) => new( PrefixStyleKind.Preset, id );
}
