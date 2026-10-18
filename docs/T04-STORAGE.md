# T04: Versioned durable storage

Status: **In progress**. Implement the host-local store, format/schema validation, serialized writes, immutable backups, explicit recovery, and transactional state/audit-intent data. T05 onward remain not started.

Use FileSystem.Data only. T01's inaccessible atomic replacement API is not assumed. Each write creates a new generation and then a separate validated commit marker. Acknowledgement follows successful readback of both. Retain previous committed generations as backups. Uncommitted candidates can be ignored; corrupted commit records or committed payloads fail closed and require explicit operator recovery. Never reinterpret existing damaged storage as a fresh installation.

Policy state and its audit intents belong to one versioned aggregate. Pending action intents remain Unknown; action execution and audit-outcome management remain T09/T10. Bootstrap owner configuration stays separate and is not automatically migrated or overwritten.
