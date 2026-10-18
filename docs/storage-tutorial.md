# Custom storage

[Guide](README.md) · [Admin menu](admin-menu.md) · [Storage and recovery](recovery.md)

Superuser uses local `FileSystem.Data` storage by default. A game can select another backend before enabling its bootstrap. The adapter runs inside the same s&box library/game process; no database, executable or service is installed by Superuser.

## Start with a working adapter

1. Copy [CustomAdminStore.cs](../dev/fixtures/CustomAdminStore.cs) into your game's `Code/<YourRoot>/Engine/`.
2. Copy [CustomStorageSetup.cs](../dev/fixtures/CustomStorageSetup.cs) into `Code/<YourRoot>/Components/`. Change both namespaces and its Engine import to your game's root.
3. Leave **Superuser Bootstrap disabled** in the scene. Attach **Custom Storage Setup** once and assign that bootstrap to its **Bootstrap** field.
4. Start your game's network session. The setup selects storage, then enables the bootstrap. For local testing, turn on **Allow Editor Testing** on the bootstrap.

The example forwards to the built-in backend and logs failed writes without logging private data. It compiles and runs as-is. Its registration is:

```csharp
Superuser.ConfigureStorage( context => new CustomAdminStore( context.LocalStore ) );
```

Register once per scene, before any bootstrap is enabled. `context.IsDedicatedServer` distinguishes production from editor tests; `context.LocalStore` already separates their data. The factory runs only on an eligible host. A null result or thrown factory leaves storage unavailable; it never silently switches databases.

## Connect your database

Replace the example's forwarding calls with your backend operations. Use an s&box-compatible transport to a database you control. A normal desktop SQL driver is not automatically usable under the engine whitelist. Keep credentials in host configuration, never in shipped code or client RPCs.

`IAdminStorageBackend` requires five operations:

| Operation | Required behavior |
|---|---|
| `ReadAsync` | Return a compatible document or an explicit status. Only an absent key is Missing; an outage is Unavailable. |
| `WriteAsync` | Atomically compare the stored revision with ExpectedRevision, then persist the entire payload at the next revision. Retain a recoverable previous version. |
| `GetBackupsAsync` | Return the observed head and at most 100 verified backups, newest first. |
| `RecoverAsync` | Restore the chosen generation as a new revision, including the supplied recovery intent; reject a changed head. |
| `PruneAuditHistoryAsync` | Check the audit head and prune superseded audit generations in bounded batches. Preserve the current journal and recoverability. |

Store the key's module/document IDs, schema, revision and complete `PayloadJson`. The payload includes state and audit intent; persist them together in one database transaction. A zero expected revision means create only if absent. Return Conflict when the revision changed. Return Committed only after confirmation; a lost response after a possible commit is Unknown.

Keep editor and dedicated namespaces separate. Use one active authoritative host per namespace: sharing a database does not add cross-server policy cache synchronization. Do not migrate live data by changing providers. Stop the host, back up, migrate and verify before restarting.

## Verify your backend

Test first creation, stale-revision conflict, restart, unavailable storage, an interrupted commit, recovery and audit retention. Failed ban writes must not disconnect players. Exercise it in the actual editor and dedicated server; a plain .NET build does not prove whitelist compatibility.

The supplied decorator is verified with local storage. No external database provider or endpoint is supplied or claimed tested. Bootstrap owner files remain local; managed owners, policy, moderation, audit and modules using `Superuser.GetStore()` use the selected backend.
