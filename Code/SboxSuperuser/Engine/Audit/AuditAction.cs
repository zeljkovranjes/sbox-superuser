using SboxSuperuser.Core.Audit;

namespace SboxSuperuser.Engine.Audit;

internal sealed class AuditAction
{
	private readonly AuditRecord _template;
	internal Guid Id => _template.Id;
	internal IReadOnlyList<AuditIntent> Intents { get; }

	internal AuditAction( ActorIdentity actor, CommandOrigin origin, Guid requestId, string command, DateTimeOffset utc,
		IReadOnlyDictionary<string, string> safeArguments = null, IEnumerable<string> targets = null, Guid? id = null )
	{
		_template = new AuditRecord( 1, id ?? Guid.NewGuid(), requestId, actor.Kind.ToString(), actor.SubjectId,
			origin.ToString(), command, safeArguments, targets, utc );
		var intents = new List<AuditIntent>();
		for ( var offset = 0; offset < Math.Max( _template.Targets.Count, 1 ); offset += 64 )
		{
			intents.Add( new AuditIntent( offset == 0 ? Id : Guid.NewGuid(), requestId, actor.Kind, actor.SubjectId,
				origin, command, utc, _template.SafeArguments, _template.Targets.Skip( offset ).Take( 64 ) ) );
		}

		Intents = intents.AsReadOnly();
	}

	internal AuditRecord Record( long sequence, CommandStatus? outcome = null, DateTimeOffset? completedUtc = null )
	{
		return new AuditRecord( sequence, Id, _template.RequestId, _template.ActorKind, _template.ActorSubject, _template.Origin,
			_template.Command, _template.SafeArguments, _template.Targets, _template.CreatedUtc,
			outcome?.ToString() ?? "Unknown", completedUtc < _template.CreatedUtc ? _template.CreatedUtc : completedUtc );
	}
}
