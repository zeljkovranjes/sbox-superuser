using SboxSuperuser.Core.Moderation;

namespace SboxSuperuser.Engine.Moderation;

internal sealed partial class HostModerationService
{
	internal async Task<CommandResult> ApplyAsync( CommandContext context, string command, string permission,
		ModerationKind kind, string target, string reason, CommandDuration duration )
	{
		await Initialization;
		await _gate.WaitAsync( context.Cancellation );
		try
		{
			context.Cancellation.ThrowIfCancellationRequested();
			if ( !_canUse( context, permission ) ) return Denied();
			if ( !IsUsable ) return Unavailable();
			if ( kind is ModerationKind.Kick or ModerationKind.Ban or ModerationKind.Warn or ModerationKind.TextMute
				&& !_canTarget( context.Actor, target ) )
				return new CommandResult( CommandStatus.Denied, "Target protection denied this action." );
			var now = _clock();
			var restrictedKind = kind is ModerationKind.Unban ? ModerationKind.Ban : kind is ModerationKind.Unmute ? ModerationKind.TextMute : kind;
			var active = _ledger.Active( target, restrictedKind, now );
			var revoking = kind is ModerationKind.Unban or ModerationKind.Unmute;
			if ( revoking && active is null ) return new CommandResult( CommandStatus.NoChange, "No active restriction exists; history was retained." );
			if ( !revoking && active is not null ) return new CommandResult( CommandStatus.Conflict, "An active restriction already exists; revoke it explicitly before replacing it." );
			DateTimeOffset? expires = null;
			if ( kind is ModerationKind.Ban or ModerationKind.TextMute )
			{
				if ( duration is null ) return new CommandResult( CommandStatus.InvalidArguments, "An explicit positive duration or permanent is required." );
				try { expires = duration.Value.HasValue ? now.Add( duration.Value.Value ) : null; }
				catch ( ArgumentOutOfRangeException ) { return new CommandResult( CommandStatus.InvalidArguments, "Duration exceeds supported UTC dates." ); }
			}
			ModerationEntry entry;
			ModerationLedger candidate;
			string state;
			try
			{
				entry = new ModerationEntry( Guid.NewGuid(), context.RequestId, target, kind, context.Actor.Kind.ToString(),
					context.Actor.SubjectId, context.Origin.ToString(), reason, now, expires, revoking ? active.Id : null );
				candidate = _ledger.Append( entry );
				state = ModerationCodec.WriteState( candidate );
			}
			catch ( Exception error ) when ( error is ArgumentException or InvalidOperationException )
			{
				return new CommandResult( CommandStatus.InvalidArguments, "Reason must be printable and 1–256 characters; moderation history is limited to 512 records and 384000 encoded bytes." );
			}
			var intent = new AuditIntent( entry.Id, context.RequestId, context.Actor.Kind, context.Actor.SubjectId,
				context.Origin, command, now, targets: new[] { target } );
			var result = await _store.WriteAsync( new PolicyTransaction( state, new[] { intent } ).CreateWriteRequest( Key, _ledger.Revision ), context.Cancellation );
			if ( result.Status != StoreWriteStatus.Committed )
			{
				if ( result.Status is StoreWriteStatus.Unknown or StoreWriteStatus.Conflict or StoreWriteStatus.Corrupt ) Status = StoreReadStatus.Unknown;
				return new CommandResult( result.Status == StoreWriteStatus.Unknown ? CommandStatus.Unknown : CommandStatus.StorageUnavailable,
					"Moderation change was not confirmed; no disconnect was attempted." );
			}
			_ledger = candidate;
			Status = StoreReadStatus.Found;
			return new CommandResult( CommandStatus.Success, "Moderation record persisted.", new Dictionary<string, string>
			{
				[ "action" ] = entry.Id.ToString(), [ "request" ] = context.RequestId.ToString(),
				[ "target" ] = target, [ "kind" ] = kind.ToString(),
				[ "expiresUtc" ] = expires?.ToString( "O", System.Globalization.CultureInfo.InvariantCulture ) ?? "none"
			} );
		}
		finally
		{
			_gate.Release();
		}
	}
}
