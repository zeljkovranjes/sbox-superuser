# T18: Two adopting games

Status: **Runtime checks complete; final checks pending.** Arena represents a player with a scene component and health. Colony represents a player with a scene-owned supply record and no pawn component. Both reference the same library; all its runtime source, including Core, is unchanged from T17. Original executable sample sources live under `dev/samples`, outside shipped runtime code.

## Executable integrations

See the [developer quick start](../dev/samples/README.md). Each sample includes its own `.sbproj`, editor-authored scene, game adapter, capability interface and gameplay representation. The only library reference is Superuser. Commands are explicit registrations, scoped game services are optional, and disabling the game administration component disposes its registrations. No external executable/service is required at runtime beyond s&box itself.

| Sample | Representation | Game behavior | Evidence |
|---|---|---|---|
| `superuser-arena-sample` / `SuperuserArena` | `ArenaPawn` scene component, host-assigned stable Steam ID | `arena.heal` restores health 25 → 100 | Editor 13108; dedicated 24652 |
| `superuser-colony-sample` / `SuperuserColony` | `ColonyRoster` scene system, dictionary of supply records; no pawn component | `colony.supply` replenishes supplies 3 → 50 | Editor 27540; dedicated 5168 |

## Observed results

Engine 26.10.13a on Windows, 2026-10-18. Both games compiled independently against the same local library with actual engine whitelist validation and zero diagnostics. Local project roots are under `E:/.sbox/03-projects/`; only original sample sources are copied to the library's development folder.

Both actual editor-host scripts passed: custom permission denied by default; native console invoked the real game effect; owner granted an exact permission; after bootstrap ownership was removed, the same actual player succeeded through that persisted grant. Restoring owner configuration allowed cleanup of the grant. Re-registering without the capability returned Unsupported. Native warning, ban, mute, unban and unmute produced five retained moderation rows. Results and audit actor attribution were checked through the public query API.

Both actual dedicated scripts passed: real native commands changed the independently represented game state and returned Unsupported without the capability. Three native access edits created a group, granted the game permission and assigned an offline fixture user. Warning, ban and mute committed moderation revision 3. The dedicated tests deliberately create an offline pawn/roster record for ID `76561198000000071`; this is game-state setup, not a simulated authenticated connection or a dedicated-client test.

The library runtime diff against T17 commit `f2e5973` is empty. These examples use only its published C# surface for integration. Trusted verification scripts inspect internal dedicated audit state through local development reflection; that reflection is not part of either game or the shipped library.

## Preservation and limitations

Both fresh game data roots were backed up before launch: `superuser-t18-arena/20261018-021702.zip` and `superuser-t18-colony/20261018-021703.zip` under `E:/06-um-data/backups` (empty original roots). Saved scene editor opt-in remains false; verification switches and test owners are runtime/disposable data only. These examples are not complete games, persistent gameplay systems, replicated player demos or visual admin UI. Dedicated-client deployment retains the T01 package-record limitation. No release player-host or migration support is claimed.

Initial commits: `77be705`, `09a4395`, `6c64321`, `830d543`, `2d78c26`. Next task after final checks: T19 security, recovery, privacy, concurrency and declared load validation.
