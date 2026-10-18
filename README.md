# Superuser

Console and chat administration for s&box dedicated servers. Commands, permissions, moderation and storage run inside one C# library.

Add the local library `notpointless.chomnr_sbox_superuser` to your game, then add **Superuser Bootstrap** to its network scene. Enable **Allow Editor Testing** when testing in the editor. The package is not published yet.

## Guide

See the [guide](docs/README.md) for setup, a command tutorial and custom chat integration.

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
- **Core API** is `Superuser`: dispatch commands, check permissions, register modules and read host-local records. See the [API reference](docs/api.md).
- **Game code** supplies its own player actions. The [samples](dev/samples/README.md) show component-based players and a roster without pawns.

[Command reference](docs/commands.md) · [Recovery](docs/recovery.md) · [Changelog](CHANGELOG.md)

## License

No license selected.
