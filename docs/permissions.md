# Permissions

[Guide](README.md) · Previous: [Commands](commands.md) · Next: [API and modules](api.md)

A command names one exact permission key. Registering the key does not grant it. Unknown keys and missing grants deny by default; wildcards are not supported.

## Resolution

1. A direct user entry wins over inherited entries.
2. Otherwise, any inherited deny wins over inherited allows.
3. Otherwise, an inherited allow grants access.
4. Otherwise, deny.

Every player inherits the `player` group. Groups can have several parents; cycles are rejected. Parent order does not change the answer. `unset` removes an entry so inheritance applies again.

Use `admin permissions explain <steamid> <key>` to see the reason for a decision.

## Owners

Owners come from explicit host configuration or dedicated-local managed ownership. They bypass registered framework permission checks. They do not bypass native engine restrictions.

Only owners and the dedicated local console can change policy. Granting `admin.security.manage` to an ordinary administrator does not make that player an owner. The reserved `owner` identity cannot be assigned as a normal group.

## Target protection

Punitive actions require a strictly higher protection level than the target. Equal rank and self-punishment are denied. Group levels range from 0 through 99; owners have the reserved level 100, so one owner cannot punish another. Offline players use their stored groups.

The actual dedicated local console has separate authority. Null callers and client-supplied roles never become console authority.

## Revocation

Permissions are checked again when queued work starts. Policy changes and recovery invalidate cached decisions. Required security modules cannot be disabled through ordinary module commands.
