# Compatibility host fixture

Development-only sources for an adopting game. All probe behavior stays in Superuser; LabSession only starts a private engine lobby.

1. Create a normal s&box game. In this workspace use `new-sbox-project "superuser-compatibility-lab" -RootNamespace SuperuserCompatibilityLab`.
2. Add the local Superuser library in the editor. The tested setup used a `Libraries/sbox-superuser` directory junction to its checkout. Never copy the library's runtime C# into the game.
3. Copy `LabSession.cs` into `Code/SuperuserCompatibilityLab/Components/` and `compatibility.scene` into `Assets/scenes/`. These are original test sources.
4. Open the game/scene in the editor. Set Startup Scene to `scenes/compatibility.scene`, network type to Multiplayer, enable platform chat, save and compile the scene.
5. The fixture defaults to dedicated testing with editor opt-in off. Enable Allow Editor Testing for the editor-host scenario. Probe Storage On Start writes disposable files; use test data. Leave Rejected Test Steam Id empty except during admission checks.
6. Follow the [T01 report](../../docs/T01-COMPATIBILITY.md). Dedicated clients require a valid published adopting game package record on this build.

This fixture has no player controller or administration logic. Do not ship its diagnostic scene in a production game.

## Task 3 bootstrap fixture

`bootstrap.scene` adds one Superuser Bootstrap to the diagnostic scene, with Allow Editor Testing off. Set it as the isolated game's startup scene for dedicated checks, or enable the switch in the editor. Keep `LabSession.cs` to start the test network session. Production games provide their own network session.

`owners.example.json` grants nobody. Copy it to the adopting game's FileSystem.Data as `superuser/owners.json` for dedicated hosting, or `superuser/editor-owners.json` for editor testing. Add only the explicit decimal Steam IDs you intend to make owners. Restart bootstrap after editing the file. Owner files stay host-local and are never copied into scene properties.

Run `superuser_status`, `superuser_identity` and, only on an actual dedicated console, `superuser_local_identity`. The player diagnostic accepts no arguments. These commands observe identity; they do not dispatch administration actions. See [T03](../../docs/T03-BOOTSTRAP.md) for verification scripts and limits.

## Task 7 command fixtures

Copy `IDogSpawner.cs` and `DogMood.cs` into the adopting game's `Api/`, and `DogCommands.cs`, `InvalidCommandSamples.cs`, `EmptyCommandServices.cs` into `Engine/Commands/`. Keep the `SuperuserCompatibilityLab` namespace or adjust it consistently for your game. These files are development fixtures, not additional runtime libraries.

Run `dev/verify-command-discovery.cs` through `run_csharp` after the game compiles. It invokes handlers in the actual library/game assemblies, including expected failures. The dog command returns Unsupported without a game-provided `IDogSpawner`; no dog assets are supplied. In a playing editor host, `dev/verify-command-registration-host.cs` checks the public host API and restores the editor switch.

## Task 9 dispatcher fixtures

Copy `IDispatchProbe.cs` into the game's `Api/`, and `DispatchProbe.cs` plus `DispatchCommands.cs` into `Engine/Commands/`. They provide a compiled main-thread counter and asynchronous hold barrier, not moderation or dog spawning. Test commands deliberately use the default help permission to avoid editing stored lab policy; never ship them in a production game.

With the isolated bootstrap scene playing, run `dev/verify-dispatch-editor.cs` for the authenticated player route. On an actual dedicated instance, run `dev/verify-dispatch-dedicated.cs` for the separate dedicated-local boundary and service queue. Both remove their registrations; the editor script restores its bootstrap switch.
