# Reference: console commands

Use these forms in the console. In the chatbox, add `/`: `admin help` becomes
`/admin help`. Replace angle-bracket values; square brackets indicate optional
arguments.

## Discovery

| Command | Purpose |
|---|---|
| `admin help [command-or-*] [page]` | Permitted commands or detailed syntax |
| `admin commands [page]` | Command names |
| `admin version` | API version |
| `admin diagnostics` | Owner-only health |
| `admin modules list [page]` | Owner-only module state |
| `admin modules enable/disable <id>` | Owner-only lifecycle control |
| `admin results [command-or-*] [cursor]` | Your own recorded results |
| `admin audit query [command-or-*] [cursor]` | Audit pages; requires `admin.audit.read` |

Use `admin help * 2` for another help page. Audit cursors come from the previous
response and expire when the journal changes.

## Access management

Only owners and the dedicated local console can edit policy.

| Command | Purpose |
|---|---|
| `admin group create/delete <group>` | Create or remove a group |
| `admin group list [page]` | Groups and protection levels |
| `admin group <group> parents set <comma-separated-parents>` | Replace inheritance; `""` clears it |
| `admin group <group> parents list [page]` | Inspect inheritance |
| `admin group <group> protection set <level>` | Set a rank from 0 through 99 |
| `admin group <group> permission allow/deny/unset <key>` | Group permission entry |
| `admin user <user> group add/remove <group>` | Membership |
| `admin user <user> group list [page]` | Membership summary |
| `admin user <user> permission allow/deny/unset <key>` | Direct permission entry |
| `admin permissions explain <user> <key> [page]` | Why a permission allows or denies |

Other-user explanations require `admin.permissions.explain`. `<user>` accepts an
exact Steam ID or an unambiguous online name.

## Moderation

| Command | Permission |
|---|---|
| `admin kick <user> <reason>` | `admin.players.kick` |
| `admin ban <user> <duration> <reason>` | `admin.players.ban` |
| `admin unban <user> [reason]` | `admin.players.unban` |
| `admin warn <user> <reason>` | `admin.players.warn` |
| `admin mute <user> <duration> <reason>` | `admin.players.mute` |
| `admin unmute <user> [reason]` | `admin.players.unmute` |
| `admin gag <user> <duration> <reason>` | `admin.players.gag` |
| `admin ungag <user> [reason]` | `admin.players.ungag` |
| `admin gimp <user> <duration> <reason>` | `admin.players.gimp` |
| `admin ungimp <user> [reason]` | `admin.players.ungimp` |
| `admin note <user> <text>` | `admin.players.note` |
| `admin moderation history <user> [page]` | `admin.players.history` |

Quote reasons containing spaces. Durations are explicit: `30m`, `2h`, `7d` or
`permanent`. Kick needs an online player; the other operations support offline
Steam IDs.

Bans persist before disconnect. Expiry retains its original UTC timestamp after
restart. Repeated revocation returns NoChange when nothing is active. Warnings
do not escalate automatically. Mute stops integrated text chat; gimp swaps each
chat message for a stock line; gag stops voice chat. Voice goes between players
without the host, so a gag switches off the gagged player's `Voice` component on
every player's machine; games must play voice through a `Voice` component, as
the sandbox gamemode does. Each is lifted only by its own revocation. While
moderation storage is unavailable, chat is held back rather than let through.

## Messages

| Command | Permission | Default |
|---|---|---|
| `admin psay <user> <message>` | `admin.chat.psay` | everyone |
| `admin asay <message>` | `admin.chat.asay` | everyone |
| `admin csay <message>` | `admin.chat.csay` | admins |
| `admin tsay <message>` | `admin.chat.tsay` | admins |
| `admin thetime` | `admin.chat.thetime` | everyone |

`psay` whispers to one online player. `asay` reaches every online player with
`admin.chat.asay.read` (moderators by default), so players can call for staff.
`csay` shows the message in the middle of every screen for six seconds; `tsay`
adds it to everyone's chat without a name. Messages are printable text of at
most 256 characters, and muted players cannot use `psay` or `asay`.

## Players

Each needs `admin.players.<command>`. Moderators get the first group by default
and admins the second; owners get everything.

| Command | Does |
|---|---|
| `slay <user>`, `sslay <user>` | Kill; `sslay` is not announced |
| `slap <user> [damage]` | Knock them around |
| `whip <user> [times] [damage]` | Slap repeatedly, half a second apart |
| `freeze`, `unfreeze <user>` | Pin in place |
| `jail`, `unjail <user>`, `jailtp <user>` | Cage them; `jailtp` first moves them to where you look |
| `ignite`, `unignite <user>`, `unigniteall` | Set on fire, put out |
| `blind`, `unblind <user>` | Black out their screen |
| `bring`, `goto`, `teleport`, `return <user>`, `send <user> <destination>` | Move players |
| `spectate <user>`, `unspectate` | Watch through their eyes |
| `noclip [user] [on]` | Fly through walls; toggles yourself when no player is given |

| Admin command | Does |
|---|---|
| `maul <user>` | Heavy blows until they die |
| `strip <user>` | Take their weapons and items |
| `ragdoll`, `unragdoll <user>` | Turn into a ragdoll and back |
| `god`, `ungod <user>` | Damage immunity |
| `hp <user> <amount>`, `armor <user> <amount>` | Set health or armor |
| `cloak`, `uncloak <user>` | Hide their body from everyone |
| `playsound <sound>` | Play a sound asset for everyone |
| `map <map>` | Change the map; players stay connected |

Punishments need higher protection than the target and never work on yourself;
`god`, `hp`, `armor` and `cloak` work on yourself. What each one does depends on
the game; see [player actions](reference/players.md).

## Votes

| Command | Permission | Default |
|---|---|---|
| `admin votemap <map> [maps...]` | `admin.votes.map` | everyone |
| `admin vote <question> [options...]` | `admin.votes.vote` | moderators |
| `admin votekick <user> <reason>` | `admin.votes.kick` | moderators |
| `admin voteban <user> <duration> <reason>` | `admin.votes.ban` | moderators |
| `admin votemap force <maps...>` | also `admin.votes.mapforce` | admins |
| `admin stopvote` | `admin.votes.stop` | admins |
| `admin veto` | `admin.votes.veto` | admins |

One vote runs at a time, for 30 seconds. Everyone connected when it starts may
vote, and can change their mind until it closes. A vote passes only with more
than half of the server, whether or not everyone votes; a forced map vote is
won by the most votes instead. Players who leave stop counting either way.
`stopvote` decides the vote on the ballots so far, and `veto` cancels it.

A passed votekick or voteban is recorded, audited and protected exactly like
`kick` and `ban`, with the reason prefixed by "Vote:". One map is a yes or no
question; several maps are the choices.

Add **Superuser Vote Panel** to show the vote: it slides in from the left with
the question, one row per option and its key (F1, F2 and so on, or a click),
and an icon for every voter that lights up green or red as they choose, up to 30
with a count for the rest. Leave it out to draw your own from
`Superuser.CurrentVote` and `IVoteEvent`, sending ballots with
`Superuser.CastVote( option )`.

## Prop protection

Superuser records who spawned each object, from its network owner, and keeps
that after they leave. Player bodies are never counted as props.

| Command | Permission | Default |
|---|---|---|
| `admin props share <user>` | `admin.props.share` | everyone |
| `admin props unshare <user>` | `admin.props.share` | everyone |
| `admin props cleanup` | `admin.props.cleanupown` | everyone |
| `admin props cleanup <user>` | also `admin.props.cleanup` | moderators |
| `admin props cleanupall` | `admin.props.cleanupall` | admins |
| `admin props delay <duration>` | `admin.props.manage` | admins |

Staff with `admin.props.touchall` (moderators by default) may touch anything. A
player's props are removed two minutes after they leave, unless they come back
first. `admin props delay` changes that (10s to 7d, or `permanent` to keep them
until someone cleans up), and it is saved. The map's own objects are never
removed.

The engine already stops players moving objects they don't own, but anything
your game does on the host for them, such as a physics gun, tool or delete,
should ask first:

```csharp
if ( !Superuser.CanTouch( caller, target ) ) return;
```

`Superuser.PropOwner( target )` gives the spawner's Steam ID, or null for the
map's objects.

## Alt accounts

| Command | Permission | Default |
|---|---|---|
| `admin alts check <user>` | `admin.alts.read` | admins |
| `admin alts suspects [page]` | `admin.alts.read` | admins |
| `admin alts ips <user>` | `admin.alts.ips` | owners only |

Superuser records each player as they join and links accounts that share:

| Evidence | Means | Strength |
|---|---|---|
| Family share | One plays on the other's copy of the game through Steam Family Sharing | Strong |
| Same IP | They connected from the same IP address | Medium: the same person or household |
| Same party | They joined in the same Steam party | Weak: usually friends |

Accounts linked to a banned account rank first, marked "Ban evasion?".
**Moderation › Alt accounts** shows the suspects beside the chosen account's
linked accounts and, for staff with `admin.alts.ips`, its IP address history;
the **Player** window has the same in an **Alts** tab.

Each account keeps its last 5 IP addresses with when each was first and last
seen. The engine only reveals a player's real address on direct connections;
through a Steam relay there is no address to record, and linking falls back to
family sharing and parties. The newest 500 accounts are kept, and accounts unseen
for 90 days are forgotten. IP addresses are personal data: keep
`admin.alts.ips` to the people who need it.

## Content bans

Needs `admin.content.manage` (admins by default).

| Command | Does |
|---|---|
| `admin content ban <kind> <pattern> [reason]` | Ban a map, addon or entity |
| `admin content unban <kind> <pattern>` | Allow it again |
| `admin content list [page]` | Everything banned |
| `admin content search <kind> [query]` | Find what isn't banned yet; the menu's lists use it |

`<kind>` is `map`, `addon` or `entity`. Patterns ignore case, and `*` matches
anything, so `addon badauthor.*` bans every package from one author.

| Kind | Pattern | Enforced |
|---|---|---|
| `map` | Map ident or `.vmap` path | `map`, `votemap` and `votemap force` refuse it |
| `entity` | Prefab path, model path or component type name | Superuser removes it within half a second of a player spawning it, and tells them why |
| `addon` | Package ident | The game asks `Superuser.IsContentAllowed( ContentKind.Addon, ident )` before mounting or spawning from a package |

Entity bans only touch what players own; the scene's and map's own objects are
never removed. Spawned objects don't record which package they came from, so
addon bans need the game's spawn code to ask, as above.

## Message wording

Every line Superuser shows players has a key and can be reworded. Changing
wording needs `admin.messages.manage` (admins by default).

| Command | Does |
|---|---|
| `admin messages list [page]` | Every key and its current wording |
| `admin messages set <key> <wording>` | Change one |
| `admin messages reset <key>` | Back to Superuser's wording |

Wording is one printable line of at most 256 characters. Each key accepts only
its own placeholders; `{actor}` is who did it, `{target}` who it happened to,
`{duration}` reads like "for 10 days" or "permanently", `{reason}` drops its
trailing full stop, `{map}` is a map and `{message}` is what was said.

| Keys | Placeholders |
|---|---|
| `ban`, `mute`, `gag`, `gimp` | `{actor}` `{target}` `{duration}` `{reason}` |
| `kick`, `warn`, `unban`, `unmute`, `ungag`, `ungimp` | `{actor}` `{target}` `{reason}` |
| `slay`, `slap`, `whip`, `maul`, `freeze`, `jail`, `ignite`, `blind`, `strip`, `ragdoll`, `god` and their `un` forms | `{actor}` `{target}` |
| `psay`, `asay` | `{actor}` `{message}` |
| `map` | `{actor}` `{map}` |
| `join`, `leave` | `{target}` |
| `tag` | none; the badge in front of announcements, 24 characters at most |

```console
admin messages set ban {actor} removed {target} {duration}: {reason}
admin messages set tag SERVER
```

Join and leave lines replace the engine's own, without its emoji.

## Prefixes

Reading needs nothing; changing needs `admin.prefixes.manage`.

| Command | Purpose |
|---|---|
| `admin prefix list [page]` | Every prefix with its tag and effects |
| `admin prefix create <id> [tag]` | A new prefix |
| `admin prefix update <id> <field> <value>` | `name`, `tag`, `template`, `palette`, `tag-effect` or `name-effect` |
| `admin prefix delete <id>` | Delete it and its assignments |
| `admin prefix group set/clear <group> [id]` | A group's prefix; `owner` means every owner |
| `admin prefix user set/clear <user> [id]` | A player's own prefix |
| `admin prefix export` | The catalog as JSON |

See ***[Prefixes](prefixes.md)*** for effects and examples.

## Settings

After the game registers [native bindings](reference/settings.md), use `admin
settings list`, `admin settings get <key>`, `admin settings set <key> <value>`
and `admin settings reset <key>`. Each binding has an explicit permission.

For health when normal dispatch cannot write audit records, use local
`superuser_status`, `superuser_authority_status` and `superuser_audit_status`.
Owner and storage recovery are [dedicated-local
operations](reference/recovery.md), not chat commands.
