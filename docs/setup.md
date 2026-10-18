# Setup

[Guide](README.md) · Next: [Your first command](commands-tutorial.md)

## Add the library

Clone this repository and add it as a local library in your s&box game. Its identity is `notpointless.chomnr_sbox_superuser`. Keep the library separate from your game's `Code/` folder.

For a local Windows checkout, the verified alternative is a directory junction from your game's `Libraries/sbox-superuser` to the checkout. Create it before opening the game. No other runtime library or service is needed.

Add exactly one **Superuser Bootstrap** to the scene that owns your game's network session. The game must start that session itself. The [samples](../dev/samples/README.md) include a small session component.

## Configure an owner

Create this file under the **adopting game's** `FileSystem.Data`, replacing the example ID with your own:

```json
{"schemaVersion":1,"owners":["76561198000000001"]}
```

| Where you run | File | Bootstrap option |
|---|---|---|
| Dedicated server | `superuser/owners.json` | Leave Allow Editor Testing off |
| Editor host | `superuser/editor-owners.json` | Turn Allow Editor Testing on |

On the tested local engine, game data is under `<sbox>/data/<org>/<ident>#local/`. Use the adopting game's identity, not the library's. Published deployments may use another engine-managed data location. Keep owner files out of source control.

Restart the scene after changing the file. Nobody becomes an owner automatically. Once dedicated-console owner commands create managed ownership, that stored list takes precedence; see [recovery](recovery.md).

## Run a command

Enable **Project Settings > Platform > Chat Enabled** if you use platform chat. A new game can default to off. Save the scene and project settings before starting the session.

Enter Play in the editor, or start your actual dedicated server. Run `admin help` in the console, then `admin diagnostics` as an owner. In the chatbox, use `/admin help`.

Successful help confirms routing. Successful diagnostics confirms owner authority. The editor option does not enable player-hosted release games.

For the optional **F6** moderation view, add **Superuser Admin Menu** on clients. See [the menu guide](admin-menu.md). To replace local storage, configure it before enabling bootstrap; see [custom storage](storage-tutorial.md).

## Try a sample from a clean checkout

1. Copy `dev/samples/arena` to a new game folder. In this workspace, scaffold a new game first with root namespace `SuperuserArena`; copy the sample's `Code/` and `Assets/` into it.
2. Mount your fresh library checkout at `Libraries/sbox-superuser`. For a newly scaffolded game, set Startup Scene to `scenes/sample.scene` and network type to Multiplayer.
3. Open the game and sample scene. Let compilation finish.
4. Configure your editor owner file and enable Allow Editor Testing.
5. Enter Play. Run `admin arena.heal <your-steamid>`. It restores the sample pawn's health from 25 to 100.

These are development tools; none is a required external program for a deployed library. Dedicated remote-client deployment still needs a valid adopting-game package record on the tested engine.

## If it does not work

| Result | Check |
|---|---|
| Disabled / Unsupported | Active network session, one enabled bootstrap, dedicated host or editor opt-in |
| Denied | Correct owner file and Steam ID; restart after editing |
| NotFound for a custom command | Module registered after bootstrap became ready |
| DuplicateBootstrap | Remove the extra bootstrap |
| StorageUnavailable / degraded health | [Inspect and recover storage](recovery.md) |
| RestartRequired | Stop and restart the session before continuing |

Stop Play or the server before editing source. Managed module reload is supported; arbitrary live source replacement is not.
