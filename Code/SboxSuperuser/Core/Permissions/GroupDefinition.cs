using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser.Core.Permissions;

internal sealed class GroupDefinition
{
	internal string Id { get; }
	internal IReadOnlyList<string> Parents { get; }
	internal IReadOnlyDictionary<string, int> Entries { get; }
	internal int ProtectionLevel { get; }

	internal GroupDefinition( string id, IEnumerable<string> parents,
		IEnumerable<KeyValuePair<string, int>> entries, int protectionLevel )
	{
		Id = ContractGuard.Key( id, nameof( id ) );
		if ( id == "owner" || protectionLevel is < 0 or > TargetProtection.MaximumGroupLevel )
		{
			throw new ArgumentException( $"Owner is reserved; group protection must be between 0 and {TargetProtection.MaximumGroupLevel}." );
		}

		Parents = PolicyEntries.Groups( parents );
		Entries = PolicyEntries.Snapshot( entries );
		ProtectionLevel = protectionLevel;
	}
}
