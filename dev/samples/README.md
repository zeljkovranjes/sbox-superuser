# Two independent adopting games

These development fixtures are separate games, not additional required libraries. Both use the same Superuser checkout and the same unchanged library Core.

| Game | Player representation | Command / permission | Optional capability |
|---|---|---|---|
| Arena | `ArenaPawn` scene component with health | `arena.heal <steamid>` / `arena.heal` | `IPlayerRecovery` |
| Colony | `ColonyRoster` scene system containing supply records, no pawn | `colony.supply <steamid>` / `colony.supply` | `IColonySupplies` |

## Run a sample

1. Copy one sample folder to its own game workspace. In this standard workspace, scaffold `superuser-arena-sample` or `superuser-colony-sample` with `new-sbox-project` first, using root namespace `SuperuserArena` or `SuperuserColony`; then overlay that sample's original files.
2. Add the local Superuser checkout using the editor library manager. The verified local setup mounts it through `Libraries/sbox-superuser`, a directory junction to this repository. Do not copy library C# into the game.
3. Open the sample `.sbproj`, wait for compilation, and open `Assets/scenes/sample.scene`. The project selects that scene at dedicated startup. Platform chat is enabled by the engine's generated defaults on the tested build; check **Project Settings > Platform** if it is disabled in your environment.
4. For editor testing, switch on **Allow Editor Testing** on the one Superuser Bootstrap. For production leave the switch off and use an actual dedicated server. `SampleSession` creates a private engine session for these fixtures; a production game supplies its existing session.
5. Put explicit owner Steam IDs in the adopting game's `FileSystem.Data/superuser/editor-owners.json` (editor), or `superuser/owners.json` (dedicated). Use `{"schemaVersion":1,"owners":["YOUR_DECIMAL_STEAM_ID"]}` with your actual ID. No ownership is inferred. Restart bootstrap after editing; re-enable the game administration component after a bootstrap restart in the same scene.
6. Enter Play. The game creates a host-side representation for each active engine player. Use `/admin arena.heal <steamid>` or `/admin colony.supply <steamid>` in the chatbox. Native console equivalents omit the slash. Results are private; the samples provide no visual admin UI.
7. For a normal player, an owner grants the exact game permission with `admin user <steamid> permission allow arena.heal` (or `colony.supply`). New permissions default to denied. Game effects are non-punitive; engine moderation uses the library's separate target-protection rules.
8. Disable the game administration component, turn off **Provide Gameplay Capability**, and re-enable it. An authorized command now returns `Unsupported`. Re-enable that option to restore the service. No service type or player-controller class is added to the library.

## Development verification

The parent `dev/verify-arena-sample.cs` and `verify-colony-sample.cs` scripts exercise actual editor-host native calls, denied defaults, a persisted grant to a non-owner, real gameplay state, missing capabilities and five moderation history rows. They require a disposable sample data directory with no owner file and intentionally create test owner/policy state. Snapshot data first and preserve the evidence afterward.

`verify-arena-dedicated.cs` and `verify-colony-dedicated.cs` run inside the respective actual dedicated processes. They create a clearly identified offline gameplay fixture, then call the real native console; they do not fabricate a live connection. They verify game effects, missing services, policy edits and moderation revision 3. Actual dedicated-client package deployment remains unverified because these games are unpublished.

Stop Play or the process before editing source. Gameplay records are neither replicated nor persistent. Authentication, permissions, moderation and storage remain in the one Superuser library. See [validation](../../docs/validation.md) for engine results and [API and modules](../../docs/api.md) for supported lifetimes.
