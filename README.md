# Superuser

Superuser provides trusted bootstrap, authenticated identities and explicit owner configuration for dedicated-server administration. Add one Superuser Bootstrap to your game's network scene; enable its editor switch for local testing. Public contracts and compatibility probes are included in the same s&box C# library. Administration commands, permission resolution, durable storage and UI are not implemented yet.

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

The dedicated console's local connection is not a player; use `superuser_local_identity` in the actual dedicated console. These APIs identify callers and explicit owners, but do not dispatch commands. Task 2's [metadata and interface contracts](docs/T02-CONTRACTS.md) remain available.

## Options

| Option | Default | What it does |
|---|---|---|
| Allow Editor Testing | `false` | Allows bootstrap or probe execution on an editor host; does not enable player-hosted release games. |
| Probe Storage On Start | `false` | Exercises isolated files under `FileSystem.Data/superuser-t01/<mode>/` and preserves an existing baseline. |
| Rejected Test Steam Id | Empty | Deliberately rejects only this test account at the admission callback. Empty accepts all connections. |

Bootstrap and compatibility probe each have their own Allow Editor Testing switch. Storage/admission options belong only to the probe. Bootstrap reads fixed host-local owner files; no owner list is stored in scene properties.

## How it works

A scene-owned coordinator initializes one runtime and suspends access when duplicate bootstraps appear. Identity comes from live engine connections; ownership comes only from validated host-local configuration. A separate non-forwarded dedicated-console route avoids trusting null or SteamId 0 callers. Scene replacement disposes the old runtime and loads explicit configuration for its replacement. Public contracts describe later systems without placeholder implementations; see [T03](docs/T03-BOOTSTRAP.md) for evidence and limits.

## Multiplayer

Dedicated servers are the only production target. Opt-in editor testing is development-only. Dedicated startup, console and interrupted-file checks passed; two real clients verified private replies and admission in editor test mode. Dedicated multiplayer validation is Blocked by the unpublished test game's missing package record. Player-hosted releases and host migration are unsupported.

## Limitations

- Task 3 adds bootstrap and identity only. No dispatcher, parser, permission resolver, module loader or durable store is installed.
- Task 1's unavailable dedicated multiplayer evidence remains marked Blocked; Task 2 does not certify that deployment.
- Chat commands use `/`; the future administration root will be `/admin`. The current probe does not implement administration commands.
- The probe is for isolated testing and should be removed from a production scene.
- Do not enter credentials or sensitive arguments into diagnostic commands: bounded test arguments are logged.
- Storage checks establish neither power-loss durability nor a completed recoverable store.
- Future ImGui source can be staged in [dev/ui-source/sbox-imgui](dev/ui-source/sbox-imgui/README.md); no UI is integrated.

## Development

Run `sbox-check` and `dotnet build Code/chomnr_sbox_superuser.csproj` after the editor generates it. Run both test projects: `dotnet test tests/SboxSuperuser.Contracts.Tests/SboxSuperuser.Contracts.Tests.csproj` and `dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj`. The original project has no test cases; the new contract suite verifies structural invariants through an engine-free harness. Run `dev/verify-contracts.cs` through the engine's `run_csharp` tool, and `dev/verify-editor.cs` with the isolated test scene playing. Plain .NET compilation does not replace the engine whitelist check.

Task 3 adds `dev/verify-bootstrap.cs` for isolated editor-host identity/owner checks, plus `dev/verify-dedicated-owners.cs` and `dev/verify-bootstrap-scenes.cs` for actual dedicated validation. The scripts restore owner-file contents; the scene test replaces the disposable test scene.

## License

No license selected yet. No third-party UI code is included.
