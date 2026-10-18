# T01: Engine compatibility

Status: In progress. Scope: T01 only; T02–T20 remain Not started.

The source specification is [version 0.2](sbox-admin-specification.docx). The user narrowed production support to actual dedicated servers and requested an explicit editor testing switch. Player-hosted releases and host migration are unsupported. Editor results never establish dedicated-server compatibility.

All probe code runs inside the single `notpointless.chomnr_sbox_superuser` library. The editor, CLI, .NET SDK and Git are development tools, not runtime dependencies. No companion process, service, installer or database is part of the library.

## Selected environment

2026-10-17, Windows, Steam app 590830, installed build ID **25964806**. Engine API evidence comes from the installed `Sandbox.Engine.xml`, `Sandbox.Reflection.xml`, `Sandbox.Filesystem.xml` and `Sandbox.System.xml`. Runtime observations and reproducible steps will be recorded below before closing T01.

## Task boundary

This task adds disposable compatibility checks, not the administration framework. No actor contracts, permission resolver, dispatcher, module manager, persistent administration store or moderation commands are implemented. Native claim checks are observations only; they never grant framework authority. A missing caller is not a trusted-console credential.
