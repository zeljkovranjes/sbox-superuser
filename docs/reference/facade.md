# The Superuser facade

Import `SboxSuperuser`. Host APIs run on the engine main thread after the
bootstrap is ready. Client code submits through the console, chat, the admin
menu, or an authenticated game RPC.

## Host entry points

| API | Use |
|---|---|
| `Superuser.GetDiagnostics()` | Bootstrap state and runtime ID |
| `TryGetPlayerIdentity( connection, out actor )` | Resolve a live engine player |
| `IsOwner( connection )` | Check explicit ownership |
| `ExecuteCommandAsync( connection, request, origin )` | Dispatch authenticated player input |
| `ExplainPermissionAsync( connection, steamId, key )` | Explain an access decision |
| `CheckPunitiveTargetAsync( connection, steamId )` | Compare target protection |
| `ApplyPolicyChangeAsync( connection, change, origin )` | Owner-only policy change |
| `RegisterModulesAsync( modules )` | Install a managed module catalog |
| `TryHandleChat( sender, text, requestId )` | Consume a custom-chat command |
| `CheckOrdinaryChat( sender )` | Authorize ordinary chat before broadcast |
| `GetStore()` | Host-local versioned storage |
| `ConfigureStorage( factory )` | Select a storage backend before enabling the bootstrap |
| `QueryAuditAsync( connection, query )` | Permission-filtered audit pages |

## Client entry points

These are what the admin menu itself uses; a replacement interface uses them
too. See [replacing the menu](../menu/replace.md).

| API | Use |
|---|---|
| `RequestAdminQuery( query )` | A private view: players, a player, activity, restrictions, chat, commands, permissions, about |
| `RequestAdminAction( request )` | An exact-target moderation action |
| `RequestAdminCommand( request )` | Any registered command the caller may run |
| `IAdminResultEvent` | Receives correlated private replies |

## Keep these away from players

`GetStore`, `ConfigureStorage`, `CreateCommandService` and module registration
are trusted host-code facilities. Never expose them directly to player input.
For a player's request, use the live engine connection, never an actor assembled
from message fields.
