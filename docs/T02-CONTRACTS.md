# T02: Library foundation and public contracts

Status: **Complete**. Scope is data contracts, interfaces and structural invariants only. T03 onward remain not started.

Public integration types live in `Code/SboxSuperuser/Api/`, using the root `SboxSuperuser` namespace per repository conventions. They are engine-independent boundary types, not a callable administration service. Internal structural rules live in Core by feature; Engine holds transport/invocation boundaries. Future Core systems must not depend on Api or Engine: adapters translate public contracts to internal domain inputs.

The foundation describes commands and arguments, host-issued context and identity, explicit results, permissions, module ownership/dependencies, and versioned host-local storage. No dispatcher, authentication issuer, permission resolver, parser, module loader, store implementation or chat handler is introduced. Task 1 probe source and its editor exercise remain byte-for-byte unchanged from `2c69ca7`.

Task 1 decisions retained: actual dedicated host in production, explicit editor opt-in, no null-caller trust, TypeLibrary discovery with explicit registration fallback, host-local FileSystem.Data with recoverable generations rather than an assumed atomic API, and `/` for chat commands. No external executables/services or game player/controller types may enter these contracts.

## Exported contract map

All types below use `using SboxSuperuser;`. Public members have XML summaries. There is no callable administration facade until an actual runtime exists.

| Area | Types | Meaning and ownership |
|---|---|---|
| Command metadata | CommandDescriptor, AdminCommandAttribute, CommandExecutionPolicy | Stable key, help, exact permission, aliases, owning module, ordered parameters and read-only/mutation policy. Attribute metadata does not create a native command. Mutation is the conservative descriptor default. |
| Argument metadata | CommandArgumentDescriptor, CommandArgumentKind, CommandTargetPolicy | String, integer, number, boolean, enum, duration and explicit player values; optional/remainder markers, numeric bounds, enum choices, online/punitive target requirements. |
| Parsed arguments | CommandArguments, CommandDuration, PlayerReference | Internal construction for parsed bags; exact typed getters throw for missing/type mismatch. Values are string, Int64, Double, Boolean, enum, duration or stable player reference. Permanent duration is explicit; finite duration must be positive. Player reference is a canonical nonzero decimal Steam ID, not an online player/controller. |
| Request and identity | CommandRequest, ActorIdentity, ActorKind, CommandOrigin | Request carries only command, tokens and retry ID. Identity distinguishes Player, DedicatedConsole and InternalService. No client-authorable owner/role fields. Identity/context have internal constructors; T03 must authenticate before issuance. Constructor access is API hygiene, not a trust boundary against hostile host code. |
| Execution context | CommandContext, ICommandServices, CommandHandler | Actor, origin, request ID, cancellation and explicitly approved optional game capabilities. Services return null when absent. Explicit handler delegate returns Task of CommandResult; synchronous attributed handlers may return CommandResult when T07 is implemented. |
| Outcomes | CommandResult, CommandStatus | Explicit status, caller-safe message, optional string-keyed structured fields. Unknown is zero/default. Private reply is the future default; no result constructor sends a reply or executes an action. |
| Permissions | PermissionDescriptor, PermissionEntry, PermissionEffect, PermissionContribution, PermissionExplanation | Exact keys and allow/deny/unset values; final decision, direct effect, contributing groups/deny sources, policy revision and explicit owner bypass. No precedence, inheritance, ownership issuance or protection evaluation is performed. |
| Modules | ModuleDescriptor, ModuleDependency, IAdminModule, IModuleRegistration | Stable ID, positive API version, dependencies and required-security marker. Dependency API compatibility means exact integer version; package releases are separate. Host-owned registration scopes enforce ownership and revoke contributions in T12. Start/Stop are declarations only. |
| Storage | IAdminStore, StoreKey, VersionedStoreDocument, StoreWriteRequest, StoreReadResult/Status, StoreWriteResult/Status | Logical module/document key, schema version, revision, host-private JSON aggregate and expected revision. Missing differs from Corrupt/UnsupportedSchema/Unavailable. Only Committed carries a confirmed positive revision. |

## Folder and dependency decisions

`Api/` is flat and uses the root namespace as required by this repository. `Core/Contracts` and `Core/Identity` contain only structural guards. `Engine/Commands`, `Engine/ChatIntegration`, `Engine/Permissions` and `Engine/Modules` declare internal adapter boundaries with no implementations. ChatIntegration avoids shadowing the engine's Platform.Chat class. Existing `Engine/Compatibility` and `Components` retain T01 diagnostics.

Future Core command/policy/module/storage logic will be added in feature folders when it exists. Empty folders, speculative runtime classes, duplicate public/core models and success-returning placeholder services are deliberately absent. Outer adapters will translate these public boundary types to Core inputs; Core cannot reference Api, Engine, Components or game-specific services.

The extra `dev/SboxSuperuser.Contracts.csproj` compiles the exact Api/Core/internal-boundary sources against plain .NET with no engine reference. It is a development harness, never a second shipped library. Its friend assembly is emitted only for tests; shipped code exposes no friend access. The original Core harness and original test project are unchanged.

## Foundation guarantees and deferred validation

Constructors check nonempty text, canonical lowercase dotted keys, valid enum values, finite ordered numeric bounds, positive durations/API/schema versions, canonical Steam IDs and storage revision consistency. Collections are defensive read-only snapshots. They do not authenticate input, parse commands, validate JSON schemas, resolve target names or authorize actions.

T07/T08 must still validate alias collisions, handler signatures, argument-kind/bound compatibility, optional-argument ordering, final remainder placement, enum choices, request quotas and actual parsed values. T09 must match request/context IDs, deduplicate by authenticated actor/origin/ID, recheck authorization and serialize mutations. No constructor should be treated as proof that these later checks ran.

IAdminStore has no implementation. Its future adapter must serialize writes, check the expected prior revision (zero means truly missing), validate payload JSON/schema and retain a previous readable generation. The candidate must be exactly expected revision + 1. A document is an aggregate: required policy/punishment changes and audit intent must be persisted together inside that aggregate. Cross-document transactions are not promised. Unknown covers interrupted/indeterminate writes; cancellation is not a rollback guarantee. No fsync or power-loss guarantee is made.

No native settings binding, chat bootstrap, custom UI, permission mutation, group model implementation, module loader or store is introduced. These remain T03–T17 work as scheduled; T18–T20 examples/release hardening are also untouched.

## Validation record

Engine **26.10.13a**, installed Steam build **25964806**, 2026-10-17. The isolated adopting game mounted the same library checkout.

- Engine whitelist compiler: library and adopting game succeeded with zero diagnostics.
- `dev/verify-contracts.cs` executed inside s&box using exported types and produced `[Superuser T02] PASS`; it constructs metadata and checks outcomes without creating runtime systems.
- `dev/verify-editor.cs` replayed T01 default-off, disabled, duplicate, quoted/zero/too-many argument and local-entry observations. Discovery, native settings and existing storage readback also passed on editor startup.
- Generated runtime project built with zero warnings/errors. New engine-free contract suite passed **20 tests**, covering malformed permission/identity values, immutable snapshots, authority-free payloads, typed argument access, duration semantics, explicit outcomes and storage revision/failure invariants.
- The original test project still has zero test cases; its command completed, with no fabricated pass count. No existing tests were modified.
- Dedicated multiplayer remains Blocked in the T01 record due to the unpublished adopting game's package lookup. That deployment is not required to prove T02's contracts or editor whitelist compilation; no T02 blocker is currently known.

Reproduce with `sbox-check`, runtime `dotnet build`, and `dotnet test` for both test projects. Open the isolated game and use `sbox-cli --connect <pid> --call run_csharp` with the absolute path to `dev/verify-contracts.cs`; play its scene before running `dev/verify-editor.cs`. Capture compile_status and read_console. The development CLI is never invoked by the library.

## Completion audit

| Requirement | Final evidence |
|---|---|
| Structure and requested contract areas | Exported contract map matches Api source; internal feature boundaries and Core guards exist. No empty feature folders or fake runtime implementations. |
| One library and game independence | PackageReferences remains empty; runtime stays under Code/SboxSuperuser. Exact contracts compile in plain .NET without engine/player dependencies. Dev harness is excluded from shipped runtime. |
| Actual s&box compilation | Final compile_status: library and adopting game succeeded with zero diagnostics; exported constructors ran in-engine and logged PASS. |
| Preserve Task 1 | Baseline diff against 2c69ca7 is empty for probes and existing editor script; runtime replay passed and final active probe count was one. |
| Validation and conventions | sbox-check and runtime build passed; 20 new tests passed; original empty test project untouched. Publication lint: zero failures, one ignored generated engine XML warning. No generated binaries tracked. |
| Documentation and scope | README, metadata, GitHub description/topics, changelog and this record agree on foundation-only support. No Task 3 work. No Task 2 blockers. |

The owned validation editor was stopped; the user's existing editor was left running. Changes are committed to the private repository. T01's dedicated multiplayer limitation remains unchanged.
