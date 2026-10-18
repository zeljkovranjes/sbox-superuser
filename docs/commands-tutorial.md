# Your first command

[Guide](README.md) · Previous: [Setup](setup.md) · Next: [Custom chat](chat.md)

We will add `ping`, which replies with `Pong!`. It needs no player controller or game service.

## Define the module

Copy [PingModule.cs](../dev/fixtures/PingModule.cs) into your game's Engine layer. Change `SuperuserExamples.Engine` to your game's namespace.

The command itself is:

```csharp
[AdminCommand( Name = "ping", Description = "Reply with Pong!", Permission = "example.ping",
	ModuleId = "example.ping", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
public static CommandResult Ping( CommandContext context )
{
	return new CommandResult( CommandStatus.Success, "Pong!" );
}
```

`StartAsync` registers the exact permission and discovers this module's attributed methods. The module ID must match the attribute. Declaring a permission does not grant it.

## Register it

For a runnable starter, copy [PingSetup.cs](../dev/fixtures/PingSetup.cs) into your game's Components layer, update its namespaces, and add one Ping Setup to the bootstrap scene. It waits for bootstrap and installs the module.

If your game already has async host setup, use this after `Superuser.GetDiagnostics().RuntimeId` has a value instead:

```csharp
var result = await Superuser.RegisterModulesAsync(
	new IAdminModule[] { new PingModule() } );
```

Import `SboxSuperuser` and the namespace containing `PingModule`. Call this once for the runtime, with all your managed modules in the array. Check `result.Status` before reporting that setup succeeded. Register again for a new scene/runtime, not every frame.

## Use it

As an explicit owner, enter `/admin ping` in the chatbox. The reply is private. Console users enter `admin ping`.

To let a normal player use it, run:

```text
admin user <steamid> permission allow example.ping
```

Use `deny` to refuse it explicitly, or `unset` to remove the direct entry. Group grants work too.

## Add arguments

The [dog example](../dev/fixtures/DogCommands.cs) shows attributed target and numeric parameters. Supported types include strings, numbers, booleans, enums, durations and `PlayerReference`. An ambiguous player name is rejected; use a Steam ID to select an offline target.

For game effects, register an explicit service with the module and obtain it through `context.Services`. The [arena and colony samples](../dev/samples/README.md) show this without coupling the library to their player classes.

Return `CommandResult` or `Task<CommandResult>`. Honor `context.Cancellation` in long operations. A read-only command is still permission-checked and audited.
