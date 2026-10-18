# Tutorial: your first command

This tutorial takes an s&box game from no administration at all to a working
`ping` command that replies `Pong!`. It needs no player controller and no game
service, so it fits any game.

All of the code is shown on this page and is also provided in runnable form:
[PingModule.cs](../dev/fixtures/PingModule.cs) and
[PingSetup.cs](../dev/fixtures/PingSetup.cs).

This tutorial assumes you have read briefly about the **host runtime**,
**commands** and **permissions** on the [*Core concepts*](concepts.md) page.

## Adding the library

Clone this repository and add it to your game as a local library. Its identity
is `notpointless.chomnr_sbox_superuser`. Keep it separate from your game's
`Code/` folder. On Windows, the verified alternative is a directory junction from
your game's `Libraries/sbox-superuser` to the checkout, created before opening
the game. Nothing else needs installing.

Add exactly one **Superuser Bootstrap** to the scene that owns your game's
network session. Your game starts that session itself; the
[samples](../dev/samples/README.md) include a small session component.

## Configuring an owner

Nobody becomes an owner automatically. Create this file in the **game's**
`FileSystem.Data`, with your own Steam ID:

```json
{"schemaVersion":1,"owners":["76561198000000001"]}
```

| Where you run | File | Bootstrap option |
|---|---|---|
| Dedicated server | `superuser/owners.json` | Leave Allow Editor Testing off |
| Editor host | `superuser/editor-owners.json` | Turn Allow Editor Testing on |

On the tested engine the folder is `<sbox>/data/<org>/<ident>#local/`, using the
game's identity, not the library's. Keep owner files out of source control, and
restart the scene after editing one.

If you use platform chat, enable **Project Settings > Platform > Chat Enabled**;
new games can default to off.

Enter Play, then run `admin help` in the console and `admin diagnostics` as the
owner. A help listing confirms routing; diagnostics confirms owner authority.

## Defining the module

Copy [PingModule.cs](../dev/fixtures/PingModule.cs) into your game's Engine
layer and change its namespace to yours. The command itself is:

```csharp
[AdminCommand( Name = "ping", Description = "Reply with Pong!", Permission = "example.ping",
	ModuleId = "example.ping", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
public static CommandResult Ping( CommandContext context )
{
	return new CommandResult( CommandStatus.Success, "Pong!" );
}
```

The module's `StartAsync` registers the exact permission and discovers its
attributed methods. The module ID must match the attribute. Declaring a
permission does not grant it to anyone.

## Registering it

For a runnable starter, copy [PingSetup.cs](../dev/fixtures/PingSetup.cs) into
your Components layer, update its namespaces, and add one **Ping Setup** to the
bootstrap scene. It waits for the bootstrap and installs the module.

If your game already has asynchronous host setup, call this instead once
`Superuser.GetDiagnostics().RuntimeId` has a value:

```csharp
var result = await Superuser.RegisterModulesAsync(
	new IAdminModule[] { new PingModule() } );
```

Pass all of your managed modules in one array, once per runtime, and check
`result.Status` before reporting success. A new scene means a new runtime and a
new registration; never register every frame.

## Using it

As the owner, enter `/admin ping` in the chatbox or `admin ping` in the console.
The reply is private.

To let an ordinary player use it:

```text
admin user <steamid> permission allow example.ping
```

`deny` refuses it explicitly; `unset` removes the entry so groups decide again.

## Where to go from here

- Arguments, services and results: ***[Commands](reference/commands.md)***.
- Gameplay effects without coupling the library to your classes: the
  [arena and colony samples](../dev/samples/README.md).
- The in-game menu: ***[Reference: the admin menu](menu.md)***.
