# T13: Platform chat commands

Status: **In progress.** The user-selected prefix is `/`, replacing the specification's illustrative `!` prefix. The bootstrap command root determines the complete chat prefix: `/admin` by default, or `/operators` when Command Root is `operators`.

Only an exact prefix followed by whitespace or the end of the message is owned. Malformed, unknown and denied admin commands must be consumed and answered privately. Ordinary chat, `!admin`, `/administrator`, system messages and already-suppressed events must remain outside command dispatch. Suppression must happen before asynchronous work. One bootstrap-owned listener and two real clients must verify execution and privacy before this task is complete.
