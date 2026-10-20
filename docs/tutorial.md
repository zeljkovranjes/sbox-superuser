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

The quickest way is to mark the class with `[AdminModule]`: the bootstrap then
creates and starts it by itself, and there is nothing else to write. Modules
like that, from you or anyone else, go in `Code/Superuser/Modules/<module>/`; the
[greeter example](../dev/examples/greeter/README.md) works that way. The steps
below register the module yourself instead, which you need when a module takes
constructor arguments.

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

## Seeing it in the admin menu

Add one **Superuser Admin Menu** to a GameObject that exists on every client and
press **F6**. Your module is already in the menu; nothing extra is registered for
it.

### In the Commands window

**Server › Commands** lists every command the viewer may run, yours included.
Pick `ping` and press **Run**: the reply pops up in the corner and the attempt is
recorded in **Activity**, exactly as if it was typed in the console.

Commands with arguments get a form built from the method's parameters, so the
types you choose decide what admins see:

| Parameter type | Field in the form |
|---|---|
| `string` | A text box (a wide one for the last, remainder argument) |
| `int`, `long`, `float`, `double` | A number box; the host enforces `Minimum` and `Maximum` |
| `bool` | A checkbox |
| an `enum` | A dropdown of its values |
| `PlayerReference` | A Steam ID box with a picker of online players |
| `CommandDuration` | A duration box with presets such as 30m, 2h and 7d |

`[AdminArgument]` sets the label and limits. From the
[dog example](../dev/fixtures/DogCommands.cs), this gives a player picker and a
number box from 1 to 5:

```csharp
[AdminCommand( Name = "dog.spawn", Description = "Spawn a dog for a player", Permission = "dog.spawn", ModuleId = "sample.dogs", Aliases = new[] { "dog" } )]
[AdminArgument( "target", Description = "Online recipient", TargetPolicy = CommandTargetPolicy.OnlinePlayer )]
[AdminArgument( "count", Description = "Number of dogs", Minimum = 1, Maximum = 5 )]
public static CommandResult Spawn( CommandContext context, PlayerReference target, int count = 1 )
```

Commands that change something (the default `ExecutionPolicy`) ask the admin to
confirm before they run; `ReadOnly` commands like `ping` run straight away.

### In Documentation and Permissions

**Superuser › Documentation** shows your module with its commands and
permission, generated from what you registered. Give it a version, an author and
guide pages with ***[Documenting your module](documenting.md)***.

Owners can grant `example.ping` from **Server › Permissions** instead of typing
the command: pick a group or a player and allow the key, which is listed there as
soon as your module registers it.

### A window of its own

Give the module a window under **Modules** with `AddMenuWindow` in `StartAsync`.
It is built from headings, text, buttons, command forms and live command
replies, and everything in it runs your commands with the usual checks:

```csharp
registration.AddMenuWindow( "Greeter", window => window
	.Text( "The smallest useful module, with its own window in the admin menu." )
	.Button( "Say hello", "hello" )
	.CommandForm( "roll" ) );
```

See [Menu windows](reference/modules.md#menu-windows) and the
[reports example](../dev/examples/reports/README.md) for a larger one.

### A button in your own UI

Outside the admin menu, use the same private request API the menu itself uses:
it goes through the same permission check, audit and private reply as the
console. The [replacement example](../dev/fixtures/ReplacementAdminExample.cs) is
a complete, compiled version of this pattern.

```csharp
public sealed class PingButton : Component, IAdminResultEvent
{
	private Guid _request;

	/// <summary>Call this from your button.</summary>
	public void Press() => _request = Superuser.RequestAdminCommand( new AdminCommandRequest( "ping" ) );

	/// <summary>Only the reply to this button's request is handled here.</summary>
	public void OnAdminResult( AdminReply reply )
	{
		if ( reply.RequestId != _request ) return;
		Log.Info( $"{reply.Status}: {reply.Message}" );
	}
}
```

Put it on a GameObject in the client scene, call `Press()` from a Razor button,
and show `reply.Message` wherever your UI wants it. Arguments go in the second
parameter of `AdminCommandRequest`, in the order the command declares them.

## Where to go from here

- A complete module that loads itself: the
  [greeter example](../dev/examples/greeter/README.md).
- Arguments, services and results: ***[Commands](reference/commands.md)***.
- Gameplay effects without coupling the library to your classes: the
  [arena and colony samples](../dev/samples/README.md).
- The in-game menu: ***[Reference: the admin menu](menu.md)***.
