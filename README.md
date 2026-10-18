# Superuser

Superuser provides host-local administration storage, authenticated identities and explicit owner configuration for dedicated servers. Add one Superuser Bootstrap to your game's network scene; enable its editor switch for local testing. Versioned transactions, retained backups, permissions, typed commands and authoritative dispatch run in the same s&box C# library. Durable action audit and UI remain pending.

## Requirements

An adopting s&box game with an active engine network session, and an actual dedicated server for production. Editor-host tests require explicit opt-in; the supplied lab fixture starts its own private engine lobby. No external executable, service or runtime installation is required by the library.

## Install

This package is not published on sbox.game. Add this local library to a test game's library references using the s&box editor. Its identity is `notpointless.chomnr_sbox_superuser`. Source is in the private [GitHub repository](https://github.com/zeljkovranjes/sbox-superuser).

## Quick start

### In the editor

1. Add one **Superuser Bootstrap** to your game's network scene, or use the isolated [bootstrap fixture](dev/fixtures/README.md).
2. Enable **Allow Editor Testing** for editor-host tests. Leave it off for dedicated hosting.
3. Configure explicit owner Steam IDs in `FileSystem.Data/superuser/editor-owners.json` for editor testing, or `superuser/owners.json` for dedicated hosting. Start with the [empty example](dev/fixtures/owners.example.json); no owner is inferred automatically.
4. Enter Play and run `superuser_status` and `superuser_identity` in the console. The second command accepts no arguments.
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
| Probe Storage On Start | `false` | Exercises isolated files under `FileSystem.Data/superuser-t01/<mode>/` and preserves an existing baseline. |
| Rejected Test Steam Id | Empty | Deliberately rejects only this test account at the admission callback. Empty accepts all connections. |

Bootstrap and compatibility probe each have their own Allow Editor Testing switch. Storage/admission options belong only to the probe. Bootstrap reads fixed host-local owner files by default; explicit dedicated-local owner changes create managed overrides. No owner list is stored in scene properties. See the [owner recovery procedure](docs/T06-AUTHORITY.md).

## How it works

A scene-owned coordinator initializes one runtime and suspends access when duplicate bootstraps appear. Identity comes from live engine connections; ownership comes only from validated host-local configuration. Storage serializes transactions and acknowledges them after closing and validating a new generation and its commit marker. Corrupt committed data fails closed; explicit local recovery creates a new revision while retaining the damaged files and prior backups. Scene replacement retires old store handles; see [bootstrap](docs/T03-BOOTSTRAP.md) and [storage](docs/T04-STORAGE.md) for evidence and limits.

## Multiplayer

Dedicated servers are the only production target. Opt-in editor testing is development-only. Dedicated startup, console and interrupted-file checks passed; two real clients verified private replies and admission in editor test mode. Dedicated multiplayer validation is Blocked by the unpublished test game's missing package record. Player-hosted releases and host migration are unsupported.

## Limitations

- Tasks 1–9 are implemented. No native admin root, general chat ingress, module loader, moderation enforcement or UI is installed.
- Task 1's unavailable dedicated multiplayer evidence remains marked Blocked; Task 2 does not certify that deployment.
- Chat commands use `/`; the future administration root will be `/admin`. The current probe does not implement administration commands.
- The probe is for isolated testing and should be removed from a production scene.
- Do not enter credentials or sensitive arguments into diagnostic commands: bounded test arguments are logged.
- Storage requires one host process per data root. Process interruption/restart recovery is verified; filesystem flush-to-device and power-loss durability are not guaranteed.
- All committed generations are retained. Payloads and backup results are bounded; retention, audit queries and action outcomes remain later tasks. See the storage guide for limits.
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

Task 9 adds [authoritative dispatch](docs/T09-DISPATCH.md), with 157 contract and 29 Core cases. Host adapters call `ExecuteCommandAsync` with the live engine caller; trusted host code can issue revocable `CommandService` handles. Module registrations explicitly approve game capabilities. Durable action audit remains T10.

## License


No license selected yet. No third-party UI code is included.
