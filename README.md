# Superuser

<p align="center">
  <img src="docs/logo.svg" width="160" alt="Superuser — navy caped figure and shield">
</p>

Administration for s&box dedicated servers, with console commands, slash chat and an optional moderation menu. Commands, permissions, moderation and storage run inside one C# library.

Add the local library `notpointless.chomnr_sbox_superuser` to your game, then add **Superuser Bootstrap** to its network scene. Enable **Allow Editor Testing** when testing in the editor.

## Guide

See the [guide](docs/README.md) for setup, commands, custom chat and [custom storage](docs/storage-tutorial.md).

For the bundled menu, add **Superuser Admin Menu** to the client scene and press **F6**. Its key is configurable. Disable or remove that component to use your own UI; console and chat keep working. See [the menu guide](docs/admin-menu.md).

## Example

A command returns a result. Superuser checks the caller's permission and sends the reply privately.

```csharp
[AdminCommand( Name = "ping", Description = "Reply with Pong!", Permission = "example.ping",
	ModuleId = "example.ping", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
public static CommandResult Ping( CommandContext context )
{
	return new CommandResult( CommandStatus.Success, "Pong!" );
}
```

This method belongs to the [complete ping module](dev/fixtures/PingModule.cs). Follow [your first command](docs/commands-tutorial.md) to register it, then enter `/admin ping` in the chatbox or `admin ping` in the console.

## Details

- **Commands** use the same dispatcher from console, platform chat and your own chat UI.
- **Permissions** use exact keys, groups and explicit owners. Policy changes are owner-only; owners cannot punish each other.
- **Moderation** includes kick, ban, warnings and text mute, with stored history.
- **Core API** is `Superuser`: dispatch commands, check permissions, register modules, select storage and request private admin views. See the [API reference](docs/api.md).
- **Game code** supplies its own player actions. The [samples](dev/samples/README.md) show component-based players and a roster without pawns.

[Command reference](docs/commands.md) · [Recovery](docs/recovery.md) · [Changelog](CHANGELOG.md)

## License

No license selected.

Bundled ImGui code retains its MIT license. See [third-party notices](THIRD_PARTY_NOTICES.md).
