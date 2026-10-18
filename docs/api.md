# API and modules

[Guide](README.md) · Previous: [Permissions](permissions.md) · Next: [Native settings](settings.md)

Import `SboxSuperuser`. Call host APIs on the engine main thread after bootstrap is ready. Client code submits through console, chat or an authenticated game RPC.

## Core entry points

| API | Use |
|---|---|
| `Superuser.GetDiagnostics()` | Bootstrap state and runtime ID |
| `TryGetPlayerIdentity(connection, out actor)` | Resolve a live engine player |
| `IsOwner(connection)` | Check explicit ownership |
| `ExecuteCommandAsync(connection, request, origin)` | Dispatch authenticated player input |
| `ExplainPermissionAsync(connection, steamId, key)` | Explain access |
| `CheckPunitiveTargetAsync(connection, steamId)` | Compare target protection |
| `ApplyPolicyChangeAsync(connection, change, origin)` | Owner-only policy change |
| `RegisterModulesAsync(modules)` | Install a managed module catalog |
| `TryHandleChat(sender, text, requestId)` | Consume a custom-chat command |
| `CheckOrdinaryChat(sender)` | Authorize ordinary chat before broadcast |
| `GetStore()` | Host-local versioned storage |
| `QueryAuditAsync(connection, query)` | Permission-filtered audit pages |

The source of truth for signatures is the [facade and public types](../Code/SboxSuperuser). `Core/` contains engine-independent rules; integrations use the public facade instead of internal classes.

Do not expose `GetStore`, `CreateCommandService` or module registration directly to player input. These are trusted host-code facilities. Use the live engine connection for player requests, never an actor assembled from message fields.

## Module lifetime

An `IAdminModule` has a `ModuleDescriptor`, `StartAsync` and `StopAsync`. Start registers permissions, commands and explicitly approved game services. Use `registration.Track` for disposable listeners.

The manager orders dependencies, validates API versions and publishes registrations atomically. Failed startup rolls back. Disable removes entry points and listeners, drains work for a bounded time, and keeps stored data. Work that outlives cancellation prevents overlapping restart.

The [ping module](../dev/fixtures/PingModule.cs) is a complete minimal example. The [game samples](../dev/samples/README.md) demonstrate direct `BeginCommandRegistration` scopes: retain a committed scope until shutdown and dispose it when its component stops.

## Results and retries

| Result | Meaning |
|---|---|
| Success / NoChange | Confirmed completion / no change needed |
| Denied / InvalidArguments / NotFound | Access, input or command lookup failed |
| Unsupported | Runtime or required capability unavailable |
| Conflict | Revision, duplicate fingerprint or capacity conflict |
| StorageUnavailable | Required storage could not complete |
| Cancelled / Failed | Cancelled before confirmed completion / safe failure |
| Unknown | The outcome cannot be confirmed; inspect before retrying |

A request ID identifies one submission by actor and origin. Retry with that same ID and payload; a different payload conflicts. Deduplication lasts five minutes in the active runtime, not across restarts. Commands are reauthorized at execution.

## API compatibility

Current module contract version: **1**. The source candidate is **0.1.0-rc.1**, unpublished.

Module dependencies declare the required API version. Incompatible versions refuse startup. Public facade signatures, descriptors and serialized component names are compatibility commitments; internal classes are not. Breaking public changes require a new API version and migration notes. Additive changes keep existing version-1 integrations working.

Storage has separate format and domain schemas. Unknown versions fail closed; no silent migration or downgrade occurs. Back up before updating.
