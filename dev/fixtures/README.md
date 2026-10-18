# Integration examples

These sources belong in an adopting game, not the library's runtime folder. Adjust namespaces to match your game.

| Example | Files | Guide |
|---|---|---|
| Ping | PingModule.cs → Engine; PingSetup.cs → Components | [First command](../../docs/tutorial.md) |
| Typed command | DogCommands.cs, DogMood.cs, IDogSpawner.cs | [Commands](../../docs/reference/commands.md) |
| Platform/custom chat UI | CustomChatExample.razor and .scss → UI | [Chat](../../docs/reference/chat.md) |
| Independent chat | IndependentChatExample.cs → Components; ExampleChatGate.cs → Engine/Chat; IExampleChatMessageEvent.cs → Api | [Chat](../../docs/reference/chat.md) |
| Native settings | NativeBindingExample.cs → Engine/Settings | [Settings](../../docs/reference/settings.md) |

The custom-chat fixture uses namespace `SuperuserCompatibilityLab`; preserve it or update all references together. Its test counter module is deliberately permissive. Do not install diagnostic commands in production.

## Isolated test host

`LabSession.cs` starts a private engine session. `bootstrap.scene` contains the bootstrap and diagnostic probe with editor testing off. Enable it explicitly for editor checks. `owners.example.json` grants nobody until you add actual IDs; put it in the game's data directory using the [tutorial](../../docs/tutorial.md).

The other files support failure, concurrency and lifecycle probes under `dev/`. They deliberately change disposable state. Back up first, read script prerequisites and restore temporary owner files and scene options afterward.

Use the [executable game samples](../samples/README.md) for a smaller starting point.
