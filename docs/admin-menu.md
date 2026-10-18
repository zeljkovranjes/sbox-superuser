# Admin menu

[Guide](README.md) · [Setup](setup.md) · [Custom storage](storage-tutorial.md)

Add one **Superuser Admin Menu** to a GameObject that exists on each client. It needs a scene camera. Press **F6** to open or close it; Escape also closes it. Set **Open Key** to another keyboard key and re-enable the component to apply it. A game button can call `menu.SetOpen(true)`.

The host still needs **Superuser Bootstrap**. Production runs on an actual dedicated server; local testing requires **Allow Editor Testing** on the editor host. Opening the menu grants no permissions.

## Layout

The header shows connection state, **Refresh** and **Close**. Below it, a colored strip reports the last action result: green for success, amber for Unknown or a conflict, red for errors. Use the **Players** and **Activity** tabs to move between views. Views refresh every 5 seconds.

## Moderate a player

In the **Players** tab, search the left column by name or Steam ID; press Enter or **Search**. Each row shows the name and its exact Steam ID. Duplicate names remain separate rows. To manage an offline ban, enter its Steam ID under **Offline lookup**.

The right column shows the selected player's online state, roles, visible active restrictions, permitted actions and history. Enter a reason. Ban and text mute also need a duration: type one such as `30m`, `2h` or `7d`, or pick a preset including **Permanent**. Action buttons are red for kick and ban, amber for warn and text mute, and green for revocations. A confirmation panel then repeats the action, target, reason and duration; inputs stay locked until you confirm or cancel. Kick requires an online player.

The host rechecks permission and target protection when executing. A disconnect, revoked permission or storage failure returns an explicit result. **Unknown** means inspect recent activity before retrying; it is not success.

## Who sees what

| View | Permission |
|---|---|
| Player list and roles | Any built-in moderation action, or `admin.players.history` |
| Ban details | `admin.players.ban`, `admin.players.unban` or history |
| Text restriction details | `admin.players.mute`, `admin.players.unmute` or history |
| Moderation history | `admin.players.history` |
| All recent activity | `admin.audit.read` |
| Own recent activity only | `admin.results.own` |

Replies go privately to the requesting connection. History pages contain 20 records; recent activity shows the latest 20 visible records. Kick history is an intent—use the correlated activity outcome to confirm execution.

## Replace the menu

Disable or remove **Superuser Admin Menu**. This releases its input panel and cursor and stops its queries. Console, slash chat and the APIs below stay available. ImGui is bundled internally; your UI does not need its types or another package.

On a connected client, submit a query:

```csharp
var requestId = Superuser.RequestAdminQuery(
	new AdminQuery( AdminQueryKind.Players, "alex" ) );
```

After your own confirmation dialog, submit an exact-target action:

```csharp
var requestId = Superuser.RequestAdminAction(
	new AdminActionRequest( AdminAction.Ban, "76561198000000044",
		"Repeated disruption", "2h" ) );
```

Implement `IAdminResultEvent` on an enabled scene component and match `reply.RequestId` to the returned ID. Replies arrive on a later update. A timeout or connection change reports Unknown; there is no automatic mutation retry.

Copy [ReplacementAdminExample.cs](../dev/fixtures/ReplacementAdminExample.cs) into your game's `Code/<YourRoot>/Components/`, change its namespace, and attach it once. Connect your widgets to its methods and `LastReply`. It demonstrates search, exact-ID inspection, activity and confirmed actions.
