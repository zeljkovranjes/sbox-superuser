# T14: Custom chat integration

Status: **Complete.** Custom UI over platform chat uses `Sandbox.Platform.Chat.Say`; an independent backend authenticates its RPC sender and calls the library before routing or broadcasting. Neither path introduces another executable, service or required UI library.

## Custom UI over platform chat

Keep the bootstrap's platform-chat listener enabled. Send entered text through `Chat.Say` exactly once. Do not also call `TryHandleChat` for that submission. Implement `IAdminChatResultEvent` on a client scene component to receive the host's private `AdminChatResultEvent`. Render its `Result`, then set `SuppressDefaultDisplay = true`. The event is local to the addressed client, not replicated state. Disabled and removed components do not receive it; no static subscription is required.

With the normal platform overlay enabled, unsuppressed results use `Chat.AddText`. A failed observer cannot suppress that fallback merely by setting the flag before throwing. A custom UI that hides the platform overlay is responsible for showing its own messages. Do not render private result data into shared or synchronized properties. Retry responses retain their request ID, so a UI may coalesce them.

## Independent backend

Use a host RPC and obtain the sender from `Rpc.Caller`. Validate the active connection, text bounds and rate limits. Call `Superuser.TryHandleChat( sender, text, requestId )` once, before any routing or broadcast. A `true` return means consumed, including invalid, denied and unknown commands; the library sends the private result. Preserve the request ID when retrying a submission. Generate a new ID for each new submission.

If the hook returns false, call `Superuser.CheckOrdinaryChat( sender )` before sending ordinary text. Route only on Success and return failures privately. At T14 this checks authenticated identity and runtime policy health; text-mute enforcement is a T16 dependency and is not implemented yet. Client-local hook calls cannot execute a command: consumption returns false and ordinary checks return Unsupported. Never accept a role, Steam ID or target connection supplied in the RPC payload as the caller identity.

The runnable `IndependentChatExample` applies a 512-character limit, disallows control characters, limits each connection to one submission per 0.75 seconds, and bounds its ingress table to 128 connections. Its host-only ordinary-message RPC preserves recipient filtering for failures; clients apply Steam name/chat filtering. These are example game transport choices, not a second required service.

## Runnable examples

Copy the files listed in [fixture setup](../dev/fixtures/README.md#task-14-custom-chat-fixtures) into an isolated adopting game. `CustomChatExample.razor` displays either platform chat or the independent backend; its `IndependentBackend` property switches the submission path. It is a small demonstration chatbox, not the deferred ImGui admin UI. The library itself has no renderer dependency.

Use an editor host with Allow Editor Testing enabled and two real remote clients. Run `setup-custom-chat-host.cs` on the host and `setup-custom-chat-client.cs` on each client. Record their object IDs and previous overlay settings for cleanup. Run `verify-custom-chat-client.cs` on client A. On B verify one ordinary message and no private results, then enable `IndependentBackend`. Submit ordinary text `T14 independent ordinary` on A and run `verify-custom-chat-peer.cs` on B. Run `verify-custom-chat-host.cs` on the host and `verify-custom-chat-observer.cs` on A. Inspect A's console for the expected observer warning and fallback result. Stop the disposable play session, or destroy the recorded runtime objects, restore the overlay settings, and disable the test bootstrap afterward. No saved scene changes are required.

`ChatExampleModule` intentionally permits the test counter through the default help permission. Never install this diagnostic module or the deliberately failing observer in production.

## Actual engine evidence (2026-10-18)

The isolated editor host (PID 25616) and two remote game clients (24908, 23656) compiled the library and adopting-game examples in the engine. Client A received four private results: platform counter execution 1, independent counter execution 2, the retried result still reporting execution 2, and a Denied policy command. Host audit contained exactly two counter executions, both Chat origin and A's authenticated player identity. B received only the two ordinary messages, with zero private results. Screenshots of both rendered chatboxes were inspected.

Disabling A's view prevented delivery to it. An active observer received one version result, set suppression and threw; the default platform notification still appeared and only the exception type was logged. No owners were added and no policy grants were changed. Actual dedicated-client transport remains uncertified because the adopting game is unpublished; these checks are editor-host multiplayer evidence.


Final checks: generated library and adopting-game builds passed with zero warnings/errors, and actual engine compilation reported zero diagnostics for both packages. All 207 contract and 61 Core tests passed unchanged. Both package standards checks passed. The lab test project contains no cases. Pre-release lint reported zero failures and two ignored-path warnings (local journal and generated engine XML). Bootstrap is disabled with editor opt-in reset false, local overlay settings restored true, and owned runtime chat objects removed; saved scene unchanged. No publication occurred.
