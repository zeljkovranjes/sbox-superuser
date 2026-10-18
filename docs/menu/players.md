# Players

The **Players** window has a search box, a table of players and, below it, the
selected player.

Search by name or Steam ID and press Enter or **Search**. The table shows each
name, its exact Steam ID and, for staff who can read history, the number of
warnings. Duplicate names stay separate rows. To manage an offline player, search
their full 17-digit Steam ID; it opens them directly.

**Ctrl+F** jumps to the search box. **Up** and **Down** move through the list.

## The selected player

Below the table, the player's online state and roles, then collapsing sections
for active restrictions, actions, notes, recent chat and history.

Enter a reason, or pick one of your recent reasons from **Recent**. Ban and text
mute also need a duration: type one such as `30m`, `2h` or `7d`, or pick a preset
including **permanent**. Action labels are red for kick and ban, amber for warn
and text mute, and green for revocations. A confirmation popup repeats the
action, target, reason and duration; nothing is sent until you press **Confirm**.
Kick needs an online player.

**Add a note** stores a private staff note in the player's moderation history
(`admin.players.note`, granted to moderators by default). The player is not told.

The host checks permission and target protection again when it executes. A
disconnect, revoked permission or storage failure returns an explicit result.
**Unknown** means inspect Activity before retrying; it is not success.

## Right-click

Right-click a player row to copy their Steam ID or name, or to start a kick, text
mute or ban. The cursor jumps to the reason field, and Enter asks you to confirm.
