# T06: Target protection and policy administration

Status: **In progress.** T05 is complete; continue through T10 and stop before T11.

Implement stored offline protection, strict rank comparison and self-target rejection, host-authenticated owner-only policy changes, reserved owner handling and explicit local recovery. Persist state with audit intent before publishing a policy revision. Existing bootstrap owner configuration remains the default; managed owner overrides require an explicit dedicated-local operation and must never fall back after corrupt managed data.
