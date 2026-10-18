# T05: Permission resolver and groups

Status: **Complete.** Tasks 1–4 remain complete. The current work order continues with T06 through T10, stopping before T11.

Implemented exact registered permissions, immutable user/group policy, implicit player membership, multiple inheritance with cycle rejection, direct-user precedence, inherited-deny precedence, default deny, revision-aware cache invalidation and diagnostic explain results. Owner authority and target protection are T06; command registration, parsing, dispatch and audit outcomes follow in T07–T10.

## Public policy model

`GroupPolicy` snapshots its stable ID, parent IDs, exact permission entries and nonnegative protection level. `UserPolicy` snapshots a canonical Steam ID, explicit groups and direct entries. `PolicySnapshot` validates the complete graph before exposing it: player is required, group/user IDs are unique, every referenced group exists, and cycles are rejected. Ordinary groups cannot create, inherit or assign the reserved owner identity.

`PolicySnapshot.CreateDefault()` defines player (public help and own results), moderator (player, kick/warn/text-mute permissions, protection 10) and admin (moderator, ban/unban/history and bound operational setting permissions, protection 50). It assigns no users. Registered permissions are separate: a stored allow for an unregistered key still denies. Owner authority is not acquired by naming a group or constructing these data types.

```csharp
var policy = PolicySnapshot.CreateDefault();
var resolver = new PermissionResolver( policy,
    new[] { new PermissionDescriptor( "admin.help", "Show command help" ) } );
var explanation = resolver.Explain( "76561198000000001", "admin.help" );
```

This is an isolated, single-threaded policy evaluator. It neither authenticates an engine caller nor installs policy into a live bootstrap. T06 owns trusted host authority, persistent administration and protected explain access; the T09 dispatcher will consume those checks. T05 has no native command, chat listener or gameplay action.

## Resolution and invalidation

Every user, including an unknown/offline Steam ID, receives player and all its ancestors. All assigned groups and ancestors contribute once, including diamond inheritance. An exact registered permission uses direct Allow/Deny first. With no direct entry, any inherited Deny wins, then any inherited Allow, otherwise deny. Unset removes an entry. Parent order cannot affect the answer; explanation sources are sorted by group ID and retain deny contributions even when a direct Allow overrides them.

Explain exposes final allowance, direct entry, contributing groups, policy and registration revisions, registered-key status, owner-bypass flag and a useful reason. This evaluator never sets owner bypass. Unknown keys deny with an explicit diagnostic; case variants and wildcard syntax are rejected by the existing exact-key contract.

`ReplacePolicy` accepts only a complete, strictly newer snapshot and clears the cache. `ReplaceRegisteredPermissions` validates the whole replacement before changing it, increments its own revision and clears the cache, including unknown-key decisions. A failed replacement leaves active policy and cached decisions intact. The cache retains at most 1024 decisions; graph limits are 256 groups, 10000 users and 4096 registered keys. T04's serialized payload limits independently constrain persisted policy.

## Validation

- 79 contract/Core tests passed (55 existing unchanged, 24 new cases). New coverage includes all direct/inherited conflict combinations in both parent orders, unknown permissions, implicit player/offline subjects, Unset, policy/registry invalidation, rejected stale revisions, one/two/three-node cycles, missing references, duplicate IDs, diamond inheritance, input mutation, reserved owner names and default groups.
- Runtime .NET build passed with zero warnings/errors. `sbox-check` passed and the original empty test project was run without claiming additional passed cases.
- Actual engine 26.10.13a, Steam build 25964806, editor PID13136 compiled the library and adopting game with zero whitelist errors/warnings. `dev/verify-permissions.cs` ran inside the engine and passed direct precedence, inherited denial, policy/registry invalidation, unknown-key denial and cycle rejection. This was a pure policy exercise in the loaded engine assembly, not a claimed network authorization test.
- Raw evidence is in ignored `dev/out/evidence/t05-*`; backup before this task sequence is `E:/06-um-data/backups/superuser-t05-t10-lab-data/20261017-221314.zip`. T01–T04 source behavior and existing tests remain intact. No T05 blocker remains.
