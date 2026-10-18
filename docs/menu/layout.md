# Layout, pins and pop-ups

The menu uses the stock Dear ImGui dark style. A menu bar runs across the top of
the screen with three menus; open windows are checked.

| Menu | Holds |
|---|---|
| **Superuser** | Refresh, About Superuser, Close |
| **Moderation** | Players, Bans & mutes, Chat |
| **Server** | Commands, Activity, Permissions (owners only) |

Windows can be moved, resized and collapsed. Views refresh every five seconds;
chat refreshes faster.

## Results

The right of the bar shows the latest result: green for success, amber for
Unknown or a conflict, red for errors. Hover it for the full text. Every finished
action from the menu also pops up briefly in the bottom-right corner and fades
out after a few seconds.

## Pinning

Click the pin in a window's title bar to keep that window on screen while the
menu is closed. Pinned windows keep updating but ignore the mouse, so the game
plays normally; open the menu to move, unpin or close them. Closing a window also
unpins it.

## What is saved

Which windows are open or pinned, and where they sit, is saved on the player's
machine in `superuser/menu-layout.json` under the game's data folder, so it
returns after a restart. The last eight reasons and the last duration are saved
the same way, in `superuser/menu-preferences.json`. Both are per machine, not per
server, and a missing or unreadable file falls back to the defaults.

## About Superuser

Shows the version, author, package and source link. Owners also see the modules
installed on the server, the same list as `admin modules list`, and **Server
status**, the same fields as `admin diagnostics`.
