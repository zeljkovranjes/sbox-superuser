# Changelog

## 2026-10-17
Title: Permission Resolution And Recoverable Storage
### Added
# Diagnostics
Compatibility probe - checks type discovery, caller identity and argument forwarding inside the library
Editor testing - enable Allow Editor Testing to exercise the dedicated-server integration without a server
Integration checks - includes opt-in storage markers, targeted chat replies and test-account admission rejection
# Development
UI source staging - reserves dev/ui-source/sbox-imgui for the selected future UI
Compatibility report - records observed engine behavior, adapter decisions and reproducible checks
Dedicated test fixture - includes an adopting scene and scripts for process interruption and invalid-file checks
# Foundation
Public contracts - describe commands, typed arguments, actors, context, results, permissions, modules and versioned host-local storage
Integration boundaries - separate untrusted command payloads from host-issued context without installing runtime systems
Contract checks - verify immutable snapshots, explicit outcomes and invalid input rejection in plain .NET and s&box
# Bootstrap
Runtime - add one Superuser Bootstrap to initialize an authoritative dedicated session or explicit editor test
Identity - resolves live engine players and keeps trusted local dedicated-console callers on a separate route
Owners - loads explicit host-local IDs with separate editor configuration; invalid or missing files grant no owners
Diagnostics - reports runtime health, duplicate bootstraps and caller identity without accepting role arguments
# Storage
Versioned transactions - persists policy state and Unknown audit intents together before confirming a committed revision
Backups - retains immutable generations with integrity checks and explicit schema validation
Recovery - dedicated local console can inspect compatible backups and restore a selected version without deleting corrupt evidence
Host isolation - separates editor and dedicated data and refuses retired runtime handles
# Permissions
Policy evaluation - resolves exact permissions with direct user precedence, inherited deny and explicit default denial
Inheritance - validates immutable group graphs, rejects cycles and includes implicit player membership
Explain - records contributing groups and policy revisions; policy and permission registration changes invalidate cached decisions
### Improved
# Diagnostics
Chat probe - uses /superuser-probe in the chatbox; nonmatching messages pass through normally
Compatibility evidence - verifies actual dedicated startup and console identity, native settings and previous-file preservation across process interruption
Network evidence - verifies caller identity, private replies and admission rejection with two real clients in editor test mode
# Bootstrap
Scene lifecycle - repeated initialization stays idempotent; duplicates suspend access and scene replacement retires previous runtimes
### Known Issues
# Validation
Dedicated multiplayer - the unpublished test game cannot load on joining clients; a published adopting game is needed for dedicated network validation
Storage guarantees - actual process interruption and restart recovery are verified; power-loss durability and cross-process writers are unsupported
Administration - Task 1 provides diagnostics only; administration commands and UI are not implemented
Administration runtime - live policy administration, dispatch, parsing and module management remain later tasks
Storage retention - retains all committed generations; automatic pruning and audit outcomes remain later tasks
