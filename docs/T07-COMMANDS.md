# T07: Command registration and discovery

Status: **Complete.** Continue with T08–T10 and stop before T11.

## Registration

Call `Superuser.BeginCommandRegistration(moduleId)` on the eligible host main thread. Register exact permissions and descriptors, optionally call `DiscoverCommands()`, then `Commit()`. Retain the scope until shutdown; disposing it revokes its commands, aliases and permissions. A new committed scope replaces the entire prior catalog for that module; disposing an older scope cannot remove the replacement. This is an atomic registration primitive, not the T12 module loader.

All names, aliases, permission ownership and argument schemas validate before publication. Invalid replacement leaves the prior catalog intact. Names and aliases share one namespace. Command permissions must exist in the resulting catalog. Permission changes invalidate cached decisions; registration grants nobody a permission. The reserved `superuser.core` permission catalog cannot be replaced. Unloading a permission owner also removes dependent command entry points.

Limits: 128 modules, 512 commands, 2048 canonical/alias entries, 4096 permissions; 16 aliases and 32 arguments per explicit command. Identifiers are at most 96 characters, descriptions 2048; enum choices are unique ignoring case, at most 256. Required arguments precede optional arguments; only the final string may consume the remainder. Player arguments require an explicit target policy.

```csharp
var registration = Superuser.BeginCommandRegistration( "sample.dogs" );
registration.RegisterPermission( new PermissionDescriptor( "dog.spawn", "Spawn game-owned dogs" ) );
registration.DiscoverCommands();
registration.Commit();
// Retain registration; dispose on module shutdown from the host main thread.
```

Explicit fallback calls `RegisterCommand(descriptor, handler)` instead of discovery. The handler is the existing `Task<CommandResult> CommandHandler(CommandContext, CommandArguments)` delegate. `GetCommandDescriptors()` returns immutable host-local metadata, not permission-filtered client help.

## Supported attributed handlers

Public static methods returning exactly `CommandResult` or `Task<CommandResult>`, with `CommandContext` first and zero through eight typed arguments. Supported types: `string`, `int`, `long`, `float`, `double`, `bool`, enums, `CommandDuration`, `PlayerReference`. Optional arguments use CLR defaults; numeric defaults must respect bounds. A null default is supported only for optional strings. Enum inputs bind from the descriptor's canonical string choice. Integer/number transport values are `long`/`double`; the binder performs the declared conversion.

`AdminArgumentAttribute` is repeatable method metadata naming the exact CLR parameter, with optional stable key, description, numeric bounds, remainder flag and target policy. This location keeps discovery within supported TypeLibrary APIs. Metadata for nonexistent/context/repeated parameters fails registration. Unsupported types, instance methods, void returns, reference arguments and open generic signatures are rejected with module and method names. Only TypeLibrary-visible methods can be discovered; use explicit registration where the engine does not expose a declaration.

Discovery uses `GetMethodsWithAttribute(... onlyStatic: false)` so visible instance handlers are rejected, not silently skipped. `ParameterInfo.HasDefaultValue` failed the actual whitelist; `IsOptional` and `DefaultValue` passed. TypeLibrary does not expose generic delegate definitions, and its generic factory rejects a constructed Task as a type argument. Small internal generic signature validators use supported TypeLibrary construction and closed `CreateDelegate` calls to reject unbound generic methods without executing them. No unrestricted reflection or dynamic code generation ships.

## Verification

110 contract/Core cases passed, including 13 new registration cases: canonical/alias collisions, failed-batch rollback, replacement identity, old-handle disposal, permission ownership, runtime revocation, malformed argument schemas and dependent-command removal. Existing tests are unchanged.

Actual engine 26.10.13a editor PID 21804 compiled library and adopting fixture with zero diagnostics. `dev/verify-command-discovery.cs` exercised the compiled game fixture: typed async invocation returned `typed|3|4000000000|1.5|2.5|True|Playful|True`; the dog command used its optional count and returned Unsupported without an `IDogSpawner`. Five invalid handler variants failed with actionable errors. Re-discovery replaced objects and disposal removed aliases.

`dev/verify-command-registration-host.cs` exercised the public facade in an opt-in editor host: registration made a permission known without granting it, explicit registration replaced discovered commands, disposal invalidated permissions, and disabling/re-enabling bootstrap with editor opt-in off refused registration. No storage mutations were needed. First script attempts exposed development-script imports, engine generic-factory limits and the documented need to re-enable bootstrap after changing its switch; these were corrected before the recorded passing runs.

Generated runtime builds, plain-.NET Core checks, both test projects and engine whitelist compilation passed. The original test project still has no test cases. No dedicated-client networking or physical dog spawning is claimed. Task 1's unpublished adopting-game deployment limitation remains; it does not block this host-local task. No native admin root, general chat dispatcher or UI was added.
