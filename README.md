# Superuser

Superuser currently provides Task 1 compatibility probes for a dedicated-server administration library. Add its probe component to an isolated test scene to check engine integration points; enable the editor testing switch when testing locally. Everything runs inside this one s&box library. Administration commands and UI are not implemented yet.

## Requirements

An adopting s&box game and a dedicated server for production validation. Editor tests require explicit opt-in. No external executable, service or runtime installation is required by the library.

## Install

This package is not published on sbox.game. Add this local library to a test game's library references using the s&box editor. Its identity is `notpointless.chomnr_sbox_superuser`. Source is in the private [GitHub repository](https://github.com/zeljkovranjes/sbox-superuser).

## Quick start

### In the editor

1. Start an empty test scene and add **Superuser Compatibility Probe** to one GameObject.
2. For editor testing, enable **Allow Editor Testing**. Leave it off for dedicated-server validation.
3. Enter Play mode and run `superuser_probe "quoted reason" tail` in the console.
4. Check the `[Superuser T01]` output. Editor runs must say `mode=EditorTest` and show two arguments.
5. From a connected client's chatbox, send `/superuser-probe`. Only that client should receive the private result.
6. Follow the [compatibility report](docs/T01-COMPATIBILITY.md) for storage, admission and dedicated-server checks.

### In code

No administration facade exists in Task 1. Use the probe component and native diagnostic commands above; public contracts belong to Task 2.

## Options

| Option | Default | What it does |
|---|---|---|
| Allow Editor Testing | `false` | Allows probe execution on an editor host; does not enable player-hosted release games. |
| Probe Storage On Start | `false` | Exercises isolated files under `FileSystem.Data/superuser-t01/<mode>/` and preserves an existing baseline. |
| Rejected Test Steam Id | Empty | Deliberately rejects only this test account at the admission callback. Empty accepts all connections. |

## How it works

The component calls internal engine adapters to observe native console, type discovery, storage and chat APIs. Production execution requires a dedicated host; an editor host additionally requires the testing switch. The native console probe records the engine-provided caller without granting authority. The chat probe consumes only the exact message `/superuser-probe` and sends a host-authenticated reply to its sender. Checks and adapter decisions are recorded in the [Task 1 report](docs/T01-COMPATIBILITY.md).

## Multiplayer

Dedicated servers are the only production target. Opt-in editor testing is development-only. Dedicated startup, console and interrupted-file checks passed; two real clients verified private replies and admission in editor test mode. Dedicated multiplayer validation is Blocked by the unpublished test game's missing package record. Player-hosted releases and host migration are unsupported.

## Limitations

- Task 1's compatibility probe and report are complete, with unavailable dedicated multiplayer evidence marked Blocked. Task 2 has not started.
- Chat commands use `/`; the future administration root will be `/admin`. The current probe does not implement administration commands.
- The probe is for isolated testing and should be removed from a production scene.
- Do not enter credentials or sensitive arguments into diagnostic commands: bounded test arguments are logged.
- Storage checks establish neither power-loss durability nor a completed recoverable store.
- Future ImGui source can be staged in [dev/ui-source/sbox-imgui](dev/ui-source/sbox-imgui/README.md); no UI is integrated.

## Development

Run `sbox-check`, `dotnet build Code/chomnr_sbox_superuser.csproj` after the editor generates it, and `dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj`. The existing test project currently has no test cases; engine probes provide the relevant evidence. With the test scene playing, run `dev/verify-editor.cs` through the engine's `run_csharp` development tool. Read the report for exact commands and required observations.

## License

No license selected yet. No third-party UI code is included.
