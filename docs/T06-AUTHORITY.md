# T06: Target protection and policy administration

Status: **Complete.** T05 is complete; continue with T07 through T10 and stop before T11.

Implemented stored offline protection, strict rank comparison and self-target rejection, host-authenticated owner-only policy changes, reserved owner handling and explicit local recovery. State and audit intent persist before a policy revision becomes active. Existing bootstrap owner configuration remains the default; managed owner overrides require an explicit dedicated-local operation and never fall back after corrupt managed data.

## Authority and protection

The bootstrap now owns a host policy service. It loads validated T05 policy from the T04 store key `security/policy`. Only a missing first-run document uses the default graph; corrupt, incompatible or unreadable existing policy disables authorization and mutations. Offline users use the same stored Steam-ID assignments as connected users. Every mutation is serialized, reauthenticates its live caller when dequeued, and checks current owner membership. A granted `admin.security.manage` entry does not let an ordinary Admin edit policy.

Effective protection is the highest inherited group level. Owners implicitly include admin ancestry and have a minimum level of 100. Punitive operations require actor level strictly greater than target level, and refuse self-targeting. DedicatedConsole may target anyone. Target protection does not grant command permission or prove an online target is still connected; those checks remain independent. Unknown offline users inherit player and its protection.

Owner bypass grants registered framework permissions and appears explicitly in explain output. Unknown permission keys still deny, and native engine restrictions remain authoritative. Reading another subject's explanation requires `admin.permissions.explain`; a current owner or dedicated console has that registered framework authority. The ordinary named group `owner` cannot be created, assigned, inherited, or used for escalation.

## Host integration

These APIs run on the host main thread and authenticate an actual live engine Connection. A null/stale caller is not local console authority:

```csharp
var result = await Superuser.ApplyPolicyChangeAsync( caller,
    PolicyChange.AddUserGroup( "76561198000000003", "moderator" ), CommandOrigin.Console );
var explanation = await Superuser.ExplainPermissionAsync( caller, caller.SteamId.ToString(), "admin.players.kick" );
var protection = await Superuser.CheckPunitiveTargetAsync( caller, "76561198000000003" );
var health = Superuser.GetAuthorityDiagnostics();
```

Only an authenticated current owner can apply the player-facing edit API. The origin is supplied by the trusted console/chat integration, not an untrusted request field. This API is a host integration service; no player RPC or general native root command is installed in T06. T09 will supply the shared dispatcher and T11 its general native-console entry.

Typed `PolicyChange` factories cover group creation/deletion, parent replacement, group permission allow/deny/unset, protection levels, user group add/remove and direct user entries. Deletion refuses player and referenced groups; cycles and missing references reject the entire candidate. Identical edits return NoChange without creating a generation. Only confirmed Committed storage publishes the new graph/cache revision and returns Success. Uncertain persistence returns Unknown and reloads fail-closed state.

`security/policy` state has `policySchema: 1`, `groups` and `users`; T04 supplies the document revision. Groups store ID, parent IDs, exact Allow/Deny entries and protection. Users store Steam ID, groups and direct entries. Stored Unset, duplicate fields/IDs, unknown fields and invalid inheritance are rejected. State and attributed Unknown intent share one aggregate. User edits record affected Steam IDs; owner changes split affected IDs into bounded intent batches. Actual action-outcome lifecycle remains T10.

The store notifies authority after committed, corrupt, conflicting or indeterminate security writes. Cached authorization is invalidated immediately and reloaded behind any active authority operation. This also covers T04's explicit backup recovery; an old allow cannot survive restoration of a newer stored deny. Retired bootstrap handles remain unavailable.

## Owner operations and recovery

Until an explicit local owner change creates `security/owners`, T03's host-local owner configuration remains authoritative. Ordinary policy changes do not create managed owners or modify either bootstrap file. A dedicated-local owner operation persists a managed override using `ownerSchema: 1` and explicit owner IDs. Once present, that validated managed document is authoritative, including an intentionally empty list. Bootstrap file edits no longer replace it. Corrupt managed data grants nobody and never resurrects bootstrap-file or cached owners.

Use only the actual dedicated server's local console:

```text
superuser_authority_status
superuser_owner_add <steamid>
superuser_owner_remove <steamid>
superuser_owner_recover replace <steamid> [steamid...]
superuser_owner_recover clear
superuser_policy_reload
```

All modifying owner/recovery entries are non-forwarded and revalidate dedicated-local authority; they refuse editor mode and clients. Player owners cannot change owner membership through policy-edit requests. Normal removal refuses the final owner even for the dedicated console. The explicitly named `superuser_owner_recover clear` procedure is the sole clear-all exception: it persists an empty managed list, disables all player owner authority, and leaves dedicated-local recovery available. Use `replace` with explicit IDs to recover afterward. Neither command silently edits or deletes bootstrap configuration.

If the managed store itself has a corrupt commit/generation, use the T04 backup procedure first:

```text
superuser_store_backups security owners
superuser_store_recover security owners <compatible-generation-guid> <observed-head-revision>
```

Likewise use key `security policy` for a damaged policy document. Existing corrupt files remain preserved. A valid generic transaction with invalid owner-domain fields can be repaired by an explicit dedicated-local owner replacement; an unreadable/corrupt generic store cannot be treated as a new installation. Store recovery automatically invalidates authority caches. No automatic policy/owner migration is performed.

Native commands start async work; wait for their logged result before assuming completion. Success is logged only after the storage result confirms persistence. `AuthorityDegraded` bootstrap health and `GetAuthorityDiagnostics()` expose domain/schema failure, owner source, active revisions and last write result without broadcasting the database.

## Validation

- 97 tests passed: all previous 79 unchanged plus 18 authority cases. Coverage includes equal/offline ranks, highest inherited protection, self-target denial, dedicated-console targeting, ordinary Admin denial despite an explicit security grant, protected explanation, persistence ordering/failure, queued owner revocation, final-owner refusal, explicit local clear/restore, corrupt managed-owner refusal, invalid policy, IO failure, recovery cache invalidation, graph edit validation and retired runtime refusal.
- Engine 26.10.13a/Steam build 25964806 compiled the library and adopting game with zero whitelist diagnostics. Both generated runtime .NET projects built with zero errors/warnings. Both standard/Core checks passed. Existing empty test projects were run without claiming additional passed cases; `git diff --check` passed.
- `dev/verify-authority-editor.cs` passed against the real editor host PID13136. It used the live local connection and public façade to persist policy, remove bootstrap ownership, prove ordinary-admin and null-caller denial, deny equal offline rank, filter another user's explanation, refuse local owner recovery in editor mode, and invalidate permissions after a direct trusted storage replacement. The original owner file and editor option were restored.
- Actual dedicated PID3248 ran `dev/verify-owner-console.cs`: add/remove, final-owner denial, explicit clear and replacement, ending at managed revision 5. The first script attempt on PID18516 incorrectly assumed synchronous native completion; its assertions were corrected to await the authority queue. The commands themselves logged the correct results, and that disposable first-run data was archived before a fresh successful run.
- `dev/verify-managed-owner-corruption.cs` deliberately damaged revision 5 while a different owner remained in the bootstrap file. Both bootstrap and cached owner authority were refused; bootstrap reported AuthorityDegraded. Actual local backup recovery restored the selected owner set as revision 6, retained damaged evidence and did not grant the bootstrap-only owner. Original bootstrap configuration was restored.
- PID3248 was terminated; fresh actual dedicated PID22908 ran `dev/verify-owner-restart.cs` and loaded managed revision 6 with both expected owner IDs. This is actual server restart evidence, not a storage-port mock.
- Existing `dev/verify-bootstrap.cs` and `dev/verify-durable-store.cs` passed again in the real editor host. No new dedicated-server error-level logs remained. Disposable security stores were archived under the lab's ignored `dev/out/t06-security-editor` and `t06-security-dedicated`; bootstrap owner files were restored to absent, and the lab tracked tree is unchanged. Owned test processes were closed; the user's original editor PID17136 was untouched.

Raw evidence: ignored `dev/out/evidence/t06-*`. No T06 blocker remains. Dedicated remote-client deployment still has T01's unpublished-package prerequisite; no new dedicated-client test is claimed. Module enable/disable security, general command routing and audit outcomes remain their later tasks.
