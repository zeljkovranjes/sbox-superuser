# Verification

## Build

After opening the library in the editor to generate its project:

```powershell
dotnet build Code/chomnr_sbox_superuser.csproj
dotnet test tests/SboxSuperuser.Contracts.Tests/SboxSuperuser.Contracts.Tests.csproj
dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj
```

Plain .NET builds do not apply the engine whitelist. Check the adopting game in
the actual editor too. The workspace's `sbox-check` checks layout and Core
compilation; its fixed README template differs from this repository's requested
example-first format.

## Verified behavior

On engine **26.10.13a**, Steam build **25964806**, all **259 contract and 75
Core tests** passed. Existing tests were preserved. The generated library build
and actual engine assembly references compile with zero warnings/errors.

| Check | Evidence |
|---|---|
| Optional menu | Actual game-camera capture verified ImGui rendering in the editor. Live checks cover cursor acquisition, disable/replacement, console and slash chat continuity. |
| Admin transport | Two actual clients deny non-staff reads/actions. Authorized client received two history records; a peer deliberately prepared with the same request ID received no private reply. |
| Custom storage | Actual editor ran the tutorial decorator through two eligible runtimes, offline warning/ban persistence and history/audit reads. Guard tests cover exceptions, wrong revisions, cancellation and invalid payloads. No external database endpoint was tested. |
| Documentation | Eleven Markdown C# blocks are extracted or matched to source; the adapter, setup and replacement-view fixtures compile in the actual engine. |
| Caller identity | Actual forwarded requests use the engine connection; null and SteamId0 player routes deny. Dedicated-local commands have distinct authority. |
| Permission conflicts | Actual engine resolver and Core tests cover direct precedence, inherited deny, cycles and cache invalidation. |
| Protection | Group ranks stop below reserved owner rank; owners, equal ranks and self-targets cannot punish each other. Queued permission revocation prevents execution. |
| Duplicate requests | Real client retry executes once; dedicated pressure test coalesces pending and completed retries. |
| Failed ban write | Real injected generation failure returns StorageUnavailable, preserves the previous revision and leaves the connected target online. |
| Private chat | Two actual editor clients: requester received five private replies, peer received none. Denied/unavailable command text was not broadcast; ordinary chat resumed. Result contracts reject control-character line injection. |
| Restart | Actual dedicated process restarts preserve policy, moderation and original UTC expiry. |
| Recovery | Processes terminated during candidate/marker writes recover through validated backups. Interrupted audit outcomes stay Unknown. |
| Native restrictions | Protected/Admin/Cheat bindings refused; native command acknowledgment remains Unknown. |
| Lifetimes | Dedicated scene replacement retires old handles; five managed reloads retain one listener and one execution per request. |
| Portability | Arena uses pawn components; Colony uses roster records. Both ran console, policy, moderation and optional capabilities with unchanged library code. |

Client privacy/admission tests use editor-host opt-in. Actual dedicated
remote-client deployment remains unverified because the unpublished adopting
game lacks its required package record. Source hot reload and host migration are
not supported.

## Measured workload

Machine: DESKTOP-7LI35HI, Ryzen 7 7800X3D (8 cores/16 threads), 30.88 GiB RAM,
Windows 11 IoT Enterprise LTSC 26100, MSI M480 PRO NVMe, .NET 10.0.401. Other
editors were open.

| Operation | Samples | Mean | p95 | Maximum |
|---|---:|---:|---:|---:|
| Parsing | 20000 | 3.483 µs | 6.9 µs | 28.237 ms |
| Cached permission | 20000 | 0.387 µs | 0.7 µs | 23.4 µs |
| Real recoverable write | 100 | 12.596 ms | 18.028 ms | 38.877 ms |

Measurements follow warmup and exclude scheduling between batches. Policy uses
eight offline benchmark actors; writes use actual engine IO with a 1395-byte
transaction. These are measurements, not production guarantees. Local p95
investigation budgets for this workload are 50 µs parsing, 10 µs policy and 50
ms persistence.

The dedicated 512-request pressure run admitted 64, refused 448 at capacity,
applied 64 effects with maximum handler concurrency1, and retained 512 audit
rows in four journal files. Total duration was 20.379 seconds including the
deliberately held first handler. This is not network throughput.

Two new storage-double tests separately prove no reads during cached permission
checks and bounded audit admission: 64 of 1000 admitted, 936 refused
immediately. Five malformed cases and 2000 seeded fuzz cases respect token
bounds.

## Bounds and reproduction

Normal dispatch: 64 pending globally / 8 per actor; 1024 remembered / 128 per
actor. Owner-control queue: 8/2 pending. Text: 4096 characters, 64 tokens, 512
characters each. Replies: 1024-character message, 16 fields, 8192 aggregate
characters. Audit state: 384000 encoded bytes plus row/age limits. Non-audit
backups and the engine's own logs need separate operator management.

The committed scripts under `dev/` reproduce these checks. Start with
`measure-runtime-workloads.cs`, `verify-runtime-pressure.cs`,
`verify-permissions.cs`, `verify-authority-editor.cs`, `verify-audit-editor.cs`
and `verify-moderation-storage-failure.cs`. Read each fixture prerequisite
first. Fault injection belongs only in disposable games with backups.

Raw local evidence is archived under the compatibility lab's `dev/out/`;
original data was restored. The portable results are recorded here. New
documentation example checks are recorded in [release notes](release.md).
