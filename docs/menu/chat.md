# Chat

Shows the last 100 public chat messages, newest first, so staff can see what was
said before acting. Filter by name, Steam ID or text. Click a line to open the
player, or right-click it to copy it or start a text mute. A player's own lines
also appear under **Recent chat** in their details.

## Privacy

- The host keeps only messages that were delivered to everyone.
- Superuser commands, and messages blocked by a mute, are never kept.
- Lines live in memory only. Nothing is written to disk, and the buffer is
  cleared when the host runtime stops.
- Reading it needs `admin.chat.read`, granted to moderators by default on new
  installs. Existing servers keep their stored policy and grant it explicitly.
