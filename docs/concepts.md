# Core concepts

This page is a brief overview of the major concepts in Superuser, enough so that
you recognize the shape of things as you read the tutorial and the reference
chapters.

Superuser is built from five kinds of pieces:

- **The host runtime** &mdash; created by one **Superuser Bootstrap** component in
  your network scene. It exists only where authority lives: an actual dedicated
  server, or the editor host when you opt into editor testing. Clients never run
  it, so nothing a client holds is ever trusted.

- **Commands** &mdash; named operations with typed arguments and exactly one
  permission key each. Built-in commands cover help, diagnostics, policy and
  moderation; your game adds its own. The same dispatcher serves the console,
  slash chat, your own chat backend and the admin menu.

- **Permissions** &mdash; exact keys such as `admin.players.ban`, granted or
  denied on groups and individual players. Everything is denied until granted.
  **Owners**, configured explicitly by the host, sit above all groups.

- **Modules** &mdash; your game's code packaged as an `IAdminModule`. A module
  declares its permissions, registers its commands and approves the game
  services those commands may use. Modules start, stop and reload as a unit.

- **Storage and audit** &mdash; versioned documents for policy, owners and
  moderation history, plus an audit journal of every attempted action. Storage
  is local `FileSystem.Data` by default and can be replaced by your own backend.

```text
 console ─┐
 /chat ───┼──▶ host runtime ──▶ authenticate ─▶ authorize ─▶ audit intent ─▶ run ─▶ audit outcome
 menu ────┘         │                                                                 │
                    └──────────────────────── private reply to the sender ◀───────────┘
```

## How a request runs

Every request follows the same path, whichever way it arrived.

1. **Identify** the sender from the engine connection. A Steam ID or role inside
   the message is never used for authority.
2. **Check limits**: size, token count, request rate and pending work.
3. **Deduplicate** by request ID. A retry with the same ID and payload returns
   the original outcome; a different payload under the same ID is a Conflict.
4. **Authorize** against the command's permission key, and for punitive actions,
   against target protection.
5. **Record an intent** in the audit journal. If that write fails, nothing runs.
6. **Run** the command, checking authority and targets once more first.
7. **Record the outcome**. If that write fails, the result is reported as
   Unknown, never as success.
8. **Reply** privately to the sender, and only to the sender.

## Outcomes

Every command returns a `CommandStatus`. Most are self-explanatory; one is not.
**Unknown** means the host could not confirm what happened, for example because
the outcome could not be recorded or the connection dropped. Treat it as "go and
look": check Activity or the audit log before repeating anything with an effect.

## Owners and protection

Owners come from host configuration, never from a command a player can run. They
bypass framework permission checks but not the engine's own restrictions. Groups
carry a protection level from 0 to 99; owners have the reserved level 100. A
punitive action needs a strictly higher level than its target, so equals cannot
punish each other and no owner can punish another owner.
