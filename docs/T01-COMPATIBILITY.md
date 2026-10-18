# T01: Engine compatibility

Status: **Probe and compatibility report complete, with unavailable evidence marked Blocked below.** This satisfies the T01 report deliverable; it is not production certification. T02–T20 are **Not started**.

Source: [specification 0.2](sbox-admin-specification.docx). User amendments: actual dedicated servers only in production, explicit editor testing switch, `/` chat prefix (future `/admin`), and [sbox-imgui](https://github.com/zeljkovranjes/sbox-imgui) for future UI. Its staging folder is `dev/ui-source/sbox-imgui/`; no UI is implemented.

All shipped code runs in one `notpointless.chomnr_sbox_superuser` library. The adopting test game only starts an engine session and references this library. Engine clients/server, CLI and .NET are development tools, not companion applications shipped with it.

## Environment and observed results

Tested 2026-10-17 on Windows: Steam app **590830**, build **25964806**, engine **26.10.13a**. Public API references were the installed Sandbox XML files. An isolated adopting game mounted this library locally. Actual dedicated server tests used editor opt-in off initially. Network tests used a separate editor host and two real engine clients, with the same Steam account but distinct authenticated connection IDs. The user's pre-existing editor was untouched.

| Integration point | Observations | Adapter decision / fallback |
|---|---|---|
| Library loading | **Pass:** actual dedicated and editor loaded the adopting startup scene and invoked the probe. | One scene component is the verified entry. Trusted bootstrap belongs to T03. |
| Runtime restriction | **Pass:** dedicated ran with editor switch off. Editor default-off, disabled and duplicate probes refused commands. Real clients reported Unsupported and refused local entry. | Host AND (dedicated OR editor with explicit opt-in). Editor switch cannot enable a release listen server. No host migration. |
| Attribute discovery | **Pass:** dedicated and editor found one attributed method and returned `T01-discovery-ok`. | TypeLibrary discovery/invocation; explicit registration fallback. No unrestricted reflection in shipped code. |
| Whitelist | **Pass for implemented probe:** engine compiler/hotload reported zero diagnostics and executed adapters. | Only public engine APIs. XML presence alone does not prove accessibility; atomic-write API failed compilation (below). |
| Forwarded console identity | **Observed:** dedicated-local native ConsoleSystem.Run injected a non-null host connection, SteamId 0. Editor-session client injected its own connection, IsHost false. | Use injected identity; null and SteamId 0 are not trusted-console credentials. Dedicated remote variant **Blocked** by missing game package record. |
| Local console entry | **Pass:** non-forwarded `superuser_probe_local` logged DedicatedLocalEntry on dedicated, EditorLocalTestEntry with editor opt-in, refused on client. | Fixed non-forwarded entry plus dedicated guard is the local-console path. Do not promote absent caller on forwarded route. No authority granted by T01. |
| Arguments | **Pass:** raw quoted input and argument-array calls retained `quoted reason`. Dedicated and remote-client array calls retained empty argument and embedded quotes. Zero arguments worked; nine refused. | Forward bounded engine tokens. Raw quoting is lost. Malformed native syntax remains **Blocked / uncharacterized**: reject unsupported syntax; serialized argument envelope over fixed transport is the fallback if future grammar requires it. Parser remains T08. |
| Native claims | **Observed:** exact `sboxsuperuser.probe` claim true on dedicated/editor host, false on remote client. | Read explicit claims on host, import no roles automatically. Explicit framework configuration is fallback. Claimed dedicated remote users **Blocked**. |
| Native settings | **Pass:** normal test ConVar set/readback and restoration; Protected test ConVar retained value despite attempted write, on dedicated and editor. | Explicit allowlisted descriptors retaining native flags. Dynamic registration/enumeration and all Admin/Cheat combinations are unverified; do not expose them. Fixed native transport plus configured logical root is fallback. |
| Chat ingress | **Pass, editor network:** exact `/superuser-probe` suppressed once. Ordinary chat, `!superuser-probe` and `/superuser-probe-extra` reached other client normally. | Host IChatEvent, authenticated Sender, suppress before replying. Already-suppressed/system messages guarded in code. Dedicated multiplayer **Blocked**. |
| Targeted replies | **Pass, editor network:** only A received the private result; B and host saw neither result nor command. Client-origin private-RPC-wrapper invocation produced no reply. | Host-only RPC filtered to authenticated sender; receiver checks host caller. This is not a malformed-packet penetration test. Dedicated multiplayer **Blocked**. |
| File IO | **Pass:** dedicated/editor baseline and candidate readback; dedicated invalid-baseline preservation. | FileSystem.Data. `WriteAllTextAtomic` appears in installed XML but is inaccessible to library compilation (CS1061). |
| Interruption/restart | **Pass for previous-file preservation:** incomplete candidate flushed with stream open, exact dedicated test PID forcibly stopped, restart read same valid baseline. | Retain prior validated generation/journal. This proves a fallback primitive, not complete store/recovery selection (T04). No atomic-replacement, fsync or power-loss guarantee. |
| Admission | **Pass, editor network:** accepted callbacks ordered AcceptConnection, OnConnected, OnActive. Matching test ID rejected/kicked without OnConnected/OnActive. Clearing ID allowed reconnection. | AcceptConnection early gate. Game-owned gameplay admission gate is fallback where unavailable; no pre-join promise there. Dedicated-client timing **Blocked**. |
| Lifecycle | **Observed:** editor hotload, duplicate refusal, single discovery per tested start, dedicated restart. | Component-scoped listeners, no static subscriptions. Require restart for unverified scene transitions; full lifecycle remains T12/T17. |

## Dedicated multiplayer prerequisite

The actual dedicated server successfully loaded the local test game. Direct loopback clients and a client preloaded with the local project both failed because the handshake requested its unpublished package record:

```text
Failed to fetch package info for notpointless.chomnr_superuser_compatibility_lab: package not found.
GameInstance load failed
```

The deployment path requires an adopting game with a valid published package record and matching client content. No sbox.game publication was performed. **Blocked:** dedicated-client forwarding, claimed remote identities, private chat delivery and admission timing on that deployment. Editor networking is the working development fallback, not evidence these dedicated-client checks passed. This prerequisite does not prevent finishing T01's compatibility report, but prevents claiming production certification.

## Reproduction

Use an isolated adopting game referencing this library. This workstation's game is `E:/.sbox/03-projects/superuser-compatibility-lab`; portable sources and [setup notes](../dev/fixtures/README.md) are in `dev/fixtures/`.

1. Open its `scenes/compatibility.scene` in the editor and compile the scene asset. Dedicated startup requires `.scene_c`; the initial launch without it failed. If Assets was created after mounting the game, restart the editor to mount it.
2. Keep Allow Editor Testing off for actual dedicated tests. Enable Probe Storage On Start only with disposable test files. Expect DedicatedServer, discovery PASS and native normal/protected setting PASS.
3. Run `superuser_probe "quoted reason" tail` and `superuser_probe_local` locally, through native console or equivalent ConsoleSystem.Run using the development bridge. Expect host connection/SteamId 0 and DedicatedLocalEntry respectively.
4. Enable Allow Editor Testing for editor networking, play and connect two clients. Run `dev/verify-editor.cs` through the bridge for default-off, disabled, duplicate, quoted/zero/too-many arguments and local-entry checks; it restores settings.
5. In A's chatbox send `/superuser-probe`: A alone receives the reply. Send ordinary chat, `!superuser-probe` and `/superuser-probe-extra`: B must see them. Automated observations used the real client's `Sandbox.Platform.Chat.Say` platform-chat path without driving keyboard input.
6. From A run forwarded and local console probes. Host must identify A for forwarded entry; local entry refuses on A. Edge call: `ConsoleSystem.Run("superuser_probe", "", "quoted \"word\"", "tail")`. Observed tokens were `["","quoted \"word\"","tail"]`.
7. Set Rejected Test Steam Id on host to A's ID. Disconnect/reconnect A: reject without connected/active callbacks. Clear the ID, reconnect and observe accepted callback order. Leave the property empty afterward.
8. Record dedicated baseline token. Run `dev/interrupt-candidate.cs`; after INTERRUPTION CHECKPOINT terminate only that test server PID with the candidate stream open. Restart on the same data and compare token. `dev/verify-storage.cs` verifies invalid-baseline preservation and restores the original in finally.

Example development launch arguments (substitute local paths; background launches use hidden windows):

```text
sbox-server.exe -mcp +net_allow_local 1 +net_local_port 45343 +port 27025 +net_query_port 27026 +game <lab.sbproj>
sbox-dev.exe -mcp -project <lab.sbproj> +net_local_port 45344
sbox.exe -mcp +net_local_port 45344 +connect local
```

Use `sbox-cli --connect <exact-pid> --call run_csharp '{"path":"<absolute-script-path>"}'`, followed by `read_console` to inspect results. Dedicated clients instead select port 45343 and need the package prerequisite first. The library never launches or depends on the CLI.

FileSystem.Data resolved beneath the engine install. The test game's newly created `.../sbox/data/notpointless/chomnr_superuser_compatibility_lab#local` is a directory junction to `E:/.sbox/03-projects/superuser-compatibility-lab/dev/data/runtime`, keeping physical test output on E:. This is test setup only; shipped code uses FileSystem.Data normally. Dedicated/editor markers have separate subdirectories.

Dedicated baseline before and after actual interruption: `T01:df8a7eb11d974261a1383b2bb6d10367`. Raw local evidence stays in ignored `dev/out/evidence/`; this report is the committed record.

## Validation and scope

- Engine whitelist compilation and runtime .NET builds: library and adopting game passed with zero errors/warnings. Both workspace/Core checks passed. `git diff --check` passed; publication lint found zero failures and one path warning in ignored generated engine XML, which is not tracked.
- Existing test projects contain **no test cases**. Their commands are run, but no unit-test pass count is claimed. Existing tests were not changed.
- No editor code is shipped. Runtime is under `Code/SboxSuperuser`; fixtures and trusted development scripts remain under `dev/`.
- [GitHub repository](https://github.com/zeljkovranjes/sbox-superuser) is private. No engine binaries, generated build output or extracted game assets are tracked. No sbox.game publication.
- No permission model, dispatcher, administrative store, moderation commands, UI or Task 2 contracts were implemented.
