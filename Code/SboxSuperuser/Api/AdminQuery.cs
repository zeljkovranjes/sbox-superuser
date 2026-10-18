using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>Untrusted view request; the host authenticates and authorizes every query.</summary>
public sealed class AdminQuery
{
	/// <summary>Gets the requested view.</summary>
	public AdminQueryKind Kind { get; }

	/// <summary>Gets a name/ID search for Players, an exact Steam ID for Player, a Steam ID filter for Restrictions, or a command key for ActivitySearch.</summary>
	public string Search { get; }

	/// <summary>Gets the zero-based page; activity returns the latest 20 records.</summary>
	public int Page { get; }

	/// <summary>Gets an exact actor Steam ID filter for ActivitySearch, or empty for any actor.</summary>
	public string Actor { get; } = "";

	/// <summary>Gets an outcome filter for ActivitySearch, or null for any outcome.</summary>
	public CommandStatus? Outcome { get; }

	/// <summary>Creates a bounded query; activity, about, commands and permissions accept no search or page.</summary>
	public AdminQuery( AdminQueryKind kind, string search = "", int page = 0 )
	{
		ContractGuard.EnumValue( kind, nameof( kind ) );
		if ( search is null || search.Length > 256 || search.Any( char.IsControl ) || page is < 0 or > 64
			|| kind is AdminQueryKind.Activity or AdminQueryKind.About or AdminQueryKind.Commands or AdminQueryKind.Permissions
				&& (search.Length != 0 || page != 0) )
		{
			throw new ArgumentException( "Invalid query search or page." );
		}
		if ( kind == AdminQueryKind.Player ) SteamIdentity.Validate( search, nameof( search ) );
		if ( kind == AdminQueryKind.ActivitySearch && search.Length != 0 ) ContractGuard.Key( search, nameof( search ) );
		if ( kind == AdminQueryKind.Chat )
		{
			if ( page != 0 ) throw new ArgumentException( "Chat accepts no page." );
			if ( search.Length != 0 ) SteamIdentity.Validate( search, nameof( search ) );
		}
		Kind = kind;
		Search = search;
		Page = page;
	}

	/// <summary>Creates a filtered activity query; empty command or actor and a null outcome match everything.</summary>
	public AdminQuery( string command, string actor, CommandStatus? outcome, int page = 0 )
		: this( AdminQueryKind.ActivitySearch, command ?? "", page )
	{
		ArgumentNullException.ThrowIfNull( actor );
		if ( actor.Length != 0 ) SteamIdentity.Validate( actor, nameof( actor ) );
		if ( outcome is { } value ) ContractGuard.EnumValue( value, nameof( outcome ) );
		Actor = actor;
		Outcome = outcome;
	}
}
