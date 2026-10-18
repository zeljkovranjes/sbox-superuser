# T15: Explicit native bindings

Status: **Implemented; final checks in progress.** One optional internal module supplies explicit host bindings. No arbitrary console execution endpoint or external runtime is introduced.

## Game setup

After bootstrap is ready, trusted host setup creates one module per runtime:

```csharp
var settings = Superuser.CreateNativeBindingsModule( new[]
{
	new NativeSettingDescriptor( "game.capacity", "my_game_capacity", "Game capacity", "game.settings.capacity",
		CommandArgumentKind.Integer, "8", NativePersistence.Framework, minimum: 1, maximum: 32 )
} );
var result = await Superuser.RegisterModulesAsync( new[] { settings }, cancellation );
```

The adopting game must actually declare `my_game_capacity` with `[ConVar]`; this does not create a native variable automatically. The compiled [NativeBindingExample](../dev/fixtures/NativeBindingExample.cs) supplies real variables and a command. Copy it into the test game's `Engine/Settings/` and use `CreateModule()`. Diagnostic variables and test code must not ship in production.

Only one `superuser.settings` module may be installed per runtime, with up to 64 settings and 64 commands. Keys and native names must be unique. Recreate it for each new bootstrap runtime. Module disable removes entry points and retains stored state. Enabling the same module again does not apply restart-only values.

## Operator commands

| Command | Behavior |
|---|---|
| `admin settings list [page]` | Eight permission-filtered descriptions per page; no values. |
| `admin settings get game.capacity` | Reads actual native state and reports desired, pending, restart and persistence separately. |
| `admin settings set game.capacity 12` | Validates the typed value, native access and readback before confirming. |
| `admin settings reset game.capacity` | Applies the explicitly registered default through the same checks. |
| `admin help settings.set.game.capacity` | Shows typed constraints and required permission. |
| `admin native.example.increment "test text"` | Submits an allowlisted native command with separately quoted arguments. |

Direct dispatcher keys are `settings.get.<key>`, `settings.set.<key>` and `settings.reset.<key>`. Routing selects the binding's descriptor before execution, using its permission and audit metadata. Chat uses `/admin ...`. Sensitive settings refuse chat even for owners, and reads redact values. Sensitive native command arguments are Unsupported pending an independently verified game adapter. Optional native arguments, durations and player targets are not supported by this scalar adapter.

## Native restrictions

Only uniquely discoverable managed ConVar/ConCmd metadata is supported. Unknown or inaccessible entries return Unsupported. Protected, Admin and Cheat flags are refused, including for owners: executing on the host must not promote players into native engine authority. Scope is explicitly Host. Sensitive variables with Replicated, UserInfo or ChangeNotice flags are refused.

All access uses public `ConsoleSystem` methods. No native fields or internal console registry are written, and no broad engine-admin claims are granted. Original entry points retain their engine behavior; wrapping one does not restrict direct console invocation. Actual native values outside framework write bounds remain inspectable and resettable if their scalar type is valid.

`ConsoleSystem.Run` returns void on engine 26.10.13a, without supported acknowledgment. The wrapper reports Unknown after submission, even when the example verifies an effect. Repeating the same framework request ID does not submit it again. Inspect the effect before sending a new request. Arbitrary built-ins, map changes and shutdown are not certified bindings.

## Persistence and failures

Session bindings require non-Saved variables and store no desired state. Engine bindings require Saved metadata and leave persistence to the engine; they create no framework setting record. Framework bindings require non-Saved variables and use versioned `superuser.settings/<binding-key>` records, isolated into editor/dedicated roots. Records validate schema, module, native name, scalar kind, restart contract and desired value. A changed or corrupt contract requires explicit recovery; it never resets silently.

Immediate writes capture the actual value, apply and compare canonical readback. Mismatch attempts rollback and does not save desired state. A failed pre-commit storage write attempts native rollback; unconfirmed persistence or rollback returns Unknown. The dispatcher saves its audit intent before the native action; stored setting transactions include a correlated intent. Partial files remain available for recovery. Native writes and filesystem writes cannot be one atomic engine transaction; interruption is reported honestly.

Restart-only bindings require framework persistence. Set/reset saves desired state without changing active state. The first module startup in a new runtime reapplies it through native access/readback checks. Pending means desired differs from active. Failed reapplication retains stored evidence and emits a warning; get shows actual state. Defaults are explicit, never inferred.

## Verification on 2026-10-18

Engine 26.10.13a: editor host 25616 and actual dedicated server 23416 ran `verify-native-bindings.cs`, `verify-native-binding-failure.cs` and `verify-native-setting-lifetime.cs`. These use actual engine code and storage, not Sandbox mocks. They passed typed validation, reset, Protected/Admin/Cheat/missing rejection, native clamp rollback, engine-owned storage absence, sensitive redaction, capability denial and filtered listing. Injecting failure into a real setting generation write restored active value 12 and preserved the prior committed revision.

The dedicated console's `admin settings set` produced a Success audit record attributed to DedicatedConsole. A quoted argument containing a semicolon stayed one argument; retrying its request ID produced exactly one native counter increment and Unknown status. Pending value 6 stayed pending while active value 2 survived module disable/enable, with no duplicate descriptors. An editor owner was denied a sensitive chat write; its temporary owner file was restored.

After stopping only the owned dedicated process and restarting it as PID 11412, `verify-native-setting-restart.cs` observed native default 2 before module registration and persisted value 6 afterward. This is actual process-restart evidence. Reproduction order: run the three main scripts, restart the isolated dedicated fixture, then run the restart script. Preserve/backup test data first and restore native values and bootstrap settings afterward. No saved scene changes are required.

Dedicated-client transport remains uncertified because the adopting game is unpublished. Source hot reload and broader lifecycle checks remain T17. Player-hosted release games and host migration remain unsupported. No publication occurred.
