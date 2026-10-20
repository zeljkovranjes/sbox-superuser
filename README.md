# Superuser

***Administration for s&box dedicated servers.***

[<img alt="github" src="https://img.shields.io/badge/github-zeljkovranjes/sbox--superuser-8da0cb?style=for-the-badge&labelColor=555555&logo=github" height="20">](https://github.com/zeljkovranjes/sbox-superuser)

This library gives an s&box game a complete administration layer of commands,
permissions, moderation, audit and storage, running inside the game's own
dedicated server process. Players reach it through the console, slash
commands in chat, or an optional in-game moderation menu.

![The Superuser admin menu in game: active bans and mutes with time left, recent chat, commands, the audit log and About](docs/admin-menu.png)

Add the local library to your game, then one **Superuser Bootstrap** to its
network scene:

```text
notpointless.chomnr_sbox_superuser
```

*Engine support: verified on s&box 26.10.13a*<br>
*[Release notes](CHANGELOG.md)*

<br>

## Guide

Please see **[docs](docs/README.md)** for a tutorial, reference material, and
example code.

<br>

## Overview

Every request carries the engine's own record of who sent it. The host decides
what that player may do, records the attempt before acting, runs the command and
replies privately to the sender. Nothing about a player's authority is taken
from the message itself.

Superuser does not know what a "player" means in your game. Gameplay commands
come from your code, registered as modules with their own permission keys; the
library supplies routing, authorization, the audit trail and storage around them.
Two [sample games](dev/samples/README.md) with nothing in common, one built on
pawns and one on a roster, use it unchanged.

<br>

## Example

A command returns a result. Superuser checks the caller's permission and sends
the reply privately.

```csharp
[AdminCommand( Name = "ping", Description = "Reply with Pong!", Permission = "example.ping",
	ModuleId = "example.ping", ExecutionPolicy = CommandExecutionPolicy.ReadOnly )]
public static CommandResult Ping( CommandContext context )
{
	return new CommandResult( CommandStatus.Success, "Pong!" );
}
```

This method belongs to the [complete ping module](dev/fixtures/PingModule.cs).
Follow the [tutorial](docs/tutorial.md) to register it, then enter `/admin ping`
in the chatbox or `admin ping` in the console.

<br>

## Details

- **Commands** use one dispatcher for the console, platform chat, your own chat
  backend and the admin menu. Typed arguments, private replies, retries by
  request ID. [Reference](docs/reference/commands.md).

- **Permissions** are exact keys on groups and players, denied until granted.
  Owners are configured by the host; policy changes are owner-only, and owners
  cannot punish each other. [Reference](docs/reference/permissions.md).

- **Moderation** covers kick, ban, warnings, text mute and staff notes, with
  persistent history and original UTC expiry kept across restarts.

- **Audit** records every attempt before it runs and its outcome after. An
  outcome that cannot be confirmed is Unknown, never success.

- **Storage** is versioned and recoverable, local by default, replaceable with
  your own backend. [Reference](docs/reference/storage.md).

- **Prefixes** put a styled, animated tag in front of names, for groups or single
  players, in the chat box and anywhere your UI asks. Effects are short scripts
  written and previewed in the menu, with World at War presets. On by default.
  [Guide](docs/prefixes.md).

- **Modules** load themselves from the game's `Code/Superuser/Modules` folder,
  add their own windows to the admin menu, and get the same in-game documentation
  pages: overview, guides, generated commands and permissions. Two
  [examples](dev/examples/reports/README.md). [Reference](docs/reference/modules.md).

- **The `Superuser` facade** is the whole public API: dispatch commands, check
  permissions, register modules, select storage and request private admin
  views. [Reference](docs/reference/facade.md).

<br>

## Admin menu

Add **Superuser Admin Menu** on clients and press **F6**. Disable it to use your
own interface; console and chat keep working.

<table>
<tr><th>Window</th><th>What it does</th></tr>
<tr><td>Players</td><td>Search, warnings, the selected player's restrictions, actions, notes and recent chat</td></tr>
<tr><td>Bans &amp; mutes</td><td>Every active restriction across all players, with time left and revoke buttons</td></tr>
<tr><td>Chat</td><td>The last 100 public messages, kept in server memory only</td></tr>
<tr><td>Commands</td><td>Any permitted command, including your game's own, from a generated form</td></tr>
<tr><td>Activity</td><td>The audit log, filtered by command, actor and outcome</td></tr>
<tr><td>Permissions</td><td>Owners edit groups, members and permission entries</td></tr>
<tr><td>Prefixes</td><td>Create, style and assign prefixes; write effects in the built-in code editor</td></tr>
<tr><td>Documentation</td><td>Every module's guides, commands and permissions</td></tr>
</table>

Windows can be pinned to stay on screen while you play; layout and pins survive a
restart. [Reference](docs/menu.md).

<br>

#### License

Licensed under the [MIT license](LICENSE).

Bundled ImGui code keeps its own MIT license. See
[third-party notices](THIRD_PARTY_NOTICES.md).
