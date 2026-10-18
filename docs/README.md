# Superuser guide

Superuser connects your game's commands to authenticated players, permissions and private replies. It runs in the dedicated server process; the editor switch enables local testing.

Start with setup, then add a ping command. Each chapter includes only the code needed for that step.

## Getting started

1. [Setup](setup.md) — add the library, configure an owner, run a command.
2. [Your first command](commands-tutorial.md) — register `ping` and grant its permission.
3. [Custom chat](chat.md) — use platform chat or your own backend.
4. [Admin menu](admin-menu.md) — enable the default view or connect your own.
5. [Custom storage](storage-tutorial.md) — register an adapter and implement a database backend.

## Reference

- [Commands](commands.md) — operator syntax.
- [Permissions](permissions.md) — groups, denies and target protection.
- [API and modules](api.md) — public entry points and module lifetime.
- [Native settings](settings.md) — bind a ConVar without bypassing engine rules.
- [Storage and recovery](recovery.md) — backups and local recovery.

## Examples and verification

- [Two game integrations](../dev/samples/README.md)
- [Build and validation](validation.md)
- [Release notes](release.md)

[Repository](../README.md)
