# T19: Security, recovery and measured validation

Status: **Complete with the existing dedicated-client deployment limitation.** No critical authorization or data-loss defect was observed in the tested paths. This is a development validation report, not certification of untested transports or production capacity. T20 prepares an unpublished candidate.

## Environment and workloads

Tested 2026-10-18 on DESKTOP-7LI35HI: AMD Ryzen 7 7800X3D, 8 cores/16 threads, 33157967872 bytes RAM, Windows 11 IoT Enterprise LTSC 10.0.26100, MSI M480 PRO 1TB NVMe E: drive, .NET SDK 10.0.401. Engine 26.10.13a / Steam build 25964806. Other editors remained open; this was not an isolated benchmark machine.

Actual dedicated PID7180 ran `dev/measure-runtime-workloads.cs` and `dev/verify-runtime-pressure.cs` against the library mounted by the compatibility lab. Editor25616 and clients24552/16952 exercised real engine networking. The library runtime source is unchanged from the T17 implementation; T19 adds probes and two regression tests.

| Individual operation | Workload after warmup | Mean | p50 | p95 | Maximum |
|---|---|---|---|---|---|
| Tokenize | 20000 calls, four inputs including a quoted 512-character argument | 3.483 µs | 0.6 µs | 6.9 µs | 28237.3 µs |
| Cached host policy | 20000 calls, eight subjects, alternating allowed help and denied ban | 0.387 µs | 0.3 µs | 0.7 µs | 23.4 µs |
| Recoverable write | 100 real writes after five warmups; final transaction 1395 UTF-8 bytes | 12.596 ms | 11.730 ms | 18.028 ms | 38.877 ms |

Parsing and policy used 1000 warmup iterations. Measurements exclude scheduling between batches; they retain individual outliers, including the 28 ms parsing pause. Policy actors were constructed by trusted development reflection solely for offline evaluation; that benchmark is not an authentication test. Persistence used actual FileSystem.Data generations, markers and readback, not a memory store. No flush-to-device guarantee is inferred.

Local investigation thresholds for rerunning these exact workloads are p95 parsing 50 µs, cached policy 10 µs and persistence 50 ms. These are deliberately loose regression investigation budgets, not runtime deadlines or latency promises. Five known malformed cases and 2000 deterministic fuzz cases (seed19, 24 accepted) produced no partial rejected tokens or accepted tokens exceeding declared bounds.

## Required security and recovery matrix

| Specification scenario | Evidence and result |
|---|---|
| Client claims another identity or role | T03/T09 reject caller-created authority; T11 real forwarded requests use the actual connection, refuse null/SteamId0 player authority and distinguish dedicated-local authority. T19 client-side invocation of the HostOnly reply wrapper produced no forged response. This is wrapper execution, not a raw-packet penetration test. |
| Direct deny versus inherited allow | `verify-permissions.cs` reran in editor25616: direct precedence, explain, unknown denial and cache invalidation passed. |
| Conflicting inherited groups | Same engine probe and existing Core tests verify deny precedence and cycle refusal independently of parent order. |
| Equal rank punishment | `verify-authority-editor.cs` reran: offline rank50/rank50 denied; ordinary admin could not edit policy even with an explicit management permission. T16 also verified connected-client rank and queued revocation. |
| Duplicate request | T19 real client A retried the independent request and retained execution2; host audit contained one row per actual mutation. Dedicated pressure probe coalesced a pending retry and a completed retry without another effect. |
| Store failure during ban | T19 injected a real generation-write failure after a warning baseline. Result StorageUnavailable, moderation revision1 unchanged, actual A remained connected. T16 additionally preserved an existing ban during failure. |
| Denied chat command | Two actual T19 clients: A received private denial, B received no command text or private result. Both rendered chatboxes were inspected. |
| Restart during expiry | T16 actual dedicated15484→26732 and T17 dedicated6552→19892 retained exact stored payloads and original UTC expiry. These recorded process-restart tests were not rerun as part of T19. |
| Direct native console | T15 actual editor/dedicated probes refused Protected/Admin/Cheat bindings and preserved native checks. Native Run remains Unknown without engine acknowledgment. T18 exercised native commands in both independent dedicated samples. |
| Storage interruption and recovery | T04 actually terminated processes during candidate and marker writes, then recovered/restarted; T10 terminated a pending audited operation. T19 reran real editor audit failures before intent and after effect: no false Success, unresolved durable intent stayed Unknown, healthy storage resumed. |

Historical evidence is linked from [T04](T04-STORAGE.md), [T10](T10-AUDIT.md), [T11](T11-CONSOLE.md), [T15](T15-NATIVE-BINDINGS.md), [T16](T16-MODERATION.md) and [T17](T17-LIFECYCLE.md). T19 does not relabel historical or plain-.NET checks as newly executed network tests.

## Pressure, bounds and privacy

The actual dedicated workload issued 512 requests across 16 host-issued service actors while holding the first handler. Exactly 64 were admitted, 448 returned Conflict, and a pending duplicate consumed no extra slot. Releasing the handler produced 64 effects, maximum handler concurrency1, and no effect from the completed retry. Total workload duration was 20.379 seconds including the held operation and rejection auditing. This measures engine dispatch plus real IO, not network throughput. Auditing retained 512 rows in four physical journal files.

Normal dispatch bounds are 64 pending globally / 8 per actor and 1024 remembered / 128 per actor; owner control uses 8/2 pending and 128/32 remembered. Audit admission bounds are 64 ordinary pending operations with separately reserved completion capacity. Result envelopes cap messages at 1024 characters, 16 fields, 64-character keys, 1024-character values and 8192 aggregate characters. Input caps are 4096 characters, 64 tokens and 512 characters per token. Audit state is limited to 384000 encoded bytes and configurable row/age retention. The actual retention probe left four journal files and no secret sentinel in any retained file. Test chat rendering retains 12 lines; shipped diagnostics report bounded metadata, not raw arbitrary commands or secret arguments.

These limits do not bound the engine's own log storage, arbitrary adopting-game logging, trusted direct store callers or the total size of retained non-audit backups. Policy/moderation generations require operator backup management. Rejection auditing still has IO cost; no blanket denial-of-service resistance or sustained network spam throughput is claimed. Custom transports retain their own authentication, filtering and rate limits.

New plain-.NET tests use explicit storage-port doubles: 10000 allowed/denied policy pairs made no reads after initialization; explicit reload failure then denied cached help. Another test held one write, admitted 64 of 1000 audit requests and immediately refused 936 without unbounded pending work. These complement, rather than substitute for, actual dedicated IO evidence.

Actual client A24552 received five private results (two executions, a retry, denial and unavailable-session feedback). B16952 received zero. During duplicate-bootstrap suspension, the platform path privately returned Unsupported and the independent backend refused identity before routing; neither leaked command sentinels. Once the duplicate was removed, ordinary independent chat resumed. B initially displayed only platform messages because its example transport selector was still false; after selecting the independent transport, the privacy check was repeated and ordinary delivery verified. This was fixture setup, not a changed assertion.

## Reproduction and retained evidence

Back up a disposable adopting game's data. Run the dedicated measurement and pressure scripts with one eligible bootstrap. Use T14 host/client setup for two real clients; select `IndependentBackend=true` on B before checking the independent phase. Run `verify-custom-chat-client.cs`, `verify-custom-chat-host.cs`, `verify-chat-reply-forgery.cs`, `verify-validation-chat-sender.cs`, then `setup-unavailable-chat.cs` and `verify-unavailable-chat.cs`. Destroy the temporary duplicate and verify ordinary delivery again with `verify-custom-chat-peer.cs`.

Run permission, authority and audit probes only against fresh disposable security state; they deliberately mutate fixtures. The failed-ban probe requires explicit temporary host ownership, connected A90071996842377217 and an existing moderation document. Restore owner configuration, editor switches, overlays and runtime objects afterward. Never use these fault-injection probes against production data.

Portable evidence is this report and the committed scripts/tests. Raw measurements, pressure JSON and pre-retention chat audit are in the lab's ignored `dev/out/t19-validation-evidence`; screenshots are library-local `dev/out/t19-private-chat-a_small.png` and `t19-peer-chat-b_small.png`. Pre-test backup: `E:/06-um-data/backups/superuser-t19-validation-data/20261018-023214.zip` (43 baseline files).

All 232 contract and 75 Core cases passed without changing existing tests. Generated library build and actual editor whitelist compilation reported zero warnings/errors. Dedicated-client deployment remains unverified because the unpublished adopting game has no required package record; real client privacy/admission evidence uses explicit editor-host mode. Player-hosted releases, host migration, power-loss durability and arbitrary live source hot reload remain unsupported.
