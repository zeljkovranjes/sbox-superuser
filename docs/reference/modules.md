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
it and starts it. Adding the module's file to a game is then all it takes, which
is how a module from someone else is meant to be used.

```csharp
[AdminModule]
public sealed class GreeterModule : IAdminModule
```

Modules load in dependency order whatever order they are found in. One that
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
