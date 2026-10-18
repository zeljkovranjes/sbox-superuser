using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>Untrusted view request; the host authenticates and authorizes every query.</summary>
public sealed class AdminQuery
{
	/// <summary>Gets the requested view.</summary>
	public AdminQueryKind Kind { get; }

	/// <summary>Gets a name/ID search for Players or an exact Steam ID for Player.</summary>
	public string Search { get; }

	/// <summary>Gets the zero-based player or history page; activity returns the latest 20 records.</summary>
	public int Page { get; }

	/// <summary>Creates a bounded query; activity accepts no search or page.</summary>
	public AdminQuery( AdminQueryKind kind, string search = "", int page = 0 )
	{
		ContractGuard.EnumValue( kind, nameof( kind ) );
		if ( search is null || search.Length > 256 || search.Any( char.IsControl ) || page is < 0 or > 64
			|| kind == AdminQueryKind.Activity && (search.Length != 0 || page != 0) )
		{
			throw new ArgumentException( "Invalid query search or page." );
		}
		if ( kind == AdminQueryKind.Player ) SteamIdentity.Validate( search, nameof( search ) );
		Kind = kind;
		Search = search;
		Page = page;
	}
}
