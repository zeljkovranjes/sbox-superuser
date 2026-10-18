# Commands

A command is a method marked with `[AdminCommand]`, discovered when its module
starts. It names one exact permission key, which must be registered by the same
module; registering a key never grants it.

```text
[AdminCommand( Name, Description, Permission, ModuleId, ExecutionPolicy )]
```

`ExecutionPolicy` is `ReadOnly` or `Mutation`. Read-only commands are still
permission-checked and audited; the admin menu asks for confirmation before
running a mutation.

## Arguments

Parameters after `CommandContext` become arguments, in order. Supported types
are strings, numbers, booleans, enums, durations and `PlayerReference`. Mark
constraints with `[AdminArgument]`. The [dog example](../../dev/fixtures/DogCommands.cs)
shows a target and numeric parameters.

A player argument accepts an exact Steam ID or an unambiguous online name. An
ambiguous name is rejected; use a Steam ID to reach an offline player.

## Game services

A command that changes gameplay needs something from your game: a pawn, a roster,
an inventory. Register that as an explicit service with the module, then obtain
it through `context.Services` inside the command. The library never touches your
classes directly. The [arena and colony samples](../../dev/samples/README.md) show
two unrelated games doing this.

## Results

Return `CommandResult` or `Task<CommandResult>`, and honor
`context.Cancellation` in long operations.

| Result | Meaning |
|---|---|
| Success / NoChange | Confirmed completion / nothing needed changing |
| Denied / InvalidArguments / NotFound | Access, input or command lookup failed |
| Unsupported | The runtime or a required capability is unavailable |
| Conflict | A revision, duplicate request or capacity conflict |
| StorageUnavailable | Required storage could not complete |
| Cancelled / Failed | Cancelled before confirmed completion / a safe failure |
| Unknown | The outcome cannot be confirmed; inspect before retrying |

## Retries

A request ID identifies one submission by one actor from one origin. Retry with
the same ID and payload to get the original outcome; a different payload under
that ID is a Conflict. Deduplication lasts five minutes within the running
runtime, not across restarts. Commands are re-authorized when they execute.
