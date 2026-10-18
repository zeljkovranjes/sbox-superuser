using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Storage;
using System.Text.Json;

namespace SboxSuperuser;

/// <summary>One replacement state and its audit intents committed as a single aggregate, without executing game actions.</summary>
public sealed class PolicyTransaction
{
	private readonly string _payload;

	/// <summary>Snapshots JSON state and intents; module-specific policy validation remains the module's responsibility.</summary>
	public PolicyTransaction( string stateJson, IEnumerable<AuditIntent> auditIntents )
	{
		ArgumentNullException.ThrowIfNull( stateJson );
		var intents = ContractGuard.Copy( auditIntents, nameof( auditIntents ) );
		_payload = "{\"state\":" + stateJson + ",\"auditIntents\":["
			+ string.Join( ",", intents.Select( intent => intent.ToJson() ) ) + "]}";
		try
		{
			TransactionFormat.Validate( _payload );
		}
		catch ( Exception error ) when ( error is FormatException or JsonException or InvalidOperationException )
		{
			throw new ArgumentException( "State or audit intents violate the transaction schema.", nameof( stateJson ), error );
		}
	}

	/// <summary>Creates an expected-revision write; persistence is performed only by the store.</summary>
	public StoreWriteRequest CreateWriteRequest( StoreKey key, long expectedRevision )
	{
		if ( expectedRevision < 0 || expectedRevision == long.MaxValue )
		{
			throw new ArgumentOutOfRangeException( nameof( expectedRevision ) );
		}
		return new StoreWriteRequest( key, new VersionedStoreDocument( 1, expectedRevision + 1, _payload ), expectedRevision );
	}
}
