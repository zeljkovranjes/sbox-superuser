using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	internal bool IsOwner( string steamId ) => IsUsable && _owners.Contains( steamId );

	private bool CanManage( ActorIdentity actor ) => actor is not null
		&& (actor.Kind == ActorKind.DedicatedConsole || actor.Kind == ActorKind.Player && IsOwner( actor.SubjectId ));

	internal async Task<CommandResult> ApplyAsync( Func<ActorIdentity> authenticate, PolicyChange change,
		CommandOrigin origin, Guid requestId, CancellationToken cancellation )
	{
		ArgumentNullException.ThrowIfNull( change );
		return await SerializedAsync( async () =>
		{
			var actor = authenticate();
			if ( !CanManage( actor ) )
			{
				return new CommandResult( CommandStatus.Denied, "Only an authenticated owner or dedicated local console may change security policy." );
			}

			if ( !IsUsable )
			{
				return new CommandResult( CommandStatus.StorageUnavailable, "Authority data is unavailable or corrupt; use local recovery." );
			}

			var candidate = PolicyEdits.Apply( _resolver.Policy, change.Edit );
			if ( ReferenceEquals( candidate, _resolver.Policy ) )
			{
				return new CommandResult( CommandStatus.NoChange, "Policy already has the requested value." );
			}

			var intent = new AuditIntent( Guid.NewGuid(), requestId, actor.Kind, actor.SubjectId, origin,
				"security.policy.change", DateTimeOffset.UtcNow,
				new Dictionary<string, string> { [ "operation" ] = change.Edit.Kind.ToString() } );
			var transaction = new PolicyTransaction( PolicyCodec.WriteState( candidate ), new[] { intent } );
			var result = await _store.WriteAsync( transaction.CreateWriteRequest( PolicyKey, _resolver.Policy.Revision ), cancellation );
			LastWriteStatus = result.Status;
			if ( result.Status == StoreWriteStatus.Committed )
			{
				_resolver.ReplacePolicy( candidate );
				PolicyStatus = StoreReadStatus.Found;
			}
			else if ( result.Status is StoreWriteStatus.Unknown or StoreWriteStatus.Conflict or StoreWriteStatus.Corrupt )
			{
				await LoadAsync( CancellationToken.None );
			}

			return MutationResult( result );
		}, cancellation );
	}

	private static CommandResult MutationResult( StoreWriteResult result )
	{
		return result.Status switch
		{
			StoreWriteStatus.Committed => new CommandResult( CommandStatus.Success, "Authority change persisted.",
				new Dictionary<string, string> { [ "revision" ] = result.CommittedRevision.Value.ToString() } ),
			StoreWriteStatus.Conflict => new CommandResult( CommandStatus.Conflict, "Stored revision changed; inspect current policy before retrying." ),
			StoreWriteStatus.Cancelled => new CommandResult( CommandStatus.Cancelled, "Change cancelled before a commit attempt." ),
			StoreWriteStatus.Unknown => new CommandResult( CommandStatus.Unknown, "Persistence outcome is unknown; inspect storage before retrying." ),
			StoreWriteStatus.InvalidData => new CommandResult( CommandStatus.InvalidArguments, "Candidate exceeds or violates the supported storage schema." ),
			_ => new CommandResult( CommandStatus.StorageUnavailable, "Required authority storage is unavailable; no success reported." )
		};
	}
}
