# T12: Module lifecycle

Status: **Implementation and runtime checks passed; final package verification in progress.** Implements dependency and API-version checks, staged registration, owner-only enable/disable, bounded cancellation and listener cleanup. Required security modules stay enabled. No stored data is deleted when a module stops.

The manager owns module registrations. Commands publish only after startup succeeds. Modules must register every listener's disposable subscription with their registration scope and honor cancellation; arbitrary trusted C# cannot be forcibly interrupted. A timed-out module remains quarantined until its work and cleanup finish, preventing overlapping reloads.

## Install a module

Implement `IAdminModule` in trusted game integration code or an internal library module. `Descriptor` declares a stable ID, positive API version and exact-version dependencies. Required absence, duplicates, repeated dependencies, cycles (including installed optional edges) and incompatible installed dependencies reject the entire catalog addition. An absent optional dependency is permitted. Catalog capacity is 128 including two required framework services; IDs are at most 96 characters.

Call the following once from host setup after bootstrap is ready:

```csharp
var source = new SamplePulseSource();
var module = new SampleCounterModule( source );
var result = await Superuser.RegisterModulesAsync( new IAdminModule[] { module }, cancellation );
```

The sample types are provided in [development fixtures](../dev/fixtures/README.md), not required dependencies. Setup is trusted host code; never expose this factory as a client RPC. Registration validates all additions before reserving IDs, then starts installed modules in dependency order. Already enabled modules do not start twice. A startup failure leaves that module faulted and its dependencies intact; the catalog remains installed for diagnosis and retry.

`StartAsync` receives staging operations only: `RegisterPermission`, `RegisterCommand`, `RegisterService<T>`, `DiscoverCommands`, and `Track(IDisposable)`. It cannot commit its own scope. Track every subscription or owned resource while staging. If startup or commit fails, commands and permissions remain unpublished, tracked resources are disposed in reverse order, and `StopAsync` is called after startup finishes. Late tracking after rollback disposes the supplied handle immediately and refuses the registration. Cleanup attempts every handle even when one throws.

The existing low-level `BeginCommandRegistration` remains available for explicitly caller-owned lifetimes. It cannot replace a manager-owned ID. Prefer the module API for reloadable features; low-level registrations are labeled separately in module listings.

## Operator commands

Run `admin modules list`, `admin modules disable sample.counter`, and `admin modules enable sample.counter`. These commands use the native dispatcher, private responses and durable audit. Only current owners and the dedicated-local console may change module state, even if an ordinary player is directly granted `admin.security.manage`. Required framework security services and modules marked `IsRequiredSecurityModule` cannot be disabled, including by owners. This deliberately exceeds the minimum protection against ordinary staff.

Enable starts required dependencies first. Disable refuses while an enabled, starting, stopping or quarantined required dependent remains. Optional dependencies do not prevent disable; consumers must tolerate their absence. Stop consumers first. Enabled flags are session-local: host setup determines what starts after a server restart. This task does not add a separate settings persistence format.

## Shutdown and bounds

Disable first removes commands and permissions, cancels queued/running requests and disposes tracked listeners. It then waits for active dispatch, including audit completion, before calling `StopAsync`. Stored documents are untouched. Completed retries retain their original result; disabling and re-enabling a module does not execute an old request again.

Startup and shutdown waits each have a five-second monotonic deadline. Work that ignores cancellation stays quarantined; a later enable refreshes its state only after startup, execution and cleanup actually finish. Throwing cleanup leaves the module quarantined until the session is restarted after repairing its code. Shutdown cancellation is cooperative, not thread termination or rollback. Synchronous module code must never block the engine main thread.

The normal serial queue remains bounded at 64 outstanding / 8 per actor, with 1,024 remembered requests / 128 per actor. Only the two built-in lifecycle commands use reserved capacity: 8 outstanding / 2 per actor, 128 remembered / 32 per actor. This lets disable run while a normal handler is awaiting completion. Combined maximums are 72 outstanding and 1,152 remembered requests. Retry conflicts are checked across both queues; public modules cannot select the control queue. Both retain completed requests for five minutes without evicting unexpired entries.

## Verification and limits

247 cases pass: 198 contract and 49 Core. The 230 pre-T12 cases are unchanged. New cases cover graph rejection, atomic catalog validation, invisible pending startup, collision rollback, dependency ordering, owner protection, repeated enable/disable, listener cleanup failures, late registration, interrupted startup, active and queued cancellation, quarantine, session disposal, and a control request while normal dispatch is held.

Actual engine 26.10.13a / Steam build 25964806: editor PID 25616 and dedicated PID 25848 ran `dev/verify-module-lifecycle.cs`. Five reload cycles each produced exactly five command executions, five pulse callbacks and five stops; disabled commands returned NotFound and listeners fell to zero. A versioned sample document remained readable after every disable. Injected startup failure published neither command nor extra listener. Required security disable was denied. The held handler received cancellation; disable returned Unknown after approximately 5.05 seconds, restart was refused, and releasing the handler completed one cleanup with an attributable Unknown audit outcome.

The engine whitelist rejects both tested `Task.WhenAny` overloads. Shutdown therefore yields in short supported timer slices against a monotonic stopwatch. Compilation is verified inside the actual engine, in addition to .NET builds.

Editing runtime constructor lambdas during development left an existing hot-loaded delegate without a matching substitution and a newly added field uninitialized. Restarting play mode created a valid runtime. This is recorded evidence for T17; T12's repeated **module** reload checks do not certify arbitrary source hot reload or cross-scene behavior. Dedicated-client deployment remains subject to the T01 package prerequisite. No publication was performed.

Backup before fixture writes: `E:/06-um-data/backups/superuser-t12-lab-data/20261018-003545.zip` (43 files). Raw test results are in ignored `dev/out/t12-*`. Owner configuration is restored by the script. Data files are development evidence only.
