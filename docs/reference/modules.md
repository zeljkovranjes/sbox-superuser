# Modules

An `IAdminModule` has a `ModuleDescriptor`, a `StartAsync` and a `StopAsync`.
Start registers permissions, commands and explicitly approved game services. Use
`registration.Track` for disposable listeners so they are released with the
module.

The [ping module](../../dev/fixtures/PingModule.cs) is a complete minimal
example.

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
