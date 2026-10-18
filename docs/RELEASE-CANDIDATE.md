# Superuser 0.1.0-rc.1 — unpublished candidate

Status: **Preparation in progress.** This label identifies a source candidate, not a published sbox.game version. Package identity remains `notpointless.chomnr_sbox_superuser`; module contract API version is 1. No package, release or public tag is created by this work.

## Candidate contents

One library contains authentication, explicit ownership, exact permissions, typed commands, bounded dispatch, durable host-local storage/audit, managed modules, native allowlisted settings, platform/custom chat and moderation. Production requires an actual dedicated server. `AllowEditorTesting` defaults false and enables only editor-host testing. Chat commands use `/`.

Start with [setup](SETUP.md), [command reference](COMMANDS.md), [API version 1](API-V1.md) and [backup/recovery](RECOVERY.md). [Two executable samples](../dev/samples/README.md) use different player representations and the same library. [T19](T19-VALIDATION.md) records security, recovery and measured workload evidence; the [task ledger](IMPLEMENTATION.md) links each earlier task.

## Release restrictions

Dedicated remote-client deployment has not been certified: the tested engine needs a package record for the unpublished adopting game. Actual dedicated startup, console, storage, restart and scene lifecycle passed; actual two-client authentication, chat privacy and admission tests used opt-in editor hosting. Do not interpret the prepared candidate as a passed dedicated-multiplayer production release gate.

Arbitrary live source edits, player-hosted releases, host migration, voice mute, visual admin UI, dashboard, external services, implicit native-role import and dynamic native command registration remain unsupported or deferred. Future ImGui source has an isolated staging folder; it is not a dependency. Storage offers close/readback and tested interruption recovery, not power-loss durability. Only one host process may write a physical data root. No redistribution license has been selected; repository access remains private.

## Installation evidence

Pending a fresh source checkout and clean adopting-game run using SETUP.md. T20 will record exact source commit, generated builds, engine whitelist result and successful authorized native command here. Publication remains a separate user action.
