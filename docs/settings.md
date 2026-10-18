# Native settings

[Guide](README.md) · Previous: [API and modules](api.md) · Next: [Storage and recovery](recovery.md)

Superuser exposes only native entries explicitly registered by the game. It does not provide arbitrary console execution.

Use `Superuser.CreateNativeBindingsModule` with `NativeSettingDescriptor` and optional `NativeCommandBindingDescriptor` values. Install the returned module alongside your other modules. The compiled [NativeBindingExample.cs](../dev/fixtures/NativeBindingExample.cs) shows integer constraints, restart-only values and permissions.

## Persistence

| Mode | Owner |
|---|---|
| Session | Current process only |
| Engine | The engine saves the native value |
| Framework | Superuser stores the desired value and applies it on startup |

A native `Saved` ConVar must stay engine-owned. Restart-only bindings keep active and pending values distinct. Writes validate, read back and confirm the native value before storing the desired state; failed readback attempts rollback.

Protected, Admin, Cheat, missing or inaccessible entries remain unavailable. Sensitive values are excluded from chat and audit arguments. Do not use framework permissions to bypass native checks.

Native commands receive typed argument arrays, never concatenated executable strings. The engine's `ConsoleSystem.Run` provides no completion acknowledgment, so a submitted native command reports Unknown rather than inventing success.
