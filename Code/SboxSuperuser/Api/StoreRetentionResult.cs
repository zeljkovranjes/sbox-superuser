using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Observed result of bounded physical audit retention; never applies to policy or owner backups.</summary>
public sealed class StoreRetentionResult
{
	/// <summary>Gets Committed when the checked retention batch completed, otherwise an explicit failure.</summary>
	public StoreWriteStatus Status { get; }

	/// <summary>Gets whether more superseded audit generations remain.</summary>
	public bool HasMore { get; }

	/// <summary>Creates a retention outcome without deleting anything.</summary>
	public StoreRetentionResult( StoreWriteStatus status, bool hasMore = false )
	{
		Status = ContractGuard.EnumValue( status, nameof( status ) );
		HasMore = hasMore;
	}
}
