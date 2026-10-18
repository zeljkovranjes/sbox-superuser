using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Permissions;

internal sealed class PolicyResolver
{
	private readonly Dictionary<string, PolicyDecision> _cache = new( StringComparer.Ordinal );
	private HashSet<string> _registered;
	internal PolicyGraph Policy { get; private set; }
	internal long RegistryRevision { get; private set; }

	internal PolicyResolver( PolicyGraph policy, IEnumerable<string> registered )
	{
		Policy = policy ?? throw new ArgumentNullException( nameof( policy ) );
		ReplaceRegistered( registered );
	}

	internal void ReplacePolicy( PolicyGraph policy )
	{
		ArgumentNullException.ThrowIfNull( policy );
		if ( policy.Revision <= Policy.Revision )
		{
			throw new ArgumentException( "Replacement policy must have a strictly newer revision.", nameof( policy ) );
		}

		Policy = policy;
		_cache.Clear();
	}

	internal void ReplaceRegistered( IEnumerable<string> permissions )
	{
		ArgumentNullException.ThrowIfNull( permissions );
		var candidate = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var permission in permissions )
		{
			ContractGuard.Key( permission, nameof( permissions ) );
			if ( !candidate.Add( permission ) || candidate.Count > 4096 )
			{
				throw new ArgumentException( "Registered permission keys must be unique, with at most 4096 keys.", nameof( permissions ) );
			}
		}

		if ( RegistryRevision == long.MaxValue )
		{
			throw new InvalidOperationException( "Permission registry revision exhausted; restart the runtime." );
		}

		_registered = candidate;
		RegistryRevision++;
		_cache.Clear();
	}

	internal PolicyDecision Explain( string steamId, string permission )
	{
		SteamIdentity.Validate( steamId, nameof( steamId ) );
		ContractGuard.Key( permission, nameof( permission ) );
		var key = steamId + "|" + permission;
		if ( _cache.TryGetValue( key, out var cached ) )
		{
			return cached;
		}

		var result = PolicyResolution.Evaluate( Policy, steamId, permission, _registered.Contains( permission ), RegistryRevision );
		// A bounded disposable cache; policy and registry replacements invalidate every decision together.
		if ( _cache.Count >= 1024 )
		{
			_cache.Clear();
		}

		_cache.Add( key, result );
		return result;
	}

	internal bool IsRegistered( string permission )
	{
		ContractGuard.Key( permission, nameof( permission ) );
		return _registered.Contains( permission );
	}
}
