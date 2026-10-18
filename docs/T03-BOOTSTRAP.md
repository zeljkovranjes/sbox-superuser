# T03: Trusted bootstrap and identity

Status: **In progress**. Only bootstrap, identity, explicit owner configuration and diagnostics are in scope. T04 onward remain not started.

Design: one scene-owned system coordinates bootstrap registrations. Only the active, authoritative network scene can own a usable runtime; scene transitions retire older runtimes. Duplicate components fail closed. Production requires an actual dedicated server; editor testing remains explicit opt-in.

Player identities come from live engine connection objects, never supplied Steam IDs or roles. DedicatedConsole is issued only inside a separate non-forwarded native command on a dedicated host; null on the shared player path is rejected. Owner IDs load from a fixed host-local file, with a separate editor file, strict validation and no automatic host/claim/group promotion. No store writer, permission resolver or dispatcher is implemented.
