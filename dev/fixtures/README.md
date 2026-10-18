# Compatibility host fixture

Development-only sources for an adopting game. All probe behavior stays in Superuser; LabSession only starts a private engine lobby.

1. Create a normal s&box game. In this workspace use `new-sbox-project "superuser-compatibility-lab" -RootNamespace SuperuserCompatibilityLab`.
2. Add the local Superuser library in the editor. The tested setup used a `Libraries/sbox-superuser` directory junction to its checkout. Never copy the library's runtime C# into the game.
3. Copy `LabSession.cs` into `Code/SuperuserCompatibilityLab/Components/` and `compatibility.scene` into `Assets/scenes/`. These are original test sources.
4. Open the game/scene in the editor. Set Startup Scene to `scenes/compatibility.scene`, network type to Multiplayer, enable platform chat, save and compile the scene.
5. The fixture defaults to dedicated testing with editor opt-in off. Enable Allow Editor Testing for the editor-host scenario. Probe Storage On Start writes disposable files; use test data. Leave Rejected Test Steam Id empty except during admission checks.
6. Follow the [T01 report](../../docs/T01-COMPATIBILITY.md). Dedicated clients require a valid published adopting game package record on this build.

This fixture has no player controller or administration logic. Do not ship its diagnostic scene in a production game.
