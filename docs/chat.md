# Custom chat

[Guide](README.md) · Previous: [Your first command](commands-tutorial.md) · Next: [Command reference](commands.md)

There are two integration paths. Choose one for each submitted message.

## Your UI uses platform chat

Submit the input through the normal platform API:

```csharp
Sandbox.Platform.Chat.Say( text );
```

Superuser consumes `/admin` commands on the host before broadcast. Ordinary chat continues normally. You do not need another call to `TryHandleChat`.

To hide the built-in overlay while using your UI:

```csharp
ProjectSettings.Platform.ChatShowUI = false;
```

Restore its previous value when your view closes. The [complete chat view](../dev/fixtures/CustomChatExample.razor) demonstrates input, ordinary messages and private results.

## Your UI uses an independent backend

Keep your backend's rate limits and text filtering. In its host RPC, derive the sender from `Rpc.Caller`; never accept a Steam ID or role from the message.

After validating the sender and message, run these checks **before** ordinary channel routing:

```csharp
if ( Superuser.TryHandleChat( sender, text, requestId ) )
{
	return;
}
var decision = Superuser.CheckOrdinaryChat( sender );
if ( decision.Status != CommandStatus.Success )
{
	return;
}
// Now route ordinary chat.
```

`true` means consumed, including denied or malformed commands. It does not mean execution succeeded. Broadcast ordinary text only after `CheckOrdinaryChat` returns Success; an unavailable runtime must not make commands public.

Assign one nonempty request ID per submission. Reuse it when retrying that submission. Do not also send the message through `Chat.Say`.

The working backend consists of [IndependentChatExample.cs](../dev/fixtures/IndependentChatExample.cs), [ExampleChatGate.cs](../dev/fixtures/ExampleChatGate.cs) and [IExampleChatMessageEvent.cs](../dev/fixtures/IExampleChatMessageEvent.cs). Mount one networked backend object and select `IndependentBackend` on the example view. Its sender comes from the engine, replies are host-only, and ordinary messages retain Steam filtering.

## Render private results

Implement `IAdminChatResultEvent` on your local scene component or panel. The example view's `OnAdminChatResult` adds `message.Result.Message` and result data to its local lines, then sets `message.SuppressDefaultDisplay = true`.

Set that flag only after displaying the result. Leave it false to keep the default notification. Do not replicate the result, send it through `Chat.Say`, or parse it as another command. Disabled or destroyed scene components stop receiving the event.

## Common questions

**Do commands start with an exclamation mark?** No. Use `/admin`. `!` is ordinary text.

**What if I change Command Root?** Chat uses `/<root>`. For native console use `superuser <root> <command>`.

**Can muted players still use commands?** Yes. Mute suppresses ordinary integrated text chat. Commands still pass through permission checks.

**Do I need the demo Razor panel?** No. Your UI can implement the same hooks, including a future ImGui view.
