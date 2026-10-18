# Superuser

Superuser provides console administration, host-local records and explicit owner configuration for dedicated servers. Add one Superuser Bootstrap to your game's network scene; enable its editor switch for local testing. Permissions, access commands, typed dispatch and durable audit records run in the same s&box C# library. General chat integration, moderation and UI remain pending.

## Requirements

An adopting s&box game with an active engine network session, and an actual dedicated server for production. Editor-host tests require explicit opt-in; the supplied lab fixture starts its own private engine lobby. No external executable, service or runtime installation is required by the library.

## Install

This package is not published on sbox.game. Add this local library to a test game's library references using the s&box editor. Its identity is `notpointless.chomnr_sbox_superuser`. Source is in the private [GitHub repository](https://github.com/zeljkovranjes/sbox-superuser).

## Quick start

### In the editor

1. Add one **Superuser Bootstrap** to your game's network scene, or use the isolated [bootstrap fixture](dev/fixtures/README.md).
2. Enable **Allow Editor Testing** for editor-host tests. Leave it off for dedicated hosting.
3. Configure explicit owner Steam IDs in `FileSystem.Data/superuser/editor-owners.json` for editor testing, or `superuser/owners.json` for dedicated hosting. Start with the [empty example](dev/fixtures/owners.example.json); no owner is inferred automatically.
4. Enter Play and run `admin help` in the console. An explicitly configured owner can run `admin diagnostics` and `admin group list`. `superuser_status` and `superuser_identity` remain local troubleshooting entries.
5. Reload the scene or disable/re-enable bootstrap after editing owner configuration. See [bootstrap setup](docs/T03-BOOTSTRAP.md) for failures and dedicated-console diagnostics.
6. For Task 1 checks, additionally add **Superuser Compatibility Probe** and opt into its editor mode. Its commands remain `superuser_probe`, `superuser_probe_local` and `/superuser-probe` in the chatbox.
7. Host code can obtain `Superuser.GetStore()` for versioned transactions. Follow the [storage and recovery guide](docs/T04-STORAGE.md); editor and dedicated records are isolated.

### In code

Use the façade on the host for diagnostics and live engine identity checks:

```csharp
using SboxSuperuser;
using Sandbox;

var health = Superuser.GetDiagnostics();
if ( Superuser.TryGetPlayerIdentity( Connection.Local, out var actor ) )
{
	Log.Info( $"{actor.SubjectId}: owner={Superuser.IsOwner( Connection.Local )}" );
}
```

The dedicated console's local connection is not a player; use `superuser_local_identity` in the actual dedicated console. These APIs identify callers and explicit owners, but do not dispatch commands. Task 2's [metadata and interface contracts](docs/T02-CONTRACTS.md) remain available. For storage, use the [transaction example](docs/T04-STORAGE.md#host-code) on the main thread and check for `Committed` before reporting success.

## Options

| Option | Default | What it does |
|---|---|---|
| Allow Editor Testing | `false` | Allows bootstrap or probe execution on an editor host; does not enable player-hosted release games. |
| Command Root | `admin` | Logical administration root. Other names use `superuser <root> <command>` in console; re-enable bootstrap to apply. |
| Probe Storage On Start | `false` | Exercises isolated files under `FileSystem.Data/superuser-t01/<mode>/` and preserves an existing baseline. |
| Rejected Test Steam Id | Empty | Deliberately rejects only this test account at the admission callback. Empty accepts all connections. |
| Audit maximum records | `512` | Owner-configured retained rows, from 1–4096, also subject to a 384000-byte state limit. |
| Audit retention days | `30` | Owner-configured creation age, from 1–3650 days; active intents stay protected. |

Bootstrap and compatibility probe each have their own Allow Editor Testing switch. Storage/admission options belong only to the probe. Bootstrap reads fixed host-local owner files by default; explicit dedicated-local owner changes create managed overrides. No owner list is stored in scene properties. See the [owner recovery procedure](docs/T06-AUTHORITY.md).

## How it works

A scene-owned coordinator initializes one runtime and suspends access when duplicate bootstraps appear. Identity comes from live engine connections; ownership comes only from validated host-local configuration. Storage serializes transactions and acknowledges them after closing and validating a new generation and its commit marker. Commands persist an audit intent before execution and a correlated outcome afterward; interrupted work remains Unknown. Audit queries filter access, redact arguments and enforce bounded retention, while policy backups remain retained. See [bootstrap](docs/T03-BOOTSTRAP.md), [storage](docs/T04-STORAGE.md) and [audit](docs/T10-AUDIT.md) for recovery and limits.

## Multiplayer

Dedicated servers are the only production target. Opt-in editor testing is development-only. Dedicated startup, console and interrupted-file checks passed; two real clients verified private replies and admission in editor test mode. Dedicated multiplayer validation is Blocked by the unpublished test game's missing package record. Player-hosted releases and host migration are unsupported.

## Limitations

- Tasks 1–12 are implemented. General chat ingress, moderation enforcement and UI remain pending.
- Task 1's unavailable dedicated multiplayer evidence remains marked Blocked; Task 2 does not certify that deployment.
- Chat commands use `/`; the future administration root will be `/admin`. The current probe does not implement administration commands.
- Native dynamic registration and full engine-name enumeration are unavailable. Visible managed-name collisions are reported; see the [console guide](docs/T11-CONSOLE.md) for the fixed transport and configured-root fallback.
- The probe is for isolated testing and should be removed from a production scene.
- Do not enter credentials or sensitive arguments into diagnostic commands: bounded test arguments are logged.
- Storage requires one host process per data root. Process interruption/restart recovery is verified; filesystem flush-to-device and power-loss durability are not guaranteed.
- Policy and owner backups retain all committed generations. Only audit history has automatic retention; damaged historical audit files are preserved and reported as degraded.
- Future ImGui source can be staged in [dev/ui-source/sbox-imgui](dev/ui-source/sbox-imgui/README.md); no UI is integrated.

## Development

See the [implementation ledger](docs/IMPLEMENTATION.md) for completed tasks, current scope and remaining work.

Run `sbox-check` and `dotnet build Code/chomnr_sbox_superuser.csproj` after the editor generates it. Run both test projects: `dotnet test tests/SboxSuperuser.Contracts.Tests/SboxSuperuser.Contracts.Tests.csproj` and `dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj`. The Core project now tests command grammar; the contract suite verifies structural invariants through an engine-free harness. Run `dev/verify-contracts.cs` through the engine's `run_csharp` tool, and `dev/verify-editor.cs` with the isolated test scene playing. Plain .NET compilation does not replace the engine whitelist check.

Task 3 adds `dev/verify-bootstrap.cs` for isolated editor-host identity/owner checks, plus `dev/verify-dedicated-owners.cs` and `dev/verify-bootstrap-scenes.cs` for actual dedicated validation. The scripts restore owner-file contents; the scene test replaces the disposable test scene.

Task 4 adds `dev/verify-durable-store.cs` and the dedicated restart/interruption scripts documented in [T04](docs/T04-STORAGE.md). These use disposable test keys and preserve test evidence. The contract/Core suite contains 55 tests, including 22 storage cases; the original empty test project remains unchanged.

Task 5 adds [immutable policy and permission resolution](docs/T05-PERMISSIONS.md), 24 tests (79 total), and `dev/verify-permissions.cs` for the actual engine assembly. Creating a policy evaluator does not install or authorize a live server policy.

Task 6 adds [owner-only policy administration, offline target protection and local owner recovery](docs/T06-AUTHORITY.md), with 97 tests in total. Use `Superuser.ApplyPolicyChangeAsync`, `ExplainPermissionAsync`, `CheckPunitiveTargetAsync` and `GetAuthorityDiagnostics` from authenticated host integrations. An explicit dedicated-local owner change creates managed ownership; thereafter the managed store takes precedence over the bootstrap owner file.

Task 7 adds [atomic command registration and typed discovery](docs/T07-COMMANDS.md), with 110 tests in total. Begin a module registration scope, declare permissions and commands, commit, and retain it until shutdown. The compiled dog sample demonstrates an optional game capability.

Task 8 adds [strict argument parsing, player targeting and generated syntax](docs/T08-PARSING.md). The suites contain 138 contract and 20 Core cases. Use `TokenizeCommandText`, `ParseCommandArguments` and `GetCommandSyntax`; parsing grants no execution authority.

Task 9 adds [authoritative dispatch](docs/T09-DISPATCH.md). Host adapters call `ExecuteCommandAsync` with the live engine caller; trusted host code can issue revocable `CommandService` handles. Module registrations explicitly approve game capabilities.

Task 10 adds [durable audit intents and outcomes](docs/T10-AUDIT.md). Use `QueryAuditAsync` for permission-filtered pages, `ConfigureAuditRetentionAsync` for owner-only settings and `GetAuditDiagnostics` for host-local health. The current suites contain 180 contract and 32 Core cases. Editor storage-failure checks and an actual dedicated-process interruption/restart verify the outcome guarantees.

Task 11 adds [native console commands and permission-filtered help](docs/T11-CONSOLE.md). Run `admin help`, `admin help group.create`, or `superuser <configured-root> help`. Access edits require owner authority and audit queries remain private. The current suites contain 186 contract and 44 Core cases; actual dedicated console and two-client editor checks passed.

Task 12 adds [managed module lifetimes](docs/T12-MODULES.md). Trusted host setup calls `RegisterModulesAsync`; owners use `admin modules list`, `admin modules enable <id>` and `admin modules disable <id>`. Failed startup rolls back registrations, listeners are tracked for cleanup, and timed-out work prevents overlapping restart. The current suites contain 198 contract and 49 Core cases. Editor and actual dedicated checks cover five reloads, persisted data retention and bounded cancellation. Arbitrary source hot reload remains a T17 verification item.

## License


No license selected yet. No third-party UI code is included.
