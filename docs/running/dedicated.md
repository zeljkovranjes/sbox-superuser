# On a dedicated server

Leave **Allow Editor Testing** off. On an actual dedicated server the runtime
starts with the network session, and its data lives under
`superuser/store/dedicated/` in the game's `FileSystem.Data`.

Owners come from `superuser/owners.json`:

```json
{"schemaVersion":1,"owners":["76561198000000001"]}
```

Once dedicated-console owner commands have created managed ownership, that stored
list takes precedence over the file; see
[storage and recovery](../reference/recovery.md).

## The local console

The server's own local console has distinct authority: it can always run
`admin` commands, and only it can run the owner and storage recovery tools.

```text
superuser_status
superuser_authority_status
superuser_audit_status
```

These report health even when normal dispatch cannot write audit records.

## What has been verified

On engine 26.10.13a, actual dedicated processes have been restarted with policy,
moderation and original UTC expiry preserved, and host, console and restart
checks pass. Client privacy and admission were tested with two editor clients.
Dedicated **remote-client** deployment is not yet verified, because the
unpublished test game lacks the package record that requires. See
[verification](../validation.md) for the full list.
