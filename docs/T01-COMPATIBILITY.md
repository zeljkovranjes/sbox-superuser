# T01: Engine compatibility

Status: **In progress — dedicated validation resumed**. The previous blocked assessment was premature: missing test sessions had not yet been demonstrated to be an environment limitation. The probe and report are implemented. Scope: T01 only; T02–T20 remain **Not started**.

The source specification is [version 0.2](sbox-admin-specification.docx). The user narrowed production support to actual dedicated servers and requested an explicit editor testing switch. Player-hosted releases and host migration are unsupported. Editor results never establish dedicated-server compatibility.

All probe code runs inside the single `notpointless.chomnr_sbox_superuser` library. The editor, CLI, .NET SDK and Git are development tools, not runtime dependencies. No companion process, service, installer or database is part of the library.

## Selected environment

2026-10-17, Windows, Steam app 590830, installed build ID **25964806**, engine **26.10.13a**. Engine API evidence comes from the installed `Sandbox.Engine.xml`, `Sandbox.Reflection.xml`, `Sandbox.Filesystem.xml` and `Sandbox.System.xml`. A separate editor opened this library itself; the user's other game/editor was not modified. This proves local library compilation and editor execution, not installation into an adopting dedicated-server game.

## Task boundary

This task adds disposable compatibility checks, not the administration framework. No actor contracts, permission resolver, dispatcher, module manager, persistent administration store or moderation commands are implemented. Native claim checks are observations only; they never grant framework authority. A missing caller is not a trusted-console credential.

The user also selected [sbox-imgui](https://github.com/zeljkovranjes/sbox-imgui) for future UI work. Source staging is reserved in `dev/ui-source/sbox-imgui/`. Nothing is imported or compiled from that directory. This selection amends the original UI direction without starting UI implementation.

## Supported-features record and adapter decisions

“Observed” below names the environment actually exercised. An API appearing in documentation or compiling is not a runtime pass.

| Integration point | Evidence on this build | Decision / fallback | Outstanding gate |
|---|---|---|---|
| Library loading and bootstrap | The editor compiled and instantiated `CompatibilityProbe` in this library's scene. | Use one scene component as the probe entry; future bootstrap is T03. Duplicate active probes refuse execution. | **Blocked:** adopting game on an actual dedicated server. |
| Runtime restriction | Editor default refused; explicit switch ran as `EditorTest`; disabled component refused. | Require `Networking.IsHost` and either `Application.IsDedicatedServer` or `Game.IsEditor && AllowEditorTesting`. The switch cannot enable a release listen server. | **Blocked:** real dedicated and release-client negative tests. |
| Attribute discovery | `TypeLibrary.GetMethodsWithAttribute<ProbeMethodAttribute>()` found exactly one method; `InvokeWithReturn<string>` returned the sentinel in editor play. | Use TypeLibrary; explicit registration remains the fallback if a handler is unavailable to discovery. Do not use unrestricted reflection in shipped code. | **Blocked:** same test in dedicated runtime. |
| Whitelist | Corrected source passed editor compilation/hotload with zero diagnostics and executed discovery and console calls. | Engine adapters use supported APIs. No `System.IO.File`, reflection, subprocess, native interop or service dependency in the probe. Trusted development scripts are not shipped runtime code. | Unexecuted branches still require dedicated runtime evidence. |
| Forwarded console caller | `[ConCmd(..., ConVarFlags.Server)]` with leading `Connection` supplied the local editor player's connection and Steam ID. | Use the engine-injected connection for player identity. A null connection remains unclassified. [Native command contract](https://sbox.game/dev/doc/code/code-basics/console-variables). | **Blocked:** remote client and dedicated-console observations. |
| Local console route | Non-forwarded `superuser_probe_local` executed as `EditorLocalTestEntry` only with the switch on. | Candidate fallback: a separate non-forwarded native entry guarded by the dedicated runtime check. Do not promote a missing caller on the shared forwarded entry. | **Blocked:** prove local-only behavior with an actual server and remote client before granting authority. |
| Argument forwarding | Raw command `superuser_probe "quoted reason" tail` and `ConsoleSystem.Run(name, args)` both produced exactly `["quoted reason","tail"]`. Zero arguments produced `[]`; nine test arguments were refused. | Forward the engine-tokenized array. It preserves the tested quoted value, not the original raw quote syntax. No arbitrary native-console execution endpoint. | **Blocked:** remote forwarding; escaped quotes, empty quoted strings and malformed syntax. |
| Native claims | The editor host's `HasPermission("sboxsuperuser.probe")` returned true. | Read explicit native claims on the host; import no roles automatically and never grant broad engine-admin status. [Native claims](https://sbox.game/dev/doc/networking/dedicated-servers/user-permissions). | **Blocked:** claimed and unclaimed remote users on a dedicated server. |
| Console metadata and restrictions | Public `ConsoleSystem` exposes GetValue, SetValue and Run; unrestricted registration/enumeration is not established. | Explicit allowlisted descriptors are the baseline; preserve Admin, Server, Cheat and Protected restrictions. Fixed probe names do not prove configurable native root registration or collision detection. [Console API](https://sbox.game/api/Sandbox.ConsoleSystem). | **Blocked:** native restriction checks; configurable root/metadata route. Use a fixed native transport with a configured logical root if supported registration cannot be proved. |
| Chat ingress | `IChatEvent`, authenticated Sender and Suppress compile. Probe consumes only exact `!superuser-probe`, skipping suppressed and system messages. | Host listener; suppress before sending any reply. No normal chat or general command parser in T01. [Chat contract](https://sbox.game/dev/doc/networking/chat). | **Blocked:** actual network chat, ordinary-message pass-through and one listener across scene changes. |
| Targeted replies | `Rpc.FilterInclude(sender)`, `[Rpc.Broadcast(NetFlags.HostOnly)]` and local `Chat.AddText` compile. | Use authenticated host RPC filtered to one recipient; receiver rejects non-host callers. No reply through Chat.Say. [RPC API](https://sbox.game/api/Sandbox.Rpc). | **Blocked:** two real clients must demonstrate private delivery and rejection of forged replies. |
| Files and recoverability | Public read/write calls compile. `WriteAllTextAtomic` appears in installed XML but compilation returned CS1061; it is unavailable to this library. | Candidate fallback: separate, validated generations/journal retaining a previous readable record. The probe writes an isolated baseline and candidate, then simulates candidate truncation. This is not the T04 store. | **Blocked:** file execution, restart and actual interruption. Never claim disk-flush/power-loss durability from readback. |
| Admission | `Component.INetworkListener.AcceptConnection(Connection, ref string)` compiles, alongside OnConnected and OnActive. Installed XML states a false result denies admission. | Reject only an explicitly configured test Steam ID. Future bans may use this gate only after timing is verified; otherwise the game must gate gameplay at admission. | **Blocked:** real connect/reject/reconnect ordering; do not claim proven pre-join protection. |
| Hotload/lifecycle | Code hotloaded in editor; discovery ran once per tested play start; a duplicate component refused commands. | No static registry or subscriptions to retain. Require restart for unverified registration changes; full module lifecycle remains T12/T17. | **Blocked:** dedicated restart, scene transition and chat listener lifecycle. Host migration is unsupported. |

## Reproduce the editor observations

1. Open this local library in the editor, or reference it from an isolated test game. Start an empty scene and add one **Superuser Compatibility Probe**.
2. With **Allow Editor Testing** off, enter Play and run `superuser_probe`. Expect `mode=Unsupported` at startup and refusal from the command.
3. Stop Play, enable the switch, and enter Play again. Expect `mode=EditorTest`, the engine version, and `discovery: PASS; matches=1`.
4. Run `superuser_probe "quoted reason" tail` and `superuser_probe_local`. Expect two arguments and `EditorLocalTestEntry`; the latter must not be described as a trusted dedicated-console observation.
5. For the automated exercise, run the following from this repository with the installed `sbox-cli`. The script restores the switch/enabled state and removes its temporary duplicate. Inspect output rather than treating script completion as an assertion pass.

```powershell
& "$engine/bin/managed/sbox-cli.exe" --call run_csharp '{"path":"E:/.sbox/02-plugins/sbox-superuser/dev/verify-editor.cs"}'
& "$engine/bin/managed/sbox-cli.exe" --call read_console '{"filter":"Superuser T01","limit":100}'
```

`$engine` is the user's `FACEPUNCH_ENGINE` value. For another checkout, substitute the absolute script path. Observed on 2026-10-17: default and disabled refusals, local-entry refusal when disabled, local-entry success with opt-in, both quoted-argument paths, empty argument array, nine-argument limit, and duplicate-probe refusal. No duplicate diagnostic execution occurred. Local raw evidence is retained under ignored `dev/out/evidence/`; the durable findings are this report.

## Dedicated-server checks still required

Use an adopting test game referencing this single library, with one probe in its startup scene. Run the game's actual `sbox-server` process; editor play and player-hosted games cannot satisfy this gate. The library does not launch or manage that process. A local game project is supported by the [dedicated-server setup documentation](https://sbox.game/dev/doc/networking/dedicated-servers/), but no adopting dedicated test game or two-client session was supplied for this task.

1. Keep **Allow Editor Testing** off. Confirm `mode=DedicatedServer` and one discovery sentinel.
2. Run both probe console commands locally; then run them from a remote client. Record exact caller/null behavior and confirm the non-forwarded entry cannot run with server authority from a client. Repeat with and without the native `sboxsuperuser.probe` claim. Do not infer an owner from any result.
3. Repeat quoted, empty and malformed argument cases. Check native access flags using isolated test bindings before choosing the final T11 transport.
4. With two real clients, send `!superuser-probe` from A. The original must be suppressed, A alone must receive the private result, and B must see neither. Ordinary chat, `!superuser-probe-extra`, system messages and already-suppressed messages must remain unaffected. Repeat after scene replacement/hotload.
5. Configure **Rejected Test Steam Id** to a test account and connect it. Confirm rejection without OnConnected/OnActive or gameplay access. Clear the value and reconnect successfully. Record the callback order.
6. Enable **Probe Storage On Start** only in a disposable test environment. The probe touches only `superuser-t01/dedicated/{baseline,candidate}.txt` under the host's `FileSystem.Data`; editor tests use a separate `editor` directory. Save the token, restart, and compare `storage previous` with it. Candidate truncation must leave baseline readable; a corrupt baseline must be preserved and reported as failure. Then run a real process-interruption test before deciding recovery guarantees.

On this workstation `FileSystem.Data.GetFullPath("superuser-t01")` resolved beneath the engine install on **C:**. Storage writes were left disabled to honor the workspace's requirement to keep test output on **E:**. An appropriate engine-supported data location/test installation must be selected before this disk test; do not substitute arbitrary OS file access in the library.

## Validation and task ledger

| Check | Result |
|---|---|
| Editor compiler for `notpointless.chomnr_sbox_superuser` | Passed after removing inaccessible atomic API: 0 errors, 0 warnings. |
| Generated `Code/chomnr_sbox_superuser.csproj` | `dotnet build` passed: 0 errors, 0 warnings. Output redirected to `dev/out/build` for the command-line build. |
| Editor checks | Observations above passed; no dedicated or two-client claim. |
| Existing test project | `dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj` completed; **no test cases exist**. This is not a unit-test pass count. Existing test project was not changed. |
| Workspace structure/Core harness | `sbox-check` passed: `SboxSuperuser: follows the standard.` The checker also built the existing plain .NET Core harness. |
| Publication checks | `um publish check .`: 0 failures; one absolute-user-path warning in ignored build output `dev/out/build/Sandbox.Engine.xml`. No engine files are tracked or pushed. `git diff --check` passed. |
| Repository | Created `zeljkovranjes/sbox-superuser` with private visibility; pushed the implementation/report and confirmed visibility through GitHub. |
| Runtime/editor code split | No editor code is shipped, so no `Editor/*.csproj` exists. |
| External runtime dependencies | None added; all executable probe logic is in this library. |

Implementation: `Code/SboxSuperuser/Components/CompatibilityProbe*.cs`, internal adapters in `Code/SboxSuperuser/Engine/Compatibility/`, and `dev/verify-editor.cs`. Implementation history starts at `b33aace`; editor exercise at `f88259b`; local-console fallback at `f89608e`. The final report/verification commits follow those changes in the private repository. No package has been published on sbox.game.

The editor generated `.vscode`, `ProjectSettings`, `Libraries` and `Localization` even when opened on this library. These were created during this test and archived to ignored `dev/out/editor-generated` after closing the owned editor, preserving the marked library layout. The user's pre-existing editor remained open and untouched. Future direct library-editor sessions may regenerate those directories; they are not part of the shipped library. The native runtime project generated under `Code/` remains available for builds.

T01 remains **Blocked**, specifically on dedicated runtime, two-client transport/privacy, admission ordering and recoverable disk-write evidence. Available editor validation is complete. Next action is to supply that evidence and update this record; **do not start T02 until the user instructs it**.
