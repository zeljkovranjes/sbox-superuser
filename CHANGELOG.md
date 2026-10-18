# Changelog

## 2026-10-18
Title: Optional Admin Menu and Custom Storage
### Added
# Interface
Moderation menu - bundled ImGui view includes searchable players, offline Steam ID lookup, roles, restrictions, history, confirmations and private activity
Menu redesign - stock Dear ImGui look: Players and Activity sections on a top menu bar open movable windows; Players shows one table with the selected player below it, and a full Steam ID search opens offline players
Player shortcuts - right-click a player to copy their Steam ID or name or start a kick, mute or ban; recent reasons and the last duration are remembered per machine
About window - version, author, package and source link; owners also see installed modules and server status
Menu navigation - Superuser, Moderation and Server menus group the windows; open windows are checked
Bans & mutes - every active ban and text mute across all players, with Unban and Unmute that ask for a reason
Commands window - run any permitted command, including game module commands, from a form built from its arguments
Permissions window - owners edit groups, members and permission entries and check why a player has access; every edit is confirmed and audited
Activity filters - filter by command, actor and outcome, and page through older records
Player notes - private staff notes kept in moderation history through the new note command and admin.players.note permission
Warning counts - the player table shows each player's warnings to staff who can read history
Keyboard - Ctrl+F jumps to player search; Up and Down move through the player list
Recent chat - moderators see the last 100 public messages, kept only in server memory and never saved; needs the new admin.chat.read permission
Time remaining - active bans and mutes show how long they have left, such as in 2h 14m
Result pop-ups - finished actions appear briefly in the bottom-right corner
Replacement API - RequestAdminCommand and the Commands, Restrictions, Permissions and filtered activity queries
Pinned windows - a title-bar pin keeps a window on screen while the menu is closed; open windows, pins and positions are saved per machine and return after a restart
Replacement views - typed queries and exact-target actions return correlated private results without exposing ImGui or unrestricted console execution
Input ownership - configurable F6 binding and Escape close; removing the menu releases its panel and cursor while console and slash chat stay available
# Storage
Custom backends - trusted host setup can select storage before bootstrap; versioned writes, recovery, audit retention and unavailable outcomes keep their existing semantics
Storage tutorial - includes a runnable adapter and setup component, exact placement instructions and database implementation requirements
# Documentation
Integration guide - documents the optional menu, replacement view and dedicated/editor storage selection
README screenshot - the top of the README shows the admin menu in game
Documentation structure - the README and docs follow one contents page with concepts, a tutorial, running guides and reference chapters for the host API, console commands and each admin menu window
README - opens with the admin menu screenshot instead of the logo and ends with the menu overview and license; known limits now live in the release status and dedicated-server pages
# Identity
Project icon - adds a navy caped figure and shield as a scalable SVG, matching the Humanoid Retargeter logo style
# Validation
Measured limits - actual dedicated pressure and persistence workloads now have a recorded environment, timings and bounded-capacity evidence
Security checks - repeated two-client privacy and injected storage failures confirm private errors and no disconnect after a failed ban write
# Integrations
Independent games - executable arena and colony examples map stable player IDs to different gameplay representations without changing the library
Optional capabilities - samples demonstrate default-denied permissions, explicit grants and Unsupported results when game services are absent
# Console
Operator commands - native admin help, access management, diagnostics and private audit queries use the authoritative dispatcher
Configured roots - bootstrap supports a logical root with a fixed native fallback and reports visible managed-name collisions
Private replies - authenticated remote console results go only to the requesting connection; retries retain one correlated audit record
Recovery diagnostics - local audit health remains inspectable when command auditing cannot write
# Modules
Managed lifetimes - validates dependencies and API versions, stages startup atomically and cleans up tracked subscriptions
Owner controls - module enable and disable retain stored data and protect required security services
Bounded shutdown - removes entry points immediately, signals cancellation and prevents restart while old work remains active
Scene transitions - dedicated scene replacement retires old authority, removes listeners and preserves policy and moderation records
Source reload recovery - structural replacement retires runtime authority and requires a fresh session; stop before editing source because method-only reload does not reliably trigger this guard
Removable sample - verifies repeated module reload and failure rollback in the editor and an actual dedicated server
# Chat
Slash commands - platform chat consumes exact /admin commands before dispatch and sends results only to the requester
Private help - chatbox help shows readable command details and pagination using the platform overlay
Chat validation - two real clients verify ordinary-chat pass-through, private errors and one attributed request per submission
Custom chat hooks - independent backends consume commands before broadcast and receive authenticated private results through a scene event
Chat examples - compiled platform and independent chatboxes demonstrate private feedback, request retries and ordinary-message checks
Result fallback - disabled views unsubscribe automatically and failing observers preserve the platform notification fallback
Unavailable sessions - enabled eligible bootstrap keeps player chat private while administration is unavailable
# Settings
Explicit bindings - allowlisted native settings and commands use typed permissions and retain native restrictions
Setting commands - list, get, set and reset report actual native values with verified write readback
Persistence ownership - engine Saved variables stay engine-owned; framework values survive dedicated restart with distinct pending state
Failure recovery - native clamping and interrupted storage writes preserve the previous committed setting and report unconfirmed outcomes honestly
# Moderation
Actions - kick, ban, warnings, text mute and private history use current permissions and target protection
Admission - committed bans reject connections before gameplay; unavailable moderation history fails closed
Text mute - platform and independent chat enforce restrictions while slash commands retain private feedback
Persistent history - UTC expiry survives dedicated restart and linked revocations retain original records
Failure handling - failed ban writes leave players connected; explicit backup recovery restores history without resetting sanctions
### Improved
# Documentation
Getting started - short Markdown chapters cover setup, a working ping command, custom chat and core APIs
Navigation - replaces implementation history and task labels with a linked guide and concise example-first README
Examples - verifies the guide snippets in a fresh game, including native dedicated ping and private client chat
# Audit
Completion checks - verifies attributable Unknown outcomes across an actual dedicated-server interruption and restart
Validation - all 314 contract and Core tests pass and the actual engine references accept the library
### Fixed
# Security
Owner protection - group ranks stop at 99 so a policy-editing owner cannot outrank and punish another owner
Private results - command messages and fields reject control characters that could forge extra console or chat lines
Reply routing - remote console replies require the original connection to remain in the live connection set
# Interface
Open key - pressing it again while the menu is open now closes the menu, unless you are typing in a text field
Escape - cancels a pending confirmation before it closes the menu
# Documentation
Logger examples - uses the engine's global Log in sample snippets so they compile in the adopting game

## 2026-10-17
Title: Durable Administration Audit
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
# Authority
Policy administration - only current owners and dedicated console authority may persist user, group and protection edits
Target protection - uses stored offline roles, refuses equal ranks and punitive self-targeting, and keeps permissions separate
Owner recovery - dedicated-local owner changes retain versions, protect the final owner and require explicit recovery to clear all owners
Degraded authority - corrupt managed ownership never falls back to bootstrap or cached owners; storage recovery invalidates permission caches
# Commands
Registration - validates module-owned commands and aliases atomically; replacement and disposal revoke old entry points
Discovery - supports public static typed handlers through TypeLibrary with explicit registration fallback and a game capability example
Metadata - validates argument order, numeric bounds, enum choices and explicit player target policies before registration
# Parsing
Arguments - accepts bounded quoted text and invariant typed values while rejecting malformed, extra and oversized input
Player selection - exact IDs remain authoritative; duplicate display names return choices without selecting a target
Syntax help - derives required, optional and remainder arguments plus constraints from command descriptors
# Dispatch
Execution - authenticates live callers and rechecks queued permission, registration and target validity before invoking handlers
Queue and retries - bounds outstanding work and deduplicates actor-scoped request IDs without repeating mutations
Cancellation - distinguishes unstarted cancellation from interrupted mutations with Unknown outcomes and safe errors
Capabilities - scopes approved game services to modules and supports explicitly limited, revocable internal callers
# Audit
Durable outcomes - requires a persisted intent before execution and preserves Unknown when an action has no confirmed outcome
Private records - provides permission-filtered pages and exact filters with argument redaction and sensitive chat-command refusal
Retention - owners configure record count and age; bounded cleanup preserves two validated generations and resumes after interruption
Health - reports degraded audit storage and prevents actions whose required intent cannot be persisted
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
Storage retention - policy and owner backups remain retained; damaged historical audit files require operator review and are never silently erased
