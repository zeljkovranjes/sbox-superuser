using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>Host-recorded action intent data, never an identity credential or a successful action outcome.</summary>
public sealed class AuditIntent
{
	/// <summary>Gets the unique action ID.</summary>
	public Guid Id { get; }

	/// <summary>Gets the correlated command request ID.</summary>
	public Guid RequestId { get; }

	private readonly string _json;

	/// <summary>Snapshots caller-safe audit fields supplied by trusted host code; outcome is always Unknown.</summary>
	public AuditIntent( Guid id, Guid requestId, ActorKind actorKind, string actorSubject,
		CommandOrigin origin, string command, DateTimeOffset createdUtc,
		IReadOnlyDictionary<string, string> safeArguments = null, IEnumerable<string> targets = null )
	{
		Id = id;
		RequestId = requestId;
		var arguments = safeArguments is null ? new Dictionary<string, string>()
			: safeArguments.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		var targetCopy = ContractGuard.Copy( targets ?? Array.Empty<string>(), nameof( targets ) );
		_json = JsonSerializer.Serialize( new
		{
			id, requestId, actorKind = actorKind.ToString(), actorSubject, origin = origin.ToString(), command,
			safeArguments = arguments, targets = targetCopy,
			createdUtc = createdUtc.ToString( "O", CultureInfo.InvariantCulture ), outcome = "Unknown"
		} );
		Core.Storage.TransactionFormat.Validate( "{\"state\":{},\"auditIntents\":[" + _json + "]}" );
	}

	internal string ToJson() => _json;
}
