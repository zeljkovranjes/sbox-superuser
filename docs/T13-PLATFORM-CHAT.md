# T13: Platform chat commands

Status: **Complete.** The user-selected prefix is `/`, replacing the specification's illustrative `!` prefix. The bootstrap command root determines the complete chat prefix: `/admin` by default, or `/operators` when Command Root is `operators`.

Only an exact prefix followed by whitespace or the end of the message is owned. Matching is case-sensitive and does not trim leading text. Malformed, unknown and denied admin commands are consumed and answered privately. Ordinary chat, `!admin`, `/administrator`, system messages and already-suppressed events remain outside command dispatch.

## Setup and use

1. Enable platform chat in the adopting game's settings and keep one Superuser Bootstrap in its active network scene.
2. Use an actual dedicated server, or opt into Allow Editor Testing. After changing that property or Command Root, disable and re-enable bootstrap to apply it. Confirm `superuser_status` reports a runtime ID; OwnersMissing still permits default player help.
3. Enter `/admin help`, `/admin help version`, or `/admin version` in the in-game chatbox. Owners may use the same authorized subcommands as native console, for example `/admin group create helpers`.
4. To change the prefix, set Command Root to `operators` and re-enable bootstrap. Chat then uses `/operators`; native console uses `superuser operators <command>` as documented in T11. No short aliases or `!` prefix are installed.

The library supplies no new chat UI. The built-in platform overlay displays private `Chat.AddText` notifications on the requesting client. Structured fields travel in a bounded host-only RPC and the default renderer presents descriptor rows and page numbers as readable text. Replies never use `Chat.Say`, so system notifications cannot recurse into command parsing. Sensitive arguments remain refused for chat by the shared dispatcher.

## Execution and lifecycle

The bootstrap component implements one command listener. It resolves the scene-owned runtime and ignores client-side events, missing system senders and events already suppressed by another listener. For an owned message it sets `Suppress` synchronously, then starts asynchronous dispatch using the engine-provided connection. The callback carries no client-authored role or actor kind. Duplicate calls with the same event object see Suppress and cannot submit again.

Each platform submission receives a fresh request ID. Separate identical messages are separate submissions; the dispatcher still deduplicates retries carrying the same actor, origin and request ID. There is no text-based deduplication. Engine chat rate limits remain in effect, alongside dispatcher quotas. No ordering is promised relative to another game's independent listener; the T14 examples will define a single ingress path for custom backends.

Only the requesting active connection is included in the reply RPC. The receiver requires an authenticated host caller and rejects invalid status, request ID, duplicate fields and oversized data. Replies allow at most 1,024 message characters, 16 fields, 64 characters per key, 1,024 per value and 8,192 total field characters. Ordinary command results and malformed-input audit records never copy raw command text into host diagnostics.

When bootstrap is disabled or ineligible, there is no active admin chat adapter. Test the editor switch and runtime diagnostic before submitting commands: a disabled adapter leaves chat to the game. Duplicate bootstrap configuration is rejected by the existing runtime coordinator. Disposing the runtime prevents pending responses from an old adapter. Full scene/source reload behavior remains T17.

## Verification

268 tests pass: 207 contract and 61 Core. All 247 pre-T13 cases are unchanged. Added cases cover slash-token boundaries, quoted arguments, malformed/oversized ownership, authenticated Player-only ingress, filtered help, owner restrictions, audit redaction, repeated request IDs and bounded reply envelopes. No mocked engine transport is counted as runtime validation.

Actual engine 26.10.13a, Steam build 25964806: editor host PID 25616, client A PID 24908 (`90071996842377217`, connection `368b0bdf-92f0-4158-8c8d-34816068c4f1`) and B PID 23656 (`90071996842377218`, connection `4aebcfbd-3d15-42f2-bf70-a1b6155cc036`). With an eligible runtime and a fresh chat journal, `dev/verify-platform-chat-client.cs` sent ten messages through real `Chat.Say`, respecting the observed engine rate limit. `dev/verify-platform-chat-host.cs` found exactly seven distinct Chat-origin requests attributed to A: three version successes, one help success, one permission denial, one unknown command and one malformed command. A received seven corresponding private responses. B received only ordinary chat, `!admin version` and `/administrator version`; neither the host nor B received any private response or owned original message.

`dev/verify-platform-chat-guards.cs` supplements real networking with synthetic scene events: system and already-suppressed messages did not execute, ordinary chat remained unsuppressed, and a repeated callback executed once with suppression visible before completion. `dev/verify-chat-reply-forgery.cs` invoked the client-side HostOnly RPC wrapper; neither client nor host received its forged sentinel. This is not a malformed-packet penetration test.

Changing the root to `operators` produced a private success for `/operators version`; the former `/admin version` passed through as ordinary chat to B. The root was restored. The actual platform chatbox was captured and visually inspected (`dev/out/t13-chatbox-final.png`); private help was visible with readable rows and pagination. Captures and raw console/audit evidence remain local under ignored `dev/out/t13-*`.

The first test attempt had an unapplied editor switch and therefore no runtime; it correctly passed messages through as ordinary chat. The fixture was corrected by re-enabling bootstrap and confirming a runtime ID before the successful run. This observation is why setup explicitly requires that check. Idle test clients could not use the inaccessible native `connect` command; only the two owned clients were stopped by exact PID and relaunched with the verified `-joinlocal` arguments. The user's original editor was untouched.

Backup: `E:/06-um-data/backups/superuser-t13-chat-data/20261018-004924.zip` (43 files). Dedicated-client deployment remains unverified under T01's unpublished game-package prerequisite; the actual privacy checks above use editor networking. No package was published. T14 custom-backend APIs and result events are not part of this task.

Final verification: both runtime builds passed with zero warnings/errors, both package/Core checks passed, all 268 tests passed, and the engine whitelist was clean. Publication lint found zero failures and two ignored local-path warnings. Bootstrap was disabled and its runtime confirmed absent before archiving only new audit evidence to the lab's `dev/out/t13-chat-evidence`. Owner files remain absent, saved scenes are unchanged, and the two owned clients remain available for T14.
