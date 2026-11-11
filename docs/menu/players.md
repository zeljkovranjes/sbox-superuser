# Players

The **Players** window has a search box and a table of players. Click a player
to open them in the **Player** window.

Search by name or Steam ID and press Enter or **Search**. The table shows each
name, its exact Steam ID and, for staff who can read history, the number of
warnings. Duplicate names stay separate rows. To manage an offline player, search
their full 17-digit Steam ID; it opens them directly.

**Ctrl+F** jumps to the search box. **Up** and **Down** move through the list.

## The Player window

The top shows the player's name, Steam ID, online state and roles. Click the
Steam ID to open their Steam profile, or right-click it to copy it. **Actions**
lists what you may do to this player. Below are tabs for active
**Restrictions**, moderation **History**, recent **Chat**, **Notes** and
**Prefix**.

**By** shows who recorded each restriction and history entry, including unbans
and unmutes. Right-click a staff member for **Open...**, which shows them in the
**Player** window, **Open Steam...** and **Copy Steam ID**. The console and host
services appear as plain text.

Choosing an action opens a popup. Type a reason, or pick one of your recent
reasons from **Recent**. Ban, mute and gag also need a duration: type one such as
`30m`, `2h` or `7d`, or click a preset including **permanent**. Action labels are
red for kick and ban, amber for warn, mute and gag, and green for revocations.
Nothing is sent until you press the action's button or Enter. The popup says so
when you may not take that action against this player. Kick needs an online
player.

**Notes** stores a private staff note in the player's moderation history
(`admin.players.note`, granted to moderators by default). The player is not told.

The host checks permission and target protection again when it executes. A
disconnect, revoked permission or storage failure returns an explicit result.
**Unknown** means inspect Activity before retrying; it is not success.

## Right-click

Right-click a player row to copy their Steam ID or name, or to kick, warn, text
mute, gag or ban them. Each opens the same popup as **Actions**.
