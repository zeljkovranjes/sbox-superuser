# Replacing the menu

Disable or remove **Superuser Admin Menu**. This releases its input panel and
cursor and stops its queries. Console, slash chat and the APIs below stay
available. ImGui is bundled internally; your interface needs neither its types nor
another package.

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

Or run any registered command the way the Commands window does:

```csharp
var requestId = Superuser.RequestAdminCommand(
	new AdminCommandRequest( "note", new[] { "76561198000000044", "Seen near spawn" } ) );
```

`AdminQueryKind.Commands`, `Restrictions`, `Chat`, `Permissions` and `About`
return the data behind the other windows; `new AdminQuery( command, actor,
outcome, page )` filters activity.

Implement `IAdminResultEvent` on an enabled scene component and match
`reply.RequestId` to the returned ID. Replies arrive on a later update. A timeout
or connection change reports Unknown; nothing is retried automatically.

Copy [ReplacementAdminExample.cs](../../dev/fixtures/ReplacementAdminExample.cs)
into your game's `Code/<YourRoot>/Components/`, change its namespace, and attach
it once. Connect your widgets to its methods and `LastReply`. It demonstrates
search, exact-ID inspection, activity and confirmed actions.
