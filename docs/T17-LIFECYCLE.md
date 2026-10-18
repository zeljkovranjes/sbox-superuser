# T17: Lifecycle and support validation

Status: **Complete, with source editing restricted to stopped sessions.** Engine 26.10.13a, Windows, 2026-10-18. Production support remains actual dedicated servers. Editor-host testing requires explicit opt-in. Player-hosted release games and host migration are unsupported.

## Verified lifetimes

| Operation | Actual evidence | Support |
|---|---|---|
| Scene replacement | Dedicated PID 6552, three replacements with stored policy, permanent ban, timed mute, sample listener, native bindings and an active held command | Supported. One fresh runtime; old handles unavailable, subscriptions removed, old module descriptors absent, held operations drained once as Unknown. |
| Managed module disable/enable | Same process, five cycles: five executions, five pulses, one listener each; uncooperative drain bounded at 5.06 seconds | Supported through module management. Previous generation remains quarantined until cleanup completes. |
| Process restart | Dedicated 6552 stopped; fresh PID 19892 | Supported. Exact policy/history payload and original UTC expiry retained; ban and mute still active; temporary modules absent. |
| Editor Play restart | Editor PID 25616, source-test runtime a347255f-c5a6-40a4-87fc-18c47391b711 replaced by fresh Play runtime 90e40ae8-67e7-42db-a4f3-b34366f58143 | Supported. Active ban retained; interrupted command's durable audit outcome Unknown. |
| Structural source replacement while running | Actual library member addition invoked IHotloadManaged; runtime retired, listener count zero, bootstrap toggle could not bypass RestartRequired; slash text synchronously suppressed | Recovery guard only. The engine reported an async delegate substitution error and the retained pending task stayed unresolved. Stop before editing; restart after an accidental live edit. |
| Method-only fast source reload | Actual diagnostic-string edit left the runtime active; IHotloadManaged was not called | Unsupported as a lifecycle boundary. Do not rely on the structural guard to detect every source edit. |
| Release player host / host migration | Eligibility and diagnostics explicitly reject these modes | Unsupported; no migration or lobby portability claim. |

The runtime revokes authority before invoking module cleanup. Its scene coordinator clears the active reference before disposal and prevents reentrant initialization during teardown. In eligible but unavailable sessions, the enabled bootstrap suppresses player chat and sends a private unavailable result. Independent chat integrations must also require `CheckOrdinaryChat(...).Status == Success` before broadcasting ordinary text. A disabled bootstrap cannot enforce chat or admission policy.

## Reproduce

Use the isolated compatibility lab, with its saved bootstrap scene and development fixtures; never use production data. Snapshot its runtime data first. `dev/verify-lifecycle-scenes.cs` seeds disposable policy/moderation and performs three actual `Game.ChangeScene` calls. Run `dev/verify-module-lifecycle.cs` for repeated managed reloads, then stop the exact dedicated PID and launch a fresh process. Run `dev/verify-lifecycle-restart.cs` against the new process.

For the unsupported-source-path probe, start the isolated editor with no owner file, run `dev/setup-source-reload.cs`, and make a controlled structural library edit. `dev/verify-source-reload.cs` verifies retirement, cancellation, removed listener and suppressed command text; it explicitly reports an unresolved retained task rather than claiming success. Stop/start Play and run `dev/verify-source-reload-restart.cs` to verify the ban and durable Unknown audit record. Do not present this as a supported live-edit workflow.

The scripts use trusted local reflection only to inspect internal state and the dedicated actor boundary. No reflection or external process ships in the library. Earlier T11/T16 records separately prove actual native-console entry and admission callbacks. Actual dedicated-client package deployment remains the T01 limitation; these lifecycle checks ran on a real dedicated process and do not substitute for that deployment test.

## Validation and preservation

Main generated runtime build: zero warnings/errors. Existing 230 contract and 75 Core cases pass unchanged. Actual editor compilation applies the engine whitelist; source replacement errors above are runtime hotload limitations, not hidden build failures. Backup: `E:/06-um-data/backups/superuser-t17-lifecycle-data/20261018-020450.zip` (43 baseline files). Task data is preserved separately in the lab's ignored evidence directory after teardown. The user's original editor PID 17136 is untouched.

Implementation: `Systems/SuperuserSession*`, `Engine/Bootstrap/BootstrapRuntime*`, `Components/SuperuserBootstrap.Chat.cs`, `Engine/ChatIntegration/Platform/UnavailableChat.cs`; initial commits `0ba8f80`, `308fb06`, `f76f2a4`, `80c0b3c`, `e4bd88c`. Next: T18, two independent adopting game projects.
