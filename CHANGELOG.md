# Changelog

## 2026-10-17
Title: Compatibility Probe
### Added
# Diagnostics
Compatibility probe - checks type discovery, caller identity and argument forwarding inside the library
Editor testing - enable Allow Editor Testing to exercise the dedicated-server integration without a server
Integration checks - includes opt-in storage markers, targeted chat replies and test-account admission rejection
# Development
UI source staging - reserves dev/ui-source/sbox-imgui for the selected future UI
Compatibility report - records observed engine behavior, adapter decisions and reproducible checks
### Known Issues
# Validation
Dedicated servers - runtime identity, two-client privacy, admission ordering and durable storage still need verification
Administration - Task 1 provides diagnostics only; administration commands and UI are not implemented
