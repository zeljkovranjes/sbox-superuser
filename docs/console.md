# Reference: console commands

Use these forms in the console. In the chatbox, add `/`: `admin help` becomes
`/admin help`. Replace angle-bracket values; square brackets indicate optional
arguments.

## Discovery

| Command | Purpose |
|---|---|
| `admin help [command-or-*] [page]` | Permitted commands or detailed syntax |
| `admin commands [page]` | Command names |
| `admin version` | API version |
| `admin diagnostics` | Owner-only health |
| `admin modules list [page]` | Owner-only module state |
| `admin modules enable/disable <id>` | Owner-only lifecycle control |
| `admin results [command-or-*] [cursor]` | Your own recorded results |
| `admin audit query [command-or-*] [cursor]` | Audit pages; requires `admin.audit.read` |

Use `admin help * 2` for another help page. Audit cursors come from the previous
response and expire when the journal changes.

## Access management

Only owners and the dedicated local console can edit policy.

| Command | Purpose |
|---|---|
| `admin group create/delete <group>` | Create or remove a group |
| `admin group list [page]` | Groups and protection levels |
| `admin group <group> parents set <comma-separated-parents>` | Replace inheritance; `""` clears it |
| `admin group <group> parents list [page]` | Inspect inheritance |
| `admin group <group> protection set <level>` | Set a rank from 0 through 99 |
| `admin group <group> permission allow/deny/unset <key>` | Group permission entry |
| `admin user <user> group add/remove <group>` | Membership |
| `admin user <user> group list [page]` | Membership summary |
| `admin user <user> permission allow/deny/unset <key>` | Direct permission entry |
| `admin permissions explain <user> <key> [page]` | Why a permission allows or denies |

Other-user explanations require `admin.permissions.explain`. `<user>` accepts an
exact Steam ID or an unambiguous online name.

## Moderation

| Command | Permission |
|---|---|
| `admin kick <user> <reason>` | `admin.players.kick` |
| `admin ban <user> <duration> <reason>` | `admin.players.ban` |
| `admin unban <user> [reason]` | `admin.players.unban` |
| `admin warn <user> <reason>` | `admin.players.warn` |
| `admin mute <user> <duration> <reason>` | `admin.players.mute` |
| `admin unmute <user> [reason]` | `admin.players.unmute` |
| `admin note <user> <text>` | `admin.players.note` |
| `admin moderation history <user> [page]` | `admin.players.history` |

Quote reasons containing spaces. Durations are explicit: `30m`, `2h`, `7d` or
`permanent`. Kick needs an online player; the other operations support offline
Steam IDs.

Bans persist before disconnect. Expiry retains its original UTC timestamp after
restart. Repeated revocation returns NoChange when nothing is active. Warnings
do not escalate automatically. Text mute covers integrated chat, not voice or
unrelated channels.

## Prefixes

Reading needs nothing; changing needs `admin.prefixes.manage`.

| Command | Purpose |
|---|---|
| `admin prefix list [page]` | Every prefix with its tag and effects |
| `admin prefix create <id> [tag]` | A new prefix |
| `admin prefix update <id> <field> <value>` | `name`, `tag`, `template`, `palette`, `tag-effect` or `name-effect` |
| `admin prefix delete <id>` | Delete it and its assignments |
| `admin prefix group set/clear <group> [id]` | A group's prefix; `owner` means every owner |
| `admin prefix user set/clear <user> [id]` | A player's own prefix |
| `admin prefix export` | The catalog as JSON |

See ***[Prefixes](prefixes.md)*** for effects and examples.

## Settings

After the game registers [native bindings](reference/settings.md), use `admin
settings list`, `admin settings get <key>`, `admin settings set <key> <value>`
and `admin settings reset <key>`. Each binding has an explicit permission.

For health when normal dispatch cannot write audit records, use local
`superuser_status`, `superuser_authority_status` and `superuser_audit_status`.
Owner and storage recovery are [dedicated-local
operations](reference/recovery.md), not chat commands.
