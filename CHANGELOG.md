# Changelog

## 2026-10-17
Title: Library Foundation
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
### Improved
# Diagnostics
Chat probe - uses /superuser-probe in the chatbox; nonmatching messages pass through normally
Compatibility evidence - verifies actual dedicated startup and console identity, native settings and previous-file preservation across process interruption
Network evidence - verifies caller identity, private replies and admission rejection with two real clients in editor test mode
### Known Issues
# Validation
Dedicated multiplayer - the unpublished test game cannot load on joining clients; a published adopting game is needed for dedicated network validation
Storage guarantees - interruption preserves the previous probe file; full recovery and power-loss guarantees are not implemented
Administration - Task 1 provides diagnostics only; administration commands and UI are not implemented
Foundation - Task 2 provides contracts only; bootstrap, dispatch, policy resolution, parsing, module management and storage remain assigned to later tasks
