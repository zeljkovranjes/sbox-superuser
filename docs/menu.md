# Reference: the admin menu

Superuser ships an optional in-game moderation menu built on a bundled Dear ImGui
port. It is a client of the same host API your own code uses: every view is a
private query, every change an authorized and audited command.

Add one **Superuser Admin Menu** to a GameObject that exists on each client; it
needs a scene camera. Press **F6** to open or close it. Escape cancels a pending
confirmation first, otherwise it closes the menu. Set **Open Key** to another key
and re-enable the component to apply it, or call `menu.SetOpen( true )` from a
game button. The host still needs **Superuser Bootstrap**, and opening the menu
grants no permissions.

- ***[Layout, pins and pop-ups](menu/layout.md)*** &mdash; the menu bar, moving
  and pinning windows, what is saved on the player's machine, result pop-ups.

- ***[Players](menu/players.md)*** &mdash; search, the selected player, actions,
  notes, recent reasons and keyboard shortcuts.

- ***[Bans & mutes](menu/bans.md)*** &mdash; every active restriction across all
  players, with time left and revoke buttons.

- ***[Chat](menu/chat.md)*** &mdash; recent public chat for moderators, and its
  privacy rule.

- ***[Commands](menu/commands.md)*** &mdash; running any permitted command,
  including your game's own, from a generated form.

- ***[Activity](menu/activity.md)*** &mdash; the audit log, filtered and paged.

- ***[Permissions](menu/permissions.md)*** &mdash; owners editing groups,
  members and permission entries.

- ***[Replacing the menu](menu/replace.md)*** &mdash; building your own interface
  on the same private API.

## Who sees what

| View | Permission |
|---|---|
| Player list and roles | Any built-in moderation action, or `admin.players.history` |
| Ban details | `admin.players.ban`, `admin.players.unban` or history |
| Text restriction details | `admin.players.mute`, `admin.players.unmute` or history |
| Moderation history, warning counts | `admin.players.history` |
| Bans & mutes | Same as ban and text restriction details |
| Chat | `admin.chat.read` |
| All recent activity | `admin.audit.read` |
| Own recent activity only | `admin.results.own` |
| Commands | Each command's own permission |
| Permissions, installed modules, server status | Owners |

Replies go privately to the requesting connection. History pages hold 20
records. Kick history is an intent; use the correlated activity outcome to
confirm it executed.
