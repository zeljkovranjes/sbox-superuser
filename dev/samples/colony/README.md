# Superuser Colony Sample

Demonstrates Superuser integration in an isolated game. Uses a game-specific player representation and optional gameplay capability. Requires the local Superuser library.

## Requirements

The local library `notpointless.chomnr_sbox_superuser` and s&box. No external runtime service.

## Install

Open this unpublished game in the editor. Add the local Superuser checkout to Libraries; do not copy its runtime source into this game.

## Quick start

### In the editor

1. Open `Assets/scenes/sample.scene`.
2. Enable **Allow Editor Testing** on the one Superuser Bootstrap for this editor test. The saved default is off.
3. Configure your explicit Steam ID in this game's `FileSystem.Data/superuser/editor-owners.json`, using the library's owner-file schema.
4. Enter Play. Sample Session creates a private engine network session; the game adapter registers when bootstrap becomes eligible.
5. Enter `/admin colony.supply <your-steamid>` in the platform chatbox, or `admin colony.supply <your-steamid>` in the native console.
6. To check an absent capability, disable the game administration component, turn off **Provide Gameplay Capability**, and re-enable it. An authorized call returns Unsupported.

### In code

The game registers its own command and optional capability through the library's public API. For local health:

```csharp
var diagnostics = SboxSuperuser.Superuser.GetDiagnostics();
Sandbox.Log.Info( diagnostics.State );
```

## Options

| Option | Default | What it does |
|---|---|---|
| Allow Editor Testing | false | Enables development-only editor hosting on Superuser Bootstrap. |
| Provide Gameplay Capability | true | Registers the game's optional effect; re-enable the game administration component after changing it. |

## How it works

The game's command changes a scene roster member's supplies. Its adapter maps a stable Steam ID to the game's own representation. Superuser owns authentication, permissions, moderation, dispatch and audit; none of that logic is copied into the game. The optional capability is approved only for this command registration scope.

## Multiplayer

Dedicated production hosts and explicit editor-host tests only. The sample starts a private engine lobby; release player hosting and host migration are unsupported. Moderation uses engine connections regardless of player representation.

## Limitations

- This is an executable integration fixture, not a complete playable game or visual admin UI.
- Gameplay state is host-local demonstration data and is not persistent or replicated.
- Stop Play before changing source. Re-enable the game adapter if you restart bootstrap in the same scene.
- Actual dedicated-client deployment needs a valid adopting game package record on the tested engine; these samples are unpublished.

## Development

Build the generated runtime project and run the library's colony verification scripts in this isolated game. Plain .NET builds do not apply the engine whitelist.

## License

No license selected. Original sample source; no engine assets included.
