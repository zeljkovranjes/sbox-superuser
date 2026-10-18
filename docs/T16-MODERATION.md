# T16: Moderation and admission

Status: **Implemented; final checks in progress.** Required administration code persists moderation inside the same library and uses the verified engine connection and chat hooks. No voice mute, external service or player-controller dependency is included.

## Commands

| Command | Required permission | Behavior |
|---|---|---|
| `admin kick <user> <reason>` | `admin.players.kick` | Requires an online target, persists its intent, then requests and confirms disconnect. |
| `admin ban <user> <duration> <reason>` | `admin.players.ban` | Persists the ban before disconnecting an online target; supports offline Steam IDs. |
| `admin unban <user> [reason]` | `admin.players.unban` | Appends a revocation linked to the original active ban. |
| `admin warn <user> <reason>` | `admin.players.warn` | Stores a warning; active targets receive a private console notification. Offline targets retain history. |
| `admin mute <user> <duration> <reason>` | `admin.players.mute` | Restricts integrated ordinary text chat; supports offline targets. |
| `admin unmute <user> [reason]` | `admin.players.unmute` | Appends a linked text-mute revocation. |
| `admin moderation history <user> [page]` | `admin.players.history` | Returns eight private records per page. |

Chat uses the same commands with `/admin`. Targets accept exact Steam IDs or unique online names; names never silently select among duplicates. Reasons require 1–256 printable characters. Ban and mute require explicit positive durations such as `30m`, `2h`, `7d`, or `permanent`. Zero, negatives, ambiguous values and overflow are rejected. Default revoke reason is `Revoked by operator.`

Punitive operations recheck permission and target protection before persistence. Owners cannot punish themselves; equal-rank punishment is denied. The dedicated local console has its separate verified authority. Existing active restrictions return Conflict rather than silently changing their expiry. Repeated unban/unmute, or revoking an already expired restriction, returns NoChange without another moderation record. Warnings never escalate automatically.

## Admission and text paths

`SuperuserBootstrap` implements the engine's `INetworkListener.AcceptConnection`. It uses that callback's engine connection identity before OnConnected/OnActive, reads only cached validated moderation state and rejects active bans. Unready, duplicate-bootstrap or degraded moderation on an otherwise eligible host fails closed. Disabled bootstrap cannot enforce anything: keep exactly one enabled bootstrap in the adopting game's network scene.

Platform chat checks the command prefix first, then ordinary-message moderation. Muted players can still submit commands and receive permission errors or recovery feedback. Ordinary muted text is suppressed synchronously before broadcast and receives a private error. The independent T14 backend calls `Superuser.CheckOrdinaryChat` before routing, so it enforces the same mute. A separate, unintegrated channel is unaffected; voice is unsupported. Restrictions remain enforced when native command-root configuration is unavailable.

Warnings use the engine's targeted console logging after persistence, not shared chat or synchronized properties. Ban and kick use the supported host-only `Connection.Kick`. A missing target after a persisted ban is a successful offline ban; a kick target that leaves before the disconnect produces NoChange. A disconnect that cannot be confirmed within one second reports Unknown. Stored kick records are intents: consult their correlated audit outcome for actual execution.

## Storage and expiry

The versioned `moderation/history` aggregate lives under the dedicated or editor store root. Records retain action/request IDs, target Steam ID, actor, origin, reason, creation UTC, optional expiry UTC and revocation links. A confirmed ban transaction precedes disconnect; required write failure never kicks the player or reports success. Interrupted or uncertain persistence reports Unknown and fails closed until valid state is established.

Expiry uses the original absolute UTC timestamp on lookup, including after restart. Each lookup removes at most one expired active-index entry; immutable history is retained. No delayed task must survive for expiry to work. The aggregate has a 512-record and 384000-byte state ceiling, with record and byte space reserved for revoking every active restriction. When the remaining budget is exhausted, new records are refused; historical records are not silently deleted. This is a v1 operating limit, not automatic moderation-history retention.

Invalid schemas, lost known history and corrupt data never reset bans to an empty state. Diagnostics reports ModerationDegraded; admission and ordinary chat fail closed while other available diagnostics/recovery remain usable. Follow [versioned storage recovery](T04-STORAGE.md), selecting a compatible moderation generation explicitly and retaining damaged evidence. Restoring an older generation can intentionally restore or undo sanctions, so inspect the chosen history. Only trusted host code or the verified dedicated-local recovery route can perform recovery.

Moderation handlers are part of required administration and are not an independently removable module. Scene/runtime teardown retires them with the rest of bootstrap. No lobby or host-migration support is claimed.

## Actual engine verification, 2026-10-18

Engine 26.10.13a, isolated editor host PID25616 and actual clients A24908/B23656:

- Offline warning, ban, unban and short mute passed. Active-ban replacement returned Conflict; repeated revocation returned NoChange. Expired mute stopped applying after its original one-second UTC deadline. History retained all four original/revocation records.
- A's warning appeared only in A's console. Both platform and independent chat suppressed muted text; A still received private version-command feedback. B received neither muted messages nor private replies. Unmute restored ordinary delivery through both paths. The rendered chatbox screenshot was inspected.
- Ban request `62754534-da89-46e1-ad32-46cf21aefb01` committed action `f275e3ea-031f-4162-88d7-046e75006e6e`, then confirmed B's disconnect. An injected real generation-write failure against A returned StorageUnavailable, preserved moderation revision8 and left A connected.
- New client6844 with B's same engine test identity was rejected before OnConnected/OnActive; no game scene loaded. After unban, client27084 joined successfully. A separate kick confirmed disconnect and retained ban, unban and kick records. Client11344 subsequently reconnected without a ban.
- An unsupported module schema produced ModerationDegraded. New client18080 was rejected before OnConnected/OnActive. Explicit validated-backup recovery appended revision12 and preserved the exact prior history. No empty-state fallback occurred.
- Actual host-side requests attributed to connected A verified equal-rank refusal and permission revocation while queued behind a held command. Both returned Denied and left moderation revision12 unchanged. Temporary policy grants were removed afterward.

Actual dedicated server15484 used native console commands to persist two offline bans (one permanent, one one-hour) and a permanent mute. After an exact-PID stop and fresh process26732, all three were active and the entire stored payload, including original UTC expiry and IDs, was unchanged. These are dedicated storage/authority checks; dedicated-client admission remains uncertified because the adopting game is unpublished. The real connection-gate checks above are editor-host multiplayer evidence.

## Reproduction

Back up the isolated game data first. `setup-moderation-host.cs` requires no existing editor owner file and a disabled runtime; it configures the test host owner and creates a runtime-only independent backend. Run `verify-moderation-editor.cs`, then T14 client-view setup on two clients and `verify-moderation-chat-client.cs` on A. On B check both transport modes with `verify-moderation-chat-peer.cs` before unmute. Verify ordinary delivery after unmute.

`verify-moderation-ban-host.cs` expects connected B with test identity90071996842377218. Relaunch that same test identity while banned and inspect both server and client logs; it must not reach OnConnected. Unban it, reconnect and run `verify-moderation-kick-host.cs`. `verify-moderation-storage-failure.cs` interrupts a real write and requires A still connected. `inject-moderation-schema-failure.cs` records its recovery backup, then installs a deliberately unsupported module schema; test a new connection while degraded and run `recover-moderation-schema.cs`. Finally run `verify-moderation-authority.cs` for equal-rank and queued-revocation checks.

On an actual dedicated fixture run `seed-moderation-dedicated.cs`, restart the owned process with the same data root, then run `verify-moderation-dedicated-restart.cs` within the one-hour restriction. These development scripts are not shipped game features. Restore the original owner file, policy, bootstrap options, client overlays and runtime objects after testing; preserve generated files as evidence. Saved scene edits are unnecessary.
