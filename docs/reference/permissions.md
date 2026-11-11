# Permissions

A command names one exact permission key. Registering the key does not grant it.
Unknown keys and missing grants deny by default; wildcards are not supported.

## Resolution

1. A direct user entry wins over inherited entries.
2. Otherwise, any inherited deny wins over inherited allows.
3. Otherwise, an inherited allow grants access.
4. Otherwise, deny.

Every player inherits the `player` group. Groups can have several parents;
cycles are rejected. Parent order does not change the answer. `unset` removes an
entry so inheritance applies again.

Use `admin permissions explain <steamid> <key>` to see the reason for a
decision.

## Owners

Owners come from explicit host configuration or dedicated-local managed
ownership. They bypass registered framework permission checks. They do not
bypass native engine restrictions.

Only owners and the dedicated local console can change policy. Granting
`admin.security.manage` to an ordinary administrator does not make that player
an owner. The reserved `owner` identity cannot be assigned as a normal group.

## Engine users.json

The engine's own `config/users.json` gives Steam accounts claims. Superuser
reads them when a player joins and grants:

| Claim | Superuser access |
|---|---|
| `superuser` or `owner` | Owner |
| `admin` | The `admin` group |
| `moderator` or `mod` | The `moderator` group |

```json
{ "SteamId": "76561198000000001", "Claims": [ "admin", "kick", "ban" ], "Name": "Alice" }
```

This access is live and never saved: take someone out of `users.json` and they
lose it when they next join; it ends at once when they leave. Other claims such
as `kick` keep their engine meaning and grant nothing in Superuser. The engine
only checks claims on Steam connections, and the host is skipped because the
engine gives it every claim.

## Target protection

Punitive actions require a strictly higher protection level than the target.
Equal rank and self-punishment are denied. Group levels range from 0 through 99;
owners have the reserved level 100, so one owner cannot punish another. Offline
players use their stored groups.

The actual dedicated local console has separate authority. Null callers and
client-supplied roles never become console authority.

## Revocation

Permissions are checked again when queued work starts. Policy changes and
recovery invalidate cached decisions. Required security modules cannot be
disabled through ordinary module commands.
