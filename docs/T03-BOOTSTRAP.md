# T03: Trusted bootstrap and identity

Status: **Implemented; final audit pending**. Only bootstrap, identity, explicit owner configuration and diagnostics are in scope. T04 onward remain not started.

Design: one scene-owned system coordinates bootstrap registrations. Only the active, authoritative network scene can own a usable runtime; scene transitions retire older runtimes. Duplicate components fail closed. Production requires an actual dedicated server; editor testing remains explicit opt-in.

Player identities come from live engine connection objects, never supplied Steam IDs or roles. DedicatedConsole is issued only inside a separate non-forwarded native command on a dedicated host; null on the shared player path is rejected. Owner IDs load from a fixed host-local file, with a separate editor file, strict validation and no automatic host/claim/group promotion. No store writer, permission resolver or dispatcher is implemented.

## Installation and explicit ownership

1. Reference this one library in an adopting game and add exactly one **Superuser Bootstrap** to the active scene. The game must establish its engine network session. The bootstrap does not start a server or lobby.
2. Dedicated hosting works with Allow Editor Testing off. For a network session hosted in the editor, enable that switch. Release listen servers and clients remain unsupported. To apply an option change, disable/re-enable the component.
3. In the adopting game's FileSystem.Data directory, create `superuser/owners.json` for dedicated hosting or `superuser/editor-owners.json` for editor testing. Begin with [owners.example.json](../dev/fixtures/owners.example.json) and add your intended owner Steam IDs as decimal strings. An empty array explicitly grants nobody:

```json
{ "schemaVersion": 1, "owners": [] }
```

4. Reload the scene or disable/re-enable bootstrap after changing the file. Configuration is read once per runtime; no live file watcher or persistence writer is installed. Missing/unreadable/corrupt/unsupported configuration grants no owners, preserves the existing file and reports diagnostics. No fallback owner is inferred.
5. Run `superuser_status` locally. It prints health, runtime ID, registration count, owner count and selected configuration path, never the owner list. `superuser_identity` records the authenticated player on the host and accepts no arguments. `superuser_local_identity` runs only through the non-forwarded entry on an actual dedicated runtime; it is refused in the editor and on clients.

The saved [bootstrap.scene](../dev/fixtures/bootstrap.scene) and LabSession are development fixtures, not a second library or external service. The editor and dedicated data directories remain separate in the existing lab.

## Public API and trust boundary

```csharp
using SboxSuperuser;

var health = Superuser.GetDiagnostics();
// authenticatedConnection must come from the engine's host-side callback.
if ( Superuser.TryGetPlayerIdentity( authenticatedConnection, out var actor ) )
{
	var explicitlyConfiguredOwner = Superuser.IsOwner( authenticatedConnection );
}
```

The façade does not accept Steam IDs, roles or ActorIdentity objects as proof of identity. It requires the exact active connection object in the engine's current connection list, rechecks host/runtime eligibility, and reads the engine-provided Steam ID. Null, disconnected, unrelated and non-player/SteamId 0 connections fail. Returning a Player identity does not grant owner membership. IsOwner performs a fresh connection check and an exact match against validated host configuration.

The dedicated host's native forwarded-console connection has SteamId 0 on this build; it is deliberately rejected on the player route. A separate local ConCmd entry, guarded by actual dedicated runtime checks, issues DedicatedConsole internally. There is no public console-identity factory, remote console-role argument or null-caller shortcut. InternalService issuance is not exposed until explicit service capabilities exist; no service gets implicit root access.

Approved host code and the server operator remain trusted. Private constructors and this façade are not a sandbox against malicious C# already running on the host. Client requests/scene properties carry no owner configuration; engine claims and group names are not imported. No custom RPC accepts actor kind or roles.

## Runtime lifecycle

`SuperuserBootstrap` only attaches/detaches its component ID and testing option. The scene-owned `SuperuserSession` maintains an idempotent registration set and one engine lifecycle callback. Reattaching the same ID does not create another runtime. Two enabled IDs suspend identity/owner access and dispose the runtime; removing the duplicate recovers one runtime and rereads explicit configuration.

Eligibility requires the active scene, an active authoritative engine session, an active host connection, and dedicated mode or explicit editor testing. Each public access rechecks these conditions. The runtime also binds the host connection object so stale session references cannot keep resolving identities. Disabled components, lost authority, scene retirement and system disposal revoke the runtime. A transition starts its replacement only after retiring other scene runtimes. No mutable static session registry or repeated per-frame component scan is used.

This build's Game.ChangeScene reuses its Scene/system container while replacing components. The test therefore checks the captured old runtime's disposed flag, a new runtime ID, and exactly one non-null runtime across all scenes; it does not assume a new Scene object. Three actual dedicated scene replacements passed.

T03 installs no platform-chat listener, dispatcher or module registrations, so it cannot duplicate those future systems. The one-listener integration belongs to T13. T01's optional probe listener remains diagnostic-only and should be removed from production scenes.

## Owner-file validation

Core validation accepts exactly schemaVersion 1 and an owners string array, with at most 128 unique canonical nonzero decimal IDs. Unknown/duplicate properties, duplicate IDs, non-string IDs, malformed JSON, unsupported schema and oversized files fail closed. Read size is bounded to 64 KiB; no owner file is written or reset by runtime code. Ready with zero owners is a valid explicit configuration; OwnersMissing and OwnersInvalid remain distinct health states. Identity diagnostics and the trusted local-console route remain available for diagnosis/recovery, without silently granting player ownership.

Versioned administrative storage, recoverable writes, owner management commands and migrations remain T04 and later. This read-only bootstrap file is not presented as an IAdminStore implementation or a durability guarantee.

## Verification record

Engine **26.10.13a**, Steam build **25964806**, Windows, 2026-10-17. The existing isolated adopting game references this checkout.

| Check | Observed evidence |
|---|---|
| Actual dedicated startup | One runtime with editor switch off; correct dedicated owner-file path. Null and host SteamId 0 rejected as players. Non-forwarded local command reported DedicatedConsole. |
| Editor bootstrap | Default-off refused; explicit opt-in initialized one runtime. Host player remained non-owner without explicit configuration. |
| Explicit owners | Editor's authenticated Steam identity became owner only after its ID was configured. Actual dedicated loaded one explicit owner; its SteamId 0 host connection remained non-owner. Invalid configuration in both modes granted zero owners and was preserved. |
| Client identity | Real engine client connected to editor host; host logged its authenticated connection and Steam ID as Player. Client-supplied DedicatedConsole/owner arguments were refused. Client-side façade returned Unsupported/false even after enabling its editor-test property. |
| Stale caller | Retained remote connection resolved while active; after the test host disconnected it, identity and owner checks both returned false. |
| Duplicate/repeated bootstrap | Repeated OnStart kept runtime ID. A second bootstrap cleared runtime and owner access. Removing it restored exactly one registration/runtime. |
| Scene lifecycle | Three actual Game.ChangeScene replacements on dedicated disposed captured old runtimes and left one new runtime each. |
| Earlier tasks | T01 probe source and T02 contracts/existing tests preserved; original diagnostics continue running in the fixture. |

Reproduce using `dev/verify-bootstrap.cs` in the isolated editor-host scene, `dev/verify-dedicated-owners.cs` on dedicated, and `dev/verify-bootstrap-scenes.cs` on dedicated. These trusted development scripts temporarily change only the isolated owner's file, restore it in finally, and/or replace the test scene. They are not shipped runtime code. Raw local evidence is under ignored `dev/out/evidence/t03-*`.

The contract test project now includes 13 additional owner/registration cases, alongside the 20 unchanged T02 tests. Engine compile results, final builds and the completion audit are recorded below when finished.

## Limits

Dedicated remote-client loading is still blocked by the unpublished adopting game's package record, as established in T01. Remote transport evidence here comes from a real editor-host client; dedicated startup/local-console/configuration/lifecycle evidence comes from the actual dedicated process. This is not a full production multiplayer security certification. No Task 3 implementation blocker is known. Task 4 has not started.
