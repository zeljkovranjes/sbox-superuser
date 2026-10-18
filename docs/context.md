# Where Superuser fits

Superuser is one particular design point for server administration, and it is
worth knowing which problems it takes on and which it leaves to you.

## What it is built for

**Authoritative dedicated servers.** The host runtime exists on an actual
dedicated server, or on the editor host when you opt in for testing. Every
decision is made there. Clients submit requests; they never hold authority, a
cached permission or a copy of private records.

**Games that define their own players.** The library has no player class, pawn
or inventory model. Gameplay commands live in your modules and reach your game
through services you approve explicitly. The two
[samples](../dev/samples/README.md) prove this with a pawn-based game and a
roster-based game that share no player code.

**Operators who need to know what happened.** Every attempted action is recorded
before it runs and its outcome afterwards. An outcome that cannot be confirmed
is reported as Unknown instead of being guessed. Storage keeps earlier versions
so a bad change can be recovered.

## What it deliberately leaves out

- **No arbitrary console execution.** Native ConVars and commands are available
  only when your game registers them explicitly, with their own permissions; see
  [native settings](reference/settings.md).

- **No silent fallbacks.** If the configured storage cannot be reached, storage
  is unavailable. Superuser never quietly switches to another backend, because a
  ban written to the wrong place is worse than a ban that visibly failed.

- **No cross-server synchronization.** One authoritative host per data
  namespace. Sharing a database between servers does not share their policy
  caches.

- **Not yet:** voice moderation, player-hosted production servers and host
  migration.

## Built into the process

Superuser runs inside the game process and installs nothing beside it: no
external executable, service or database. The default storage is the game's own
`FileSystem.Data`. A game that wants a real database writes a storage adapter
that runs inside the same process and uses s&box-compatible APIs; see
[custom storage](reference/storage.md).
