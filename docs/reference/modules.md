# Modules

An `IAdminModule` has a `ModuleDescriptor`, a `StartAsync` and a `StopAsync`.
Start registers permissions, commands and explicitly approved game services. Use
`registration.Track` for disposable listeners so they are released with the
module.

The [ping module](../../dev/fixtures/PingModule.cs) is a complete minimal
example.

## Loading automatically

Mark a module with `[AdminModule]` and it loads itself: when the bootstrap
starts, Superuser creates it with its public parameterless constructor, installs
it and starts it. Adding the module's folder to a game is then all it takes,
which is how a module from someone else is meant to be used. Drop-in modules go
in the game's `Code/Superuser/Modules` folder, one folder per module. The folder
is only a convention, since everything under `Code` compiles, but it keeps
third-party modules apart from the game's own code. Don't start a module's
namespace with `Superuser`: it would hide the `Superuser` class.

```csharp
[AdminModule]
public sealed class GreeterModule : IAdminModule
```

Modules load in dependency order whatever order they are found in. The console
lists each one as it starts, with its version, author and what it added. One that
can't load (a missing dependency, a duplicate ID, a constructor that throws) is
skipped with a warning in the console and doesn't stop the others. Passing a
marked module to `RegisterModulesAsync` as well is harmless: it is skipped there.
Use `RegisterModulesAsync` for modules that need constructor arguments or must
start after your own setup.

The [greeter example](../../dev/examples/greeter/README.md) is a complete module
that loads this way.

## Lifetime

The manager orders modules by their dependencies, validates API versions and
publishes each module's registrations atomically. A failed start rolls back
everything that module registered.

Disabling a module removes its entry points and listeners, drains its work for a
bounded time, and keeps its stored data. Work that outlives cancellation
prevents an overlapping restart. Required security modules cannot be disabled
through ordinary module commands.

Owners can list, enable and disable modules with `admin modules list`,
`admin modules enable <id>` and `admin modules disable <id>`.

## Menu windows

A module can add its own windows to the admin menu, under **Modules**. A window
is described, not drawn: headings, text, buttons that run a command, a command's
generated form, and live replies of read-only commands, which refresh every few
seconds.

```csharp
registration.AddMenuWindow( "Greeter", window => window
	.Text( "The smallest useful module, with its own window in the admin menu." )
	.Button( "Say hello", "hello" )
	.CommandForm( "roll" ) );
```

Everything in a window runs one of the module's commands, so the host checks the
permission, asks for confirmation when the command changes something, and
records it in Activity; no module code runs on players' machines. Each viewer
receives only the elements whose commands they may run, and a window only when
something in it is theirs to use. A module can add up to 4 windows of 64 elements.
The [reports example](../../dev/examples/reports/README.md) has a staff window and
a player window side by side.

![Module windows in the admin menu](../module-windows.png)

## Documentation

For a walkthrough, see ***[Documenting your module](../documenting.md)***.

Every module gets a page in **Superuser › Documentation**, with the same four
parts so they all read alike: an **Overview** (description, version, author,
website, state, dependencies), your **guides**, then **Commands** and
**Permissions**, which Superuser generates from your registrations. A module
with no guides still has useful documentation.

Say who made it on the descriptor, and add guides while starting:

```csharp
public ModuleDescriptor Descriptor { get; } = new( "example.ping", "A ping command", 1 )
{
	Version = "1.0.0",
	Author = "Your name",
	Website = "https://example.com"
};

registration.AddDocumentation( "Using ping", "# Using ping\n\nRun `admin ping` to check commands reach the server." );
registration.AddDocumentationFile( "Setup", "docs/ping-setup.md" );
```

`AddDocumentationFile` reads Markdown from the game's mounted files. Pages keep
the order you add them; a module can have 32 pages of up to 32 KB, with titles of
up to 64 characters. The Markdown subset is headings, paragraphs, bullet and
numbered lists, code blocks, tables, inline code, bold and italic. A code block
tagged `effect` is colored as an effect script. Direct registration scopes have
the same two methods.

## Direct registration

For a component that owns a few commands without a full module, use
`Superuser.BeginCommandRegistration`. Keep the committed scope until shutdown and
dispose it when the component stops. The
[game samples](../../dev/samples/README.md) use this.

## Compatibility

The current module contract is version **1**. Module dependencies declare the API
version they need, and an incompatible version refuses to start.

Public facade signatures, descriptors and serialized component names are
compatibility commitments; internal classes are not. A breaking public change
requires a new API version and migration notes. Additive changes keep version-1
integrations working.

Storage has its own format and domain schemas. Unknown versions fail closed; no
silent migration or downgrade happens. Back up before updating.
