using System.Collections.ObjectModel;
using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Audit;

internal sealed class AuditRecord
{
	internal long Sequence { get; }
	internal Guid Id { get; }
	internal Guid RequestId { get; }
	internal string ActorKind { get; }
	internal string ActorSubject { get; }
	internal string Origin { get; }
	internal string Command { get; }
	internal IReadOnlyDictionary<string, string> SafeArguments { get; }
	internal IReadOnlyList<string> Targets { get; }
	internal DateTimeOffset CreatedUtc { get; }
	internal DateTimeOffset? CompletedUtc { get; }
	internal string Outcome { get; }

	internal AuditRecord( long sequence, Guid id, Guid requestId, string actorKind, string actorSubject, string origin, string command,
		IReadOnlyDictionary<string, string> safeArguments, IEnumerable<string> targets, DateTimeOffset createdUtc,
		string outcome = "Unknown", DateTimeOffset? completedUtc = null )
	{
		if ( sequence < 1 || id == Guid.Empty || requestId == Guid.Empty || createdUtc.Offset != TimeSpan.Zero
			|| completedUtc.HasValue && (completedUtc.Value.Offset != TimeSpan.Zero || completedUtc.Value < createdUtc)
			|| outcome != "Unknown" && !completedUtc.HasValue )
		{
			throw new ArgumentException( "Audit identifiers, sequence and UTC timestamps must be explicit and ordered." );
		}

		ValidateActor( actorKind, actorSubject, origin );
		ContractGuard.Key( command, nameof( command ) );
		if ( command.Length > 96 || actorSubject.Length > 96 )
		{
			throw new ArgumentException( "Audit identity and command keys exceed their limits." );
		}

		if ( outcome is not ("Unknown" or "Success" or "NoChange" or "InvalidArguments" or "Denied" or "NotFound"
			or "Unsupported" or "Cancelled" or "Failed" or "Conflict" or "StorageUnavailable") )
		{
			throw new ArgumentException( "Unknown audit outcome." );
		}

		var arguments = new Dictionary<string, string>( StringComparer.Ordinal );
		foreach ( var pair in safeArguments ?? new Dictionary<string, string>() )
		{
			ContractGuard.Key( pair.Key, nameof( safeArguments ) );
			if ( pair.Key.Length > 96 || pair.Value is null || pair.Value.Length > 256 || arguments.Count >= 32 )
			{
				throw new ArgumentException( "Safe audit arguments exceed their limits." );
			}

			arguments.Add( pair.Key, pair.Value );
		}

		var targetCopy = (targets ?? Array.Empty<string>()).Take( 257 ).ToArray();
		if ( targetCopy.Length > 256 || targetCopy.Distinct( StringComparer.Ordinal ).Count() != targetCopy.Length )
		{
			throw new ArgumentException( "Audit target IDs must be unique, with at most 256 targets." );
		}

		foreach ( var target in targetCopy )
		{
			SteamIdentity.Validate( target, nameof( targets ) );
		}

		Sequence = sequence;
		Id = id;
		RequestId = requestId;
		ActorKind = actorKind;
		ActorSubject = actorSubject;
		Origin = origin;
		Command = command;
		SafeArguments = new ReadOnlyDictionary<string, string>( arguments );
		Targets = Array.AsReadOnly( targetCopy );
		CreatedUtc = createdUtc;
		Outcome = outcome;
		CompletedUtc = completedUtc;
	}

	internal AuditRecord Complete( string outcome, DateTimeOffset utc )
	{
		if ( CompletedUtc.HasValue )
		{
			return Outcome == outcome ? this : throw new InvalidOperationException( "A recorded outcome cannot be replaced." );
		}

		return new AuditRecord( Sequence, Id, RequestId, ActorKind, ActorSubject, Origin, Command, SafeArguments, Targets,
			CreatedUtc, outcome, utc < CreatedUtc ? CreatedUtc : utc );
	}

	private static void ValidateActor( string kind, string subject, string origin )
	{
		if ( kind == "Player" && origin is "Console" or "Chat" or "Interface" )
		{
			SteamIdentity.Validate( subject, nameof( subject ) );
			return;
		}

		if ( kind == "DedicatedConsole" && origin == "Console" && subject == "dedicated-console" )
		{
			return;
		}

		if ( kind == "InternalService" && origin == "InternalService" )
		{
			ContractGuard.Key( subject, nameof( subject ) );
			return;
		}

		throw new ArgumentException( "Audit actor and source do not match." );
	}
}
