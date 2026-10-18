# T02: Library foundation and public contracts

Status: **In progress**. Scope is data contracts, interfaces and structural invariants only. T03 onward remain not started.

Public integration types live in `Code/SboxSuperuser/Api/`, using the root `SboxSuperuser` namespace per repository conventions. They are engine-independent boundary types, not a callable administration service. Internal structural rules live in Core by feature; Engine holds transport/invocation boundaries. Future Core systems must not depend on Api or Engine: adapters translate public contracts to internal domain inputs.

The foundation will describe commands and arguments, host-issued context and identity, explicit results, permissions, module ownership/dependencies, and versioned host-local storage. No dispatcher, authentication issuer, permission resolver, parser, module loader, store implementation or chat handler is introduced. Task 1 probes remain unchanged.

Task 1 decisions retained: actual dedicated host in production, explicit editor opt-in, no null-caller trust, TypeLibrary discovery with explicit registration fallback, host-local FileSystem.Data with recoverable generations rather than an assumed atomic API, and `/` for chat commands. No external executables/services or game player/controller types may enter these contracts.
