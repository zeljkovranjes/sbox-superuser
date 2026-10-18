# Superuser

Superuser provides public administration contracts and compatibility probes for dedicated servers. Use the exported types to describe integrations, and add the probe component to an isolated scene for engine checks with explicit editor opt-in. Everything runs inside one s&box C# library. Runtime administration systems and UI are not implemented yet.

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

Task 2 exports metadata and interfaces in `SboxSuperuser`. This creates a descriptor only; no command is registered or executed:

```csharp
using SboxSuperuser;

var descriptor = new CommandDescriptor(
	"example.inspect", "Inspect the game's optional example service.",
	"example.inspect", "example",
	executionPolicy: CommandExecutionPolicy.ReadOnly );
```

No administration facade exists yet. See the [contract notes](docs/T02-CONTRACTS.md) for signatures, ownership and deferred behavior.

## Options

| Option | Default | What it does |
|---|---|---|
| Allow Editor Testing | `false` | Allows probe execution on an editor host; does not enable player-hosted release games. |
| Probe Storage On Start | `false` | Exercises isolated files under `FileSystem.Data/superuser-t01/<mode>/` and preserves an existing baseline. |
| Rejected Test Steam Id | Empty | Deliberately rejects only this test account at the admission callback. Empty accepts all connections. |

## How it works

Public contracts describe commands, arguments, identities, results, permissions, modules and versioned storage without providing runtime implementations. Internal engine boundaries keep untrusted request payloads separate from host-issued context; structural rules remain independent of s&box. The unchanged probe component checks engine APIs and consumes only `/superuser-probe` for a targeted diagnostic reply. Production execution requires a dedicated host; editor testing requires explicit opt-in. See the [Task 1 findings](docs/T01-COMPATIBILITY.md) and [Task 2 contract record](docs/T02-CONTRACTS.md).

## Multiplayer

Dedicated servers are the only production target. Opt-in editor testing is development-only. Dedicated startup, console and interrupted-file checks passed; two real clients verified private replies and admission in editor test mode. Dedicated multiplayer validation is Blocked by the unpublished test game's missing package record. Player-hosted releases and host migration are unsupported.

## Limitations

- Task 2 delivers contracts only. No bootstrap, dispatcher, parser, permission resolver, module loader or durable store is installed.
- Task 1's unavailable dedicated multiplayer evidence remains marked Blocked; Task 2 does not certify that deployment.
- Chat commands use `/`; the future administration root will be `/admin`. The current probe does not implement administration commands.
- The probe is for isolated testing and should be removed from a production scene.
- Do not enter credentials or sensitive arguments into diagnostic commands: bounded test arguments are logged.
- Storage checks establish neither power-loss durability nor a completed recoverable store.
- Future ImGui source can be staged in [dev/ui-source/sbox-imgui](dev/ui-source/sbox-imgui/README.md); no UI is integrated.

## Development

Run `sbox-check` and `dotnet build Code/chomnr_sbox_superuser.csproj` after the editor generates it. Run both test projects: `dotnet test tests/SboxSuperuser.Contracts.Tests/SboxSuperuser.Contracts.Tests.csproj` and `dotnet test tests/SboxSuperuser.Tests/SboxSuperuser.Tests.csproj`. The original project has no test cases; the new contract suite verifies structural invariants through an engine-free harness. Run `dev/verify-contracts.cs` through the engine's `run_csharp` tool, and `dev/verify-editor.cs` with the isolated test scene playing. Plain .NET compilation does not replace the engine whitelist check.

## License

No license selected yet. No third-party UI code is included.
