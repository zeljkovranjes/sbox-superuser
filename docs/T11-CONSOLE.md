# T11: Native console and operator commands

Status: **Complete.** Native console entry, authenticated player forwarding, private replies, owner-only access commands, diagnostics and descriptor help use T09 dispatch and T10 audit. Next: T12 module lifecycle.

Engine inspection on 26.10.13a confirms ConsoleSystem exposes Run/GetValue/SetValue, while Command, ManagedCommand and ConVarSystem are internal. Runtime native-name registration is unavailable through the public API. Use the T01 fallback: fixed native transport with a configurable logical root. The default native `admin` convenience entry can be used when no visible managed-name collision exists; a namespaced transport remains available for another configured root. Report collisions instead of overwriting commands dynamically. Native engine enumeration and autocomplete are not promised.

Current goal continues through T20 in dependency order and prepares an unpublished release candidate. `/` remains the chat prefix; visual UI stays deferred.

## Operator setup

Add one bootstrap to the game's active network scene. Use an actual dedicated server in production, or enable Allow Editor Testing in the isolated editor. Configure explicit owner IDs in the correct host-local owner file as described in T03; no native role is imported. Run `admin help`, then `admin help group.create`. Help works without autocomplete and filters commands by current permission and owner-only restrictions.

The default native `admin` entry is non-forwarded. On a dedicated host it authenticates the dedicated-local path; on a client it sends bounded engine tokens through the fixed Server transport `superuser_request`, where the engine injects the real Connection. Null or SteamId 0 on that player route is denied, including the dedicated host connection. Requests never accept caller-supplied roles. Replies use a HostOnly RPC filtered to the exact requesting connection; the receiving side checks the host sender. Dedicated-local results print only in the local server console.

Change Command Root on bootstrap and re-enable it to configure another logical name. For `operators`, use `superuser operators help`. Do not expect a dynamically created native `operators` alias. The same token router also accepts canonical dotted keys, for example `admin user.group.add <steamid> moderator`. Native argument arrays retain empty strings and quoted values; the library never joins them into executable console text. Every submission gets a new ID; the fixed transport carries the ID for authenticated retries.

## Commands

| Console form | Access and behavior |
|---|---|
| `admin help [command-or-*] [page]` | Permitted descriptors, syntax, examples, constraints and permission. Eight rows per page. |
| `admin commands [page]` / `admin version` | Public discovery permission. |
| `admin diagnostics` / `admin modules list [page]` | Current owners or dedicated console. Module list describes registered command modules. |
| `admin group create <group>` / `admin group delete <group>` | Owner-only; reserved owner and referenced-group protections remain enforced. |
| `admin group list [page]` | Owner-only group, parent and protection summary. |
| `admin group <group> parents set <comma-separated-parents>` | Owner-only replacement; `""` clears parents. Cycles reject atomically. |
| `admin group <group> parents list [page]` | Owner-only parent inspection. |
| `admin group <group> protection set <level>` | Owner-only nonnegative integer protection. |
| `admin user <steamid-or-name> group add/remove <group>` | Owner-only membership changes, with offline IDs supported. |
| `admin user <steamid-or-name> group list [page]` | Owner-only direct and implicit player membership. |
| `admin user <steamid-or-name> permission allow/deny/unset <key>` | Owner-only direct permission change. |
| `admin group <group> permission allow/deny/unset <key>` | Owner-only group permission change. |
| `admin permissions explain <steamid-or-name> <key> [page]` | Own explanation is available; another subject requires admin.permissions.explain. |
| `admin audit query [command-or-*] [revision:sequence]` | Requires admin.audit.read; five private rows per page. |
| `admin results [command-or-*] [revision:sequence]` | Requires admin.results.own; returns only the caller's Player rows. |

Use `admin help * 2` for the second help page. Audit cursors are revision-bound: writes invalidate them, but the two built-in read-only audit queries do not write audit events themselves. No public registration API can opt a mutating handler out of auditing. Existing dedicated-local owner recovery and store recovery entries remain available separately; they are never exposed through the player route.

When auditing fails, dispatched commands fail closed. Use non-forwarded `superuser_status`, `superuser_authority_status`, `superuser_audit_status`, and the T04 dedicated-local backup/recovery procedure. These host-local diagnostics report health without row contents and remain usable without writing a command intent.

## Collision and reload limits

Bootstrap checks visible TypeLibrary ConVar/ConCmd metadata, including case-insensitive duplicate root and fixed-transport names. A collision or invalid root reports ConsoleUnavailable and refuses framework ingress at that root. Native attribute registration is engine-owned and precedes this check; the library cannot undo the engine's choice between conflicting native attributes or enumerate every native engine entry. Avoid conflicting names in the adopting game and use the documented configured-root fallback. Static native entry changes require bootstrap restart; no unsupported dynamic registration or autocomplete hook is used.

## Verification

230 tests passed: 186 contract and 44 Core cases. The 212 pre-T11 cases remain unchanged. New checks cover routing without retokenization, exact argument preservation, root validation/collisions, filtered help, constraints/examples, owner-only policy commands despite a direct management grant, reserved ownership, own/other explain, distinct audit actors and stable audit pagination.

Actual engine 26.10.13a / Steam build 25964806: editor 25616 and dedicated 2448 ran `dev/verify-native-console.cs`. Native help, create/delete, diagnostics, configured `operators` root and collision with `superuser_probe` passed. Editor mutations recorded Player `76561199542962865`; dedicated mutations recorded DedicatedConsole `dedicated-console`. Dedicated forwarded transport request `99949b51-55cf-4a48-bcd1-c99207182059` was denied for its SteamId 0 caller, while the dedicated-local entry succeeded. Native audit health printed separately.

Two actual editor clients 15448 and 26504 used engine local-instance identities `90071996842377217` and `90071996842377218`. `verify-console-client.cs` submitted help twice with request `470043b7-4403-4210-b822-fc6e831a88b8`; `verify-console-host.cs` observed one successful row attributed to client A. Request `e92a93a9-2f07-4c7d-9e83-afe7d262d96c` attempted reserved owner membership and was Denied. Client A alone received help and denial replies; peer and host logs contained neither request's private response. A client-origin HostOnly reply-wrapper invocation produced no forged reply in either client or host.

After explicit host-local owner configuration for A, its real `admin group create t11remote` persisted successfully. B's delete was denied and did not receive A's success. A then removed the test group; stored policy revision 4 contained only default groups and no users. This verifies actual editor network identity/privacy, not unpublished dedicated-client deployment. T01's dedicated-client limitation remains recorded.

Pre-test backup: `E:/06-um-data/backups/superuser-t11-t20-lab-data/20261018-000227.zip` (43 files). Runtime evidence is in ignored `dev/out/t11-*`. Trusted development reflection only inspects internal test state; no unrestricted reflection ships in the library.

Final runtime builds and both package/Core checks passed; actual editor whitelist compilation reported zero errors and warnings. Publication lint found no failures (two ignored local-path warnings). Temporary owner configuration was removed and new security/audit test data archived under the lab's `dev/out/t11-native-evidence`; the dedicated test process was stopped by exact PID. No saved scene changes or publication were made.
