# Greeter: an example module

The greeter is a complete third-party module in one file,
[GreeterModule.cs](GreeterModule.cs). It is what a module you download, or write
for someone else's game, looks like: it brings its own commands, permission and
documentation, and loads itself.

| Command | What it does |
|---|---|
| `admin hello` | Replies "Hello! The greeter module is running." |
| `admin roll [sides]` | Rolls a die with 2 to 1000 sides, 6 if you leave it out |

## Using it

1. Copy `GreeterModule.cs` anywhere in your game's `Code` folder, and change its
   namespace to yours if you like.
2. Enter Play. The bootstrap finds the module and starts it; there is nothing to
   register.
3. As the owner, run `admin hello`. To let everyone use the greeter:

   ```text
   admin group player permission allow example.greeter.use
   ```

## What to look at

**It loads itself.** The `[AdminModule]` attribute is all it takes. Superuser
creates the module with its parameterless constructor when the bootstrap starts,
installs it and starts it. Without the attribute you would pass it to
`Superuser.RegisterModulesAsync` yourself, which is still the way for modules
that need constructor arguments.

```csharp
[AdminModule]
public sealed class GreeterModule : IAdminModule
```

**It says who made it.** The descriptor's `Version`, `Author` and `Website`
appear on the module's Overview page in **Superuser › Documentation**.

**It registers commands explicitly.** `RegisterCommand` takes a descriptor and a
handler, so no attributes are needed on the handler methods. The `roll`
argument is an `Integer` between 2 and 1000; the host checks the range before the
handler runs, and the admin menu turns it into a number field.

**It documents itself.** `AddDocumentation` adds a "Using the greeter" page next
to the generated Commands and Permissions pages. See
[Documenting your module](../../../docs/documenting.md).

**It needs nothing from the game.** That is why it fits any game. A module that
heals players or spawns items asks the game for a service instead; the
[arena and colony samples](../../samples/README.md) show how.

## In the admin menu

- **Server › Commands** lists `hello` and `roll`; `roll` has a number field.
- **Superuser › Documentation** shows the greeter's Overview, its guide and its
  generated Commands and Permissions pages.
- **Server › Permissions** lists `example.greeter.use`, so owners can grant it to
  a group without typing.

## How it is checked

The library's contract tests load this exact file through the automatic loader
and run both commands through the real dispatcher, including the permission
check and the 2 to 1000 range. It was also dropped into a game in the s&box
editor: the bootstrap found it, started it, and both commands answered.
