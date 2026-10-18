using SboxSuperuser.Core.Permissions;

namespace SboxSuperuser.Engine.Authority;

internal sealed partial class HostPolicyService
{
	internal Task<CommandResult> ChangeOwnerAsync( Func<ActorIdentity> authenticate, string steamId, bool add,
		Guid requestId, CancellationToken cancellation )
	{
		return WriteOwnersAsync( authenticate, current => current.Change( steamId, add ), false,
			add ? "security.owner.add" : "security.owner.remove", requestId, cancellation );
	}

	internal Task<CommandResult> RecoverOwnersAsync( Func<ActorIdentity> authenticate, IEnumerable<string> explicitOwners,
		Guid requestId, CancellationToken cancellation )
	{
		var replacement = new OwnerSet( explicitOwners );
		return WriteOwnersAsync( authenticate, _ => replacement, true, "security.owner.recover", requestId, cancellation );
	}

	private async Task<CommandResult> WriteOwnersAsync( Func<ActorIdentity> authenticate, Func<OwnerSet, OwnerSet> change,
		bool recovery, string command, Guid requestId, CancellationToken cancellation )
	{
		return await SerializedAsync( async () =>
		{
			var actor = authenticate();
			if ( actor?.Kind != ActorKind.DedicatedConsole )
			{
				return new CommandResult( CommandStatus.Denied, "Owner membership changes require the non-forwarded dedicated local console." );
			}

			var stored = await _store.ReadAsync( OwnersKey, 1, cancellation );
			if ( stored.Status is not (StoreReadStatus.Found or StoreReadStatus.Missing)
				|| stored.Status == StoreReadStatus.Missing && _managedOwners )
			{
				OwnerStatus = stored.Status == StoreReadStatus.Missing ? StoreReadStatus.Corrupt : stored.Status;
				return new CommandResult( CommandStatus.StorageUnavailable, "Managed owner storage needs compatible-backup recovery; existing data was preserved." );
			}

			if ( !recovery )
			{
				await LoadAsync( cancellation );
				if ( OwnerStatus is not (StoreReadStatus.Found or StoreReadStatus.Missing) )
				{
					return new CommandResult( CommandStatus.StorageUnavailable, "Owner configuration is invalid; use explicit local owner recovery." );
				}
			}

			OwnerSet candidate;
			try
			{
				candidate = change( _owners );
			}
			catch ( InvalidOperationException )
			{
				return new CommandResult( CommandStatus.Denied, "Final owner removal refused; only explicit local recovery may clear all owners." );
			}

			if ( ReferenceEquals( candidate, _owners ) || _managedOwners && candidate.Ids.SequenceEqual( _owners.Ids ) && OwnerStatus == StoreReadStatus.Found )
			{
				return new CommandResult( CommandStatus.NoChange, "Owner membership already has the requested value." );
			}

			var expected = stored.Document?.Revision ?? 0;
			var intent = new AuditIntent( Guid.NewGuid(), requestId, actor.Kind, actor.SubjectId,
				CommandOrigin.Console, command, DateTimeOffset.UtcNow );
			var transaction = new PolicyTransaction( PolicyCodec.WriteOwners( candidate ), new[] { intent } );
			var result = await _store.WriteAsync( transaction.CreateWriteRequest( OwnersKey, expected ), cancellation );
			LastWriteStatus = result.Status;
			if ( result.Status == StoreWriteStatus.Committed )
			{
				_owners = candidate;
				_ownerRevision = result.CommittedRevision.Value;
				_managedOwners = true;
				OwnerStatus = StoreReadStatus.Found;
			}
			else if ( result.Status is StoreWriteStatus.Unknown or StoreWriteStatus.Conflict or StoreWriteStatus.Corrupt )
			{
				await LoadAsync( CancellationToken.None );
			}

			return MutationResult( result );
		}, cancellation );
	}
}
