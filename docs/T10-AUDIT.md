# T10: Audit and durable outcomes

Status: **Complete.** T05–T10 implemented in order. T11 has not started. Everything runs in the existing s&box library, on an actual dedicated host or explicitly enabled editor host.

## Intent and outcome contract

The runtime owns `HostAuditService` over T04 storage at `superuser/store/<editor|dedicated>/audit/journal`. Every dispatched handler, including read-only handlers, requires a confirmed persisted Unknown intent first. The dispatcher rechecks live identity, permission, registration and target protection after awaiting that write. It stores the observed outcome before returning success; a failed outcome write returns Unknown. Handler exceptions and interrupted mutations also remain Unknown. No rollback or cross-restart exactly-once execution is promised.

Direct policy and owner changes use the same audit requirement and bind queued authorization to the audited identity. Their existing security-state transactions retain their own Unknown intent with the same request ID. Security-state and central audit writes are separate recoverable commits: interruption between them leaves attributable Unknown evidence rather than an invented outcome. Owner-only rules and the dedicated-local owner membership boundary remain enforced.

Rows contain sequence, action ID, request ID, authenticated actor kind/subject, source, registered command key, safe arguments, stable targets, UTC creation/completion times and outcome. Denied, InvalidArguments, Failed, Cancelled and Unknown remain distinct. Invalid or unknown command names use `command.unresolved`; arbitrary input and full native command strings are never copied into the journal. Unauthenticated contexts cannot attribute a claimed actor. If audit storage itself is unavailable, rejected attempts cannot be promised a durable row.

## Redaction and access

Arguments default to `[redacted]`. Set `CommandArgumentAudit.SafeValue` only for approved typed scalars; free-text strings cannot opt into it. `Sensitive` and credential-like parameter names remain redacted. Commands with sensitive parameters are refused for Chat origin. Raw tokens, handler messages/results and exception text are excluded. A game chat adapter must still suppress command text before public broadcast; general chat ingress is a later task.

`Superuser.QueryAuditAsync(caller, query)` authenticates a live engine connection and rechecks authorization after reading. `admin.audit.read` permits all rows; `admin.results.own` permits only the caller's Player rows. Querying another actor under own-results permission is Denied. Filters support exact request ID, command, actor subject, outcome, origin, target Steam ID and inclusive UTC creation range. Results are newest first, 1–100 rows per page, default 25. A cursor binds revision and exclusive sequence; any intervening write requires restarting pagination. Queries do not audit themselves or expose files.

`ConfigureAuditRetentionAsync` requires a current owner. `GetAuditDiagnostics` is trusted host-local code access to health, revision, retention options and count; it returns no row contents. Bootstrap reports AuditDegraded when audit persistence or cleanup needs attention. No RPC, general native root or UI is added by T10.

## Retention and recovery

Defaults are 512 rows and 30 days; configurable ranges are 1–4096 rows and 1–3650 days. A 384000-byte encoded state ceiling can retain fewer rows. Active intents are protected; a full protected set refuses new writes. Expired rows are filtered at query time and compacted at startup, on writes and by idle maintenance at most once per minute. Audit requests allow 64 pending operations, with room for up to 128 completion operations. Completed/retired operations release retention pins.

Retention writes two compacted snapshots before deleting old generations, so the retained backup cannot resurrect expired rows. A persisted checkpoint flag resumes this sequence after interruption. Cleanup deletes at most 64 old generations per pass, marker first, then data; an interrupted deletion leaves an ignored orphan that a later pass can finish. Only the fixed audit journal is eligible. Policy and owner backups never enter automatic pruning. Damaged committed historical audit files are preserved, flagged as degraded and left for operator review. The newest two validated committed generations remain available.

Corrupt current data fails closed without resetting the journal. Use T04's dedicated-local backup inspection/recovery procedure for `audit journal`, preserving the original files. Restoring a backup may lose newer outcomes; unresolved evidence stays Unknown. Host-local export is an operator copy of the stopped host's audit directory together with its generation markers, kept private. Never expose those files through a client command or repository.

Durability means the in-process stream was closed/flushed and read back with its integrity-checked commit marker. Actual process termination/restart is verified. Flush-to-device, power-loss survival, external tamper resistance and concurrent processes writing the same data root are not guaranteed. Retention intentionally limits historical evidence; do not use an old request ID as proof an action has never run.

## Verification

212 tests pass: 180 contracts and 32 Core, with all 186 pre-T10 cases unchanged. New checks cover immutable Unknown/outcome semantics, checkpoint schema, protected/count/age/encoded-byte retention, permission-filtered queries and reauthentication, revision-bound cursors, intent-before-handler ordering, retry correlation, pre/post-action write failures, sensitive chat refusal/redaction, queued cancellation, direct policy auditing, damaged evidence preservation, 64-generation cleanup bounds, stale revisions and interrupted marker cleanup.

Engine 26.10.13a, Steam build 25964806: editor PID 21804 ran `dev/verify-audit-editor.cs` successfully. Four real compiled capability mutations, duplicate suppression, actual storage failures before intent and after action, redaction, sensitive-chat refusal, pagination and physical cleanup to four files (two generation/marker pairs) passed. `dev/verify-audit-own-results.cs` passed with no configured owners: two player/service actions produced only one caller-visible row, another-actor query and null caller were Denied, and non-owner retention configuration was refused.

Dedicated PID 22940 ran `dev/interrupt-audit-outcome.cs`. Its intent committed, the compiled game handler mutated once, and the outcome generation was flushed halfway with its stream open. Only that owned process was terminated. Fresh dedicated PID 15308 ran `dev/verify-audit-restart.cs`: request `691371ea-c272-467b-8e41-cc645a5d0033`, action `4e3358fc-728f-425e-867f-ac8ada4b1ca5`, actor `dedicated-console`, command `probe.increment` remained Unknown with null completion time. The existing dedicated dispatcher verification then passed three mutations, and a second audit query still retained the interrupted Unknown row.

Pre-test backup: `E:/06-um-data/backups/superuser-t10-lab-data/20261017-235228.zip` (43 files). Test evidence lives in ignored `dev/out/t10-*`; the scripts are committed. Temporary owner configuration was restored. This verifies host-local storage and dispatch, not dedicated-client deployment: T01's unpublished test-game limitation remains. The library still uses `/` for chat integrations and reserves `/admin` for the later transport task.

Final checks: both generated runtime builds and both package checks passed; engine whitelist has zero warnings/errors; all 212 cases passed. The final editor and dedicated checks were repeated after the last source changes. An engine restriction on compiler-generated ExceptionDispatchInfo from await-in-finally was fixed by awaiting cleanup outside finally. Owned test processes were stopped by exact PID, the original user editor was left running, and new audit evidence was archived under the lab's ignored dev/out/t10-audit-evidence-20261017. Both owner configuration files remain absent and the lab tracked tree is unchanged.
