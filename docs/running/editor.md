# In the editor

Turn on **Allow Editor Testing** on the Superuser Bootstrap. The editor host then
runs the same runtime a dedicated server would, with its own owners and its own
data, kept apart from dedicated data under `superuser/store/editor/`.

Editor owners come from `superuser/editor-owners.json` in the game's data
folder:

```json
{"schemaVersion":1,"owners":["76561198000000001"]}
```

Restart the scene after editing it. The editor option does not enable
player-hosted release builds; it exists for testing only.

## Trying a sample from a clean checkout

1. Copy `dev/samples/arena` to a new game folder. In this workspace, scaffold a
   new game first with root namespace `SuperuserArena`, then copy the sample's
   `Code/` and `Assets/` into it.
2. Mount your library checkout at `Libraries/sbox-superuser`. For a newly
   scaffolded game, set Startup Scene to `scenes/sample.scene` and network type
   to Multiplayer.
3. Open the game and the sample scene, and let compilation finish.
4. Create your editor owner file and enable Allow Editor Testing.
5. Enter Play and run `admin arena.heal <your-steamid>`. The sample pawn's health
   goes from 25 to 100.

These are development tools; none of them is required by a deployed game.
