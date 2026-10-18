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

## Task 12 module fixtures

Copy `SampleCounterModule.cs`, `SamplePulseSource.cs`, `SamplePulseSubscription.cs`, `FailingStartupModule.cs` and `HeldCommandModule.cs` into the game's `Engine/Modules/`. Run `dev/verify-module-lifecycle.cs` on the isolated editor host or dedicated server. It installs a removable module, invokes native management commands, checks five reloads, retained versioned data, listener cleanup, startup rollback and bounded shutdown of a deliberately held command. It restores owner configuration and bootstrap settings afterward. The deliberately failing and held modules are development-only fixtures; never install them in a production game.

## Task 14 custom chat fixtures

Copy these original sources into the adopting game, preserving the example namespace:

| Files | Destination beneath Code/SuperuserCompatibilityLab |
|---|---|
| IExampleChatMessageEvent.cs | Api/ |
| ExampleChatGate.cs, ChatExampleModule.cs | Engine/Chat/ |
| IndependentChatExample.cs, FailingChatResultObserver.cs | Components/ |
| CustomChatExample.razor, CustomChatExample.razor.scss | UI/ |

Follow [T14](../../docs/T14-CUSTOM-CHAT.md) for the two-client sequence and cleanup. Setup creates runtime objects only, hides the platform overlay locally and installs a deliberately permissive test counter. Neither saved scene settings nor production permissions should be changed. The failing observer is only for the fallback check. The independent transport passes the actual RPC caller to the library; neither client supplies identity or permission claims. These fixtures are examples inside the adopting game, not another required library or service.

## Task 15 native binding fixtures

Copy `NativeBindingExample.cs` into the adopting game's `Engine/Settings/`. It declares test ConVars (ordinary, Saved, Protected, Admin, Cheat, clamped and sensitive) and a counter ConCmd. Trusted host setup calls its `CreateModule()` and passes the result to `Superuser.RegisterModulesAsync`. Do not ship these diagnostics.

Run `verify-native-bindings.cs`, `verify-native-binding-failure.cs` and `verify-native-setting-lifetime.cs` on the isolated editor or actual dedicated host. The failure script interrupts a real settings generation write through the existing development checkpoint. The lifetime script temporarily configures the editor host as an owner, then restores the file. Restart the dedicated fixture and run `verify-native-setting-restart.cs` for persisted pending-state evidence. See [T15](../../docs/T15-NATIVE-BINDINGS.md) for setup, restoration and limitations.

## Task 16 moderation checks

No additional shipped game adapter is required for engine connections. The T14 independent backend already calls the ordinary-message hook. Use the existing bootstrap and chat fixtures, then follow the ordered scripts and cleanup in [T16](../../docs/T16-MODERATION.md). Tests create temporary owner/policy state, disconnect owned test clients, and deliberately inject an unsupported moderation schema. Use only the isolated fixture and retain its recorded recovery backup. Dedicated process tests verify stored UTC expiry; actual dedicated-client deployment remains limited by the unpublished adopting game.
