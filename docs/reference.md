# Reference: the host API

The ***[Core concepts](concepts.md)*** chapter covered the model Superuser uses.
This chapter documents what your game's code calls to build on it. Everything is
in the `SboxSuperuser` namespace; call host APIs on the engine main thread after
the bootstrap is ready.

- ***[The Superuser facade](reference/facade.md)*** &mdash; every public entry
  point on one page, and which ones must never be exposed to player input.

- ***[Commands](reference/commands.md)*** &mdash; attributed commands, argument
  types, game services, results and retries.

- ***[Modules](reference/modules.md)*** &mdash; module lifetime, dependencies,
  reload, and API compatibility.

- ***[Permissions](reference/permissions.md)*** &mdash; how a decision is
  resolved; owners; target protection; revocation.

- ***[Chat](reference/chat.md)*** &mdash; platform chat, an independent chat
  backend, and rendering private results in your own view.

- ***[Native settings](reference/settings.md)*** &mdash; exposing a ConVar or
  native command without bypassing engine rules.

- ***[Custom storage](reference/storage.md)*** &mdash; replacing the default
  storage with your own backend.

- ***[Storage and recovery](reference/recovery.md)*** &mdash; where data lives,
  backups, failure inspection, and owner recovery.

The source of truth for signatures is the
[facade and public types](../Code/SboxSuperuser). `Core/` holds the
engine-independent rules; integrations use the public facade, never internal
classes.
