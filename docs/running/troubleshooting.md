# When something fails

| Result | Check first |
|---|---|
| Disabled / Unsupported | An active network session, one enabled bootstrap, and a dedicated host or the editor option |
| Denied | The right owner file and Steam ID; restart after editing it |
| NotFound for your command | The module was registered after the bootstrap became ready |
| DuplicateBootstrap | Remove the extra bootstrap |
| StorageUnavailable, or degraded health | [Inspect and recover storage](../reference/recovery.md) |
| RestartRequired | Stop and restart the session before continuing |
| Unknown | The outcome could not be confirmed; check Activity before retrying |

If platform chat does nothing, check **Project Settings > Platform > Chat
Enabled**. New games can default it to off.
