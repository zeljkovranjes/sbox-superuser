# Storage and recovery

[Guide](README.md) · Previous: [Native settings](settings.md) · Next: [Build and validation](validation.md)

Data belongs to the adopting game's `FileSystem.Data`. Editor and dedicated records are separated under `superuser/store/editor/` and `superuser/store/dedicated/`.

This page describes the default local backend. For a replacement, follow [custom storage](storage-tutorial.md) and its provider's backup procedures. The same local recovery commands call the selected backend.

## Back up

Stop the host, then copy the entire `superuser/` directory. Include owner configuration, generation files and commit markers. Keep backups private: they contain policy, moderation and audit records.

Use only one host process per physical data root. A successful write means the generation and marker closed and passed integrity readback. Process interruption recovery is tested; power-loss durability is not guaranteed.

## Inspect a failure

Use the actual dedicated server's local console:

```text
superuser_status
superuser_authority_status
superuser_audit_status
superuser_store_backups <module> <document>
```

Useful document keys are `security policy`, `security owners`, `moderation history` and `audit journal`.

Corrupt or unsupported records fail closed. Do not delete damaged files to simulate a fresh installation. An interrupted action stays Unknown; inspect its request ID before repeating an effect.

## Recover a document

1. Stop the server and preserve a separate copy of the damaged directory.
2. Restart with the same data root. Inspect compatible backups and the observed head revision.
3. Choose the intended generation, then run:

```text
superuser_store_recover <module> <document> <generation-guid> <observed-head-revision>
```

4. Wait for Committed, then inspect health and the resulting state.

Recovery appends a new generation and preserves damaged evidence. A changed head returns Conflict; inspect again. Restoring older moderation state may restore or undo sanctions. Without a compatible generation, restore a complete known-good document backup while the host is stopped.

## Recover owners

These commands are dedicated-local only:

```text
superuser_owner_add <steamid>
superuser_owner_remove <steamid>
superuser_owner_recover replace <steamid> [steamid...]
superuser_owner_recover clear
superuser_policy_reload
```

Normal removal refuses the final owner. Explicit `recover clear` is the exception: it removes player owner authority while leaving local recovery available.

Once a managed owner record exists, editing the bootstrap JSON file no longer changes ownership. Repair corrupt storage first, then use `recover replace`. These operations refuse clients and editor mode; they are not exposed through chat.

## Retention and export

Audit defaults to 512 rows and 30 days, also bounded by encoded size. Host code can configure it through `ConfigureAuditRetentionAsync`; active intents remain protected. Query pages through `QueryAuditAsync`, or copy the stopped host's files for a private export.

Other stores retain generations. Moderation keeps at most 512 history records with space reserved for revocations; it refuses new records at capacity rather than deleting history. Plan offline backups and capacity management before production use.
