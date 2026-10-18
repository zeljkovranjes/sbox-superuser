# Release status

**0.1.0-rc.1 — unpublished source candidate, preparation complete.** Package
identity: `notpointless.chomnr_sbox_superuser`. Module API: **1**. Repository
visibility: private.

The candidate includes console and chat commands, permissions, explicit
ownership, moderation, native bindings, module lifecycle, host-local storage and
audit. It requires no external runtime service or executable.

## Installation and examples

Verified on 2026-10-18 with engine 26.10.13a. A fresh library checkout at commit
`2221a47` was mounted by a new, empty-data game, `superuser-docs-example`;
runtime source is unchanged in the final candidate. The current ping
module/setup and exact Markdown snippets were compiled into that game.

- Editor owner setup reached Ready and native `admin ping` returned Pong with a
  successful Player audit record.
- Saving Allow Editor Testing through the inspector and starting a fresh Play
  session registered ping automatically.
- A real remote client received private Pong replies through platform chat and
  the authenticated custom-chat hook. Both recorded the actual client identity
  and Chat origin.
- All eight C# blocks in the README, guide and sample READMEs match compiled
  source or the generated engine fixture. Registration, both diagnostics calls,
  overlay control and chat routing executed successfully. The initial logger
  typo was corrected to `Log.Info`.
- Actual dedicated server19968 started with editor testing off. Native ping and
  diagnostics succeeded as DedicatedConsole with no player owner configured.
  Their completed outcomes were persisted.

The generated example build and engine whitelist check reported zero
warnings/errors. All 314 contract/Core tests pass after the security regression
additions. Markdown links and task-label checks pass through `python
dev/check-docs.py`. The fixed workspace README-template check reports the
intentional outline change requested for this repository; its checker is
unchanged. The fresh example game's standards check passed; its empty test
project contributes no extra test count. Its local verification commit is
`0fa4c37`.

Testing found that new games can default platform chat to off. Setup now
explicitly enables it before launch. Test owner data is archived, the saved
editor switch is restored to false, and owned test clients/servers are stopped.

Dedicated-client deployment still needs a valid adopting-game package record on
the tested engine. Dedicated host/console/restart checks and editor two-client
checks passed; this is not a certified multiplayer production release. No
package, GitHub release or public tag has been published.

Voice moderation, player-hosted production and host migration remain deferred.
ImGui source has a [staging folder](../dev/ui-source/sbox-imgui/README.md). No
redistribution license has been selected.

See the [changelog](../CHANGELOG.md) for changes and [validation](validation.md)
for evidence.
