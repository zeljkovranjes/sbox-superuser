# T09: Authoritative command dispatch

Status: **Behavior verified; final package checks pending.** Continue with T10 durable audit after this task. No Task 11 native root or general chat ingress.

## Execution boundary

`Superuser.ExecuteCommandAsync(liveConnection, request, origin, cancellation)` is the player integration API. Call it on the authoritative host main thread with the engine-authenticated connection and `Console` or `Chat` origin. The request still carries only command, raw argument tokens and request ID. It contains no actor, role, permission, origin or service provider. Null callers and the dedicated host's Steam ID 0 are denied on this route.

The dispatcher creates host-issued context and reauthenticates the exact original connection before admission and execution. Foreign contexts, mismatched request IDs and incompatible actor/origin pairs fail closed. The separate dedicated-local identity resolver enters the same internal dispatcher; no new native console entry is installed. Public adapters must never promote missing callers or expose the host-code service factory to player input.

Admission validates input bounds, registration, current authority and permission, then parses stable player IDs. The serial worker rechecks runtime, cancellation, caller, exact registration object, permission, online target presence and punitive protection immediately before invoking the handler. A queued name cannot retarget a different player who later takes that name. Registration replacement/unload refuses queued old handlers. T06 owner-only policy administration remains independently enforced.

## Queue, retries and cancellation

All commands currently share one serial worker, including read-only commands. Limits are 64 outstanding entries globally and 8 per actor, across origins. The retry cache keeps at most 1024 entries globally and 128 per actor for five minutes after completion. Outstanding work never expires. Unexpired outcomes are not evicted to make room: capacity exhaustion returns Conflict with a retry-later message. Completed entries release handler closures, raw arguments, capabilities and cancellation registrations; the cache retains the key, payload hash and bounded result.

The retry key is actor kind + authenticated subject + origin + request ID. Identical retry payloads share the original result; changed payloads return Conflict. A retry lookup happens before reparsing targets, so completed results remain available to the same authenticated actor after names change. Reusing an ID after the retention window is a new request. Separate chat submissions must have fresh IDs. This is in-memory session deduplication; it is not a cross-restart exactly-once guarantee.

Cancellation before execution returns Cancelled and never invokes the handler. Cancelled queued entries retain their bounded queue slots until drained, preventing an unbounded cancelled-work backlog. Once a mutation starts, interruption or an exception returns Unknown; read-only interruption returns Cancelled and read-only exceptions return Failed. No rollback is implied. A handler must cooperate with its cancellation token; a hung handler can hold the serial queue, which then refuses excess work. Runtime shutdown cancels queued work and signals active contexts. Retrying does not replace the original request's cancellation ownership.

Handler exceptions are reported to host diagnostics, with a generic caller-safe message. Runtime diagnostics log exception types without exception text that might contain sensitive input. Results are bounded to a 1024-character message, 16 fields, 64-character keys, 1024-character values and 8192 aggregate field characters; invalid results become Unknown.

## Game capabilities and internal callers

`CommandRegistrationScope.RegisterService<T>(instance)` approves one exact capability type for that module's handlers. There are at most 32 unique types per module. `context.Services.GetService<T>()` returns only the module's approved instance or null. Other modules cannot obtain it through their contexts; no fallback implementation is constructed.

Trusted host code may call `Superuser.CreateCommandService(serviceId, permissions)` with at most 64 unique registered exact permission keys. The returned `CommandService.ExecuteAsync` uses the same validation and queue with InternalService origin; disposal revokes future and queued authority. Services are not owners, cannot administer policy through T06, and have no implicit punitive player rank. Missing optional capabilities remain Unsupported when the handler reports that result.

## Verification

157 contract tests and 29 Core tests passed (28 new cases over T08). Checks cover forged/foreign contexts, actor/origin and request-ID mismatches, null callers, retry collisions and one execution, queued permission revocation, connection loss, registration replacement, stable names, online departure, offline equal-rank protection, target promotion, service revocation, exact capability scope, cancellation, degraded authority, oversized results and safe exceptions. Core checks prove serialization, global/per-actor bounds, pending retry lifetime, retained dedup capacity and continuation after errors.

Actual editor 26.10.13a PID 21804 ran `dev/verify-dispatch-editor.cs`: the public player API invoked a compiled game counter capability, retried once without repeating it, queued mutations, resumed on the engine main thread, denied null callers and self-targeting, revoked a queued service, distinguished queued/active cancellation and removed disposed entry points. Counter mutations: 2. The first script attempt preceded fixture hotload; it was rerun only after successful compilation.

Actual dedicated PID 16676 ran `dev/verify-dispatch-dedicated.cs` with bootstrap editor opt-in off. The engine's separate dedicated-local identity reached the dispatcher, while its local host connection failed the player route. Retry, serial service execution, offline dedicated target protection and safe exceptions passed. Counter mutations: 3. Trusted development reflection reaches the internal local-console boundary because the general native root belongs to T11. The owned process was stopped by exact PID afterward. Neither test performed moderation or changed security policy.

T10 must add durable intent/outcome evidence before this runtime is ready for production mutations. Dedicated-client transport remains unverified for the unpublished adopting game, as recorded in T01; these were host-local dispatcher tests, not dedicated multiplayer certification.
