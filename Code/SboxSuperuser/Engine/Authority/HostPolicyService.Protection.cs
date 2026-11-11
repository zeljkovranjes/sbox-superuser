using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	readonly Dictionary<ulong, int> _protectionLevels = new();
	PolicyGraph _protectionPolicy;
	OwnerSet _protectionOwners;

	internal bool TryGetProtectionLevel( ulong steamId, out int level )
	{
		level = -1;
		if ( !IsUsable || steamId == 0 ) return false;
		if ( !ReferenceEquals( _protectionPolicy, _resolver.Policy ) || !ReferenceEquals( _protectionOwners, _owners ) )
		{
			_protectionLevels.Clear();
			_protectionPolicy = _resolver.Policy;
			_protectionOwners = _owners;
		}
		if ( _protectionLevels.TryGetValue( steamId, out level ) ) return true;
		if ( _protectionLevels.Count >= 256 ) _protectionLevels.Clear();
		level = TargetProtection.Level( _resolver.Policy, _owners, steamId.ToString( System.Globalization.CultureInfo.InvariantCulture ), _resolver.Claims );
		_protectionLevels.Add( steamId, level );
		return true;
	}
}
