using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>A write outcome; interrupted or indeterminate writes report Unknown, never fabricated success.</summary>
public sealed class StoreWriteResult
{
	/// <summary>Gets the observed write outcome.</summary>
	public StoreWriteStatus Status { get; }

	/// <summary>Gets the confirmed persisted revision only for Committed, otherwise null.</summary>
	public long? CommittedRevision { get; }

	/// <summary>Creates outcome data only; the storage adapter must establish any committed claim.</summary>
	public StoreWriteResult( StoreWriteStatus status, long? committedRevision = null )
	{
		Status = ContractGuard.EnumValue( status, nameof( status ) );
		if ( (status == StoreWriteStatus.Committed) != committedRevision.HasValue
			|| committedRevision < 1 )
		{
			throw new ArgumentException( "Only Committed must carry a positive revision.", nameof( committedRevision ) );
		}

		CommittedRevision = committedRevision;
	}
}
