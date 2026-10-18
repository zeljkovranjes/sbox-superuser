# T14: Custom chat integration

Status: **In progress.** Adds a host-only consumed-message hook, a scene-owned private-result event, an ordinary-message moderation check, and two runnable integration examples. Custom UI over platform chat uses only `Chat.Say`; an independent backend authenticates its RPC sender and calls the hook before routing or broadcasting. Neither path introduces another executable, service or required UI library.
