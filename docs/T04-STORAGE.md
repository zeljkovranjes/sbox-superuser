# T04: Versioned durable storage

Status: **Complete.** This is the historical T04 completion record; see the [implementation ledger](IMPLEMENTATION.md) for subsequent work. Source: [specification 0.2](sbox-admin-specification.docx), T04 and its storage/recovery requirements.

## Runtime and API

One library provides the implementation through `Superuser.GetStore()`, returning `IRecoverableAdminStore` or null outside an eligible T03 bootstrap. Call it and its methods from the engine main thread. Production requires an actual dedicated host. Editor-host testing requires `AllowEditorTesting`; clients and release listen servers cannot enable it with that switch. There are no runtime executables, services, databases, raw filesystem calls or external dependencies.

All file access uses the adopting game's `FileSystem.Data`. Dedicated documents live under `superuser/store/dedicated/<module>/<document>/`; editor documents use `superuser/store/editor/`. `StoreKey` accepts logical keys, not paths. Data is never replicated or sent in replies. Only trusted host code receives the store; exposing these APIs to arbitrary player RPC input would bypass the future dispatcher and is unsupported.

Each runtime owns one serialization gate for all store operations. Writes require an expected revision. A stale revision returns Conflict. Retained handles refuse access after bootstrap disposal, loss of authority, or scene replacement. Use exactly one host process per physical data root; the gate is not a cross-process lock. Close the server before copying or changing its data externally.

### Host code

This is an example of trusted host code inside an async method; it does not register an administrative command:

```csharp
using SboxSuperuser;
using System.Threading;

var store = Superuser.GetStore();
if ( store is null ) return;
var key = new StoreKey( "example", "policy" );
var current = await store.ReadAsync( key, 1, CancellationToken.None );
if ( current.Status != StoreReadStatus.Found && current.Status != StoreReadStatus.Missing ) return;

var intent = new AuditIntent( Guid.NewGuid(), Guid.NewGuid(), ActorKind.InternalService,
    "example.service", CommandOrigin.InternalService, "example.update", DateTimeOffset.UtcNow );
var transaction = new PolicyTransaction( "{\"enabled\":true}", new[] { intent } );
var write = await store.WriteAsync(
    transaction.CreateWriteRequest( key, current.Document?.Revision ?? 0 ), CancellationToken.None );
if ( write.Status == StoreWriteStatus.Committed )
{
    // The new revision has passed persistence and integrity readback.
}
```

`AuditIntent` snapshots recorded actor kind/subject, request/action IDs, origin, command, caller-safe argument fields, target Steam IDs and a UTC timestamp. Its outcome is always Unknown. These are audit data, not identity credentials or proof of authorization. Redact fields before constructing an intent; no automatic secret redaction is claimed. `PolicyTransaction` snapshots one replacement state object and its intents into one aggregate. Domain-specific policy and moderation validation remain later modules; T04 validates the structural transaction schema. Multi-document atomic transactions are not offered: coupled policy and intent data must share one aggregate.

The caller must not apply a persistent policy change or report success until Committed. For future game actions: persist intent, perform the action, then record its outcome through T10. An interrupted intent stays Unknown. T04 executes no game action and fabricates no action success. New transactions should carry their own intents; retained generations preserve previous transaction history.

## Format and commit protocol

Format version 1 uses two immutable files per committed generation:

- `g-<20-digit-revision>-<32-digit-generation-guid>.json`: exact fields `formatVersion`, `generation`, `schemaVersion`, `revision`, `payload`.
- `c-<same-stem>.json`: exact fields `formatVersion`, `generation`, `revision`, `digest`. The digest is SHA-256 of the exact UTF-8 generation text.

Schema 1 payload has exactly `state` (object) and `auditIntents` (array). Unknown envelope/intent fields, duplicate JSON fields, incompatible versions, inconsistent revisions/IDs, invalid identities/timestamps and integrity failures are rejected. State field semantics belong to the adopting module. The digest detects accidental corruption; it is not protection against an operator who can rewrite local files and digests.

The serialized write sequence is:

1. Validate schema and payload, then inspect existing committed state. Missing is allowed only for an absent document directory. An empty or damaged existing directory is Corrupt, never a fresh installation.
2. Check expected revision. Create a new GUID generation using `OpenWrite(FileMode.CreateNew)`. Write, flush, dispose, then read back the exact text.
3. Create its commit marker the same way. Read back both files and validate the entire committed pair before returning Committed and its revision.
4. Keep every previous committed pair, incomplete candidate and corrupt file. No overwrite, deletion or pruning occurs.

An orphan generation with no commit marker cannot have been acknowledged. With an older valid commit, reads keep returning that commit. A malformed highest commit marker or damaged/missing referenced generation returns Corrupt with no document, even when an older backup is valid. Ambiguous highest revisions also fail closed. Unsupported committed versions return UnsupportedSchema. Ordinary writes refuse all these states; no automatic fallback can erase a previously acknowledged ban or owner record.

A failure before attempting the marker returns Unavailable or Cancelled. Once marker creation is attempted, an indeterminate failure returns Unknown, without a committed revision. Inspect storage before retrying: the operation may have persisted even though acknowledgement failed. Read/IO failures report degraded status and safe diagnostics, not policy JSON. Unsupported upgrade requests leave the current version and backups untouched; no migration is run automatically.

## Operator backup and recovery

Use these **native commands in the actual dedicated server's local console**, not the chatbox:

```text
superuser_status
superuser_store_backups <module> <document>
superuser_store_recover <module> <document> <generation-guid> <observed-head-revision>
```

The entries are non-forwarded and recheck dedicated-local authority. They do not infer console authority from a null player caller. Recovery console commands refuse editor mode and clients. Trusted editor-host code can exercise the recovery API with explicit editor opt-in. Chat commands still use `/`; this task adds no general console/chat dispatcher.

1. Inspect the reported status and compatible backups. Inspection returns at most 100 validated backups, newest first, plus `HasMore`. Head revision includes incomplete candidates so recovery will not reuse their revision.
2. Before restoring, stop the server and copy the entire affected document directory for a separate offline backup. Preserve all generation/marker pairs and damaged evidence. Also back up `superuser/owners.json` separately; it is T03's explicit configuration and is never rewritten by this store. Restart using the same data root.
3. Select the intended compatible generation and use the displayed head revision. Restoring an older version deliberately rolls back later policy state: review that choice as the operator. A changed head returns Conflict; inspect again rather than retrying blindly.
4. Recovery validates that backup again, appends a dedicated-console `storage.recover` intent, and commits a new generation above every observed revision. Original valid and corrupt files remain untouched. Confirm Committed and then inspect/read the new revision. The appended intent remains Unknown as action outcomes belong to T10.
5. If there is no compatible backup, restore a previously saved complete document directory while the server is stopped, preserving the damaged directory separately. Do not delete a corrupt directory to make it appear newly installed. An unknown filename requires offline operator inspection/quarantine of that file before automatic recovery can interpret the directory safely. Never invent empty owner or ban state as a repair.

Bootstrap owner configuration remains independent. Invalid owner configuration still grants nobody and remains on disk. Correct it through T03's explicit local configuration procedure; T04 does not migrate it, edit owners, or implement T06 final-owner administration.

## Limits

- Maximum generation/marker read: 1 MiB; maximum transaction payload: 512 KiB UTF-8, JSON depth 32. JSON escaping can enlarge the generation, so both limits are checked before creating files.
- Each aggregate has 1–256 intents; an intent has at most 32 safe argument fields of 256 characters and 64 target IDs. Recovery appends one intent and is subject to the same limits; an over-limit recovery refuses with InvalidData and preserves all records. Keep headroom in aggregates intended for recovery.
- Backup inspection returns up to 100 compatible entries; enumeration still scans retained files. No automatic retention or pruning is installed. Audit pagination, filtering, redaction and outcomes remain T10; broader load validation remains T19.
- The tested guarantee is successful engine file operations/readback plus recovery across process termination and restart. No atomic replacement API, fsync, power-loss, hardware-failure or cross-process-write guarantee is asserted.
- Entire-directory deletion outside the store cannot be distinguished from first installation. Do not manually delete production storage. Host files and offline backups remain the operator's responsibility.

## Verification evidence

Tested 2026-10-17 local time (UTC events extend into October 18), Steam app 590830 build 25964806, engine 26.10.13a. Library and isolated adopting game `notpointless.chomnr_superuser_compatibility_lab` used local library mounting. The existing test-only data junction keeps physical files on E:. No sbox.game publication or external storage process was used.

| Requirement | Observed evidence |
|---|---|
| Real engine adapter | `dev/verify-durable-store.cs` passed on editor host PID19420 and actual dedicated PID5192, repeated on final code with dedicated PID4508. Checks creation, readback, revision conflict, two backups, deliberate corruption, blocked reset, explicit recovery, preserved corrupt file, retired-handle refusal and unchanged bootstrap owner configuration. Editor switch off returned no store; local recovery console refused editor execution. |
| Restart preserves records | Seed committed revision 1, generation `62c1be83d3314934b1a6d8af2e777502`. Terminated dedicated PID5192; new PID13108 read the exact token `T04-survive-process-restart`, test owner ID and test ban ID with the original Unknown intent. |
| Candidate interruption | PID13108 paused the actual adapter after half the candidate was written/flushed with stream open, then was terminated. PID13408 read revision 1 unchanged; 376-byte incomplete revision-2 candidate remained. |
| Commit interruption | PID13408 paused after half the commit marker was written/flushed, then was terminated. PID21264 reported Corrupt, head 2, no active document and one compatible baseline backup. The 78-byte torn marker and 752-byte candidate remained. |
| Explicit recovery | A create-at-zero request against that corruption was refused. Actual `ConsoleSystem.Run` invoked the non-forwarded dedicated-local inspection and recovery commands. Recovery reported Committed revision 3 with baseline state and a `storage.recover` Unknown intent. All damaged files remained. |
| Recovery survives another restart | Terminated PID21264; PID4508 read revision 3, generation `ba0add3edc7f453a8bf3edb54aff5eb0`, with both test owner/ban records and both intents. |
| Core fault coverage | 55 tests passed, including 22 new storage cases: partial candidate/marker with IO failure and cancellation, commit-readback failure returning Unknown, queued writers/cancellation, stale runtime, damaged/missing payloads, digest failure, all backups corrupt, duplicate highest revisions, unsupported upgrade/recovery, bounded backup listing, size limits and immutable audit snapshots. Existing 33 tests were not edited. |
| Whitelist | Engine compiled the library and adopting game with zero errors/warnings. SHA-256, SemaphoreSlim, CreateNew streams and file enumeration executed successfully. `OverflowException` type references were rejected during development and removed; the implementation uses allowed error classifications. |

The scripts operate on disposable test keys. `dev/seed-durable-store.cs` refuses to overwrite an existing fixture. `dev/interrupt-durable-store.cs` uses a private instance checkpoint set only by the trusted development bridge; no shipped command, ConVar or player input activates fault injection. Set `superuser/t04-interrupt-phase.txt` to `candidate` or `commit`, start that script, wait for its INTERRUPTION CHECKPOINT log, terminate only the test PID, then run `dev/verify-durable-restart.cs` on a fresh server. `dev/recover-durable-test.cs` deliberately invokes local recovery for that known corrupt fixture. Repeat in a separate disposable game/data root; do not reset production data for testing.

Raw local evidence: ignored `dev/out/evidence/t04-*`. This committed report is the portable record. T01's unrelated dedicated-client package prerequisite remains unresolved; it does not block host-local T04 restart/recovery tests. There is no T04 implementation blocker. At T04 completion, tasks T05 onward had not begun.

Final repository checks: library and lab `sbox-check` passed; both generated runtime projects built with zero warnings/errors; engine whitelist compilation had zero diagnostics. All 55 contract/Core tests passed with no skips. Original empty test projects were run without claiming additional passed cases. Existing T02/T03 tests and the original Core harness are unchanged. Final editor/server logs contained no new error-level entries. Publication lint reported zero failures and one warning in ignored generated engine XML; no engine binaries or generated outputs are tracked. `git diff --check` passed. Owned test processes were stopped, the lab tracked tree remained unchanged, and the user's pre-existing editor PID17136 was left running. GitHub remains private.
