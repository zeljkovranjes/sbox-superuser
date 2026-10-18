# T05: Permission resolver and groups

Status: **In progress.** Tasks 1–4 remain complete. The current work order is T05 through T10, stopping before T11.

Implement exact registered permissions, immutable user/group policy, implicit player membership, multiple inheritance with cycle rejection, direct-user precedence, inherited-deny precedence, default deny, revision-aware cache invalidation and diagnostic explain results. Owner authority and target protection are T06; command registration, parsing, dispatch and audit outcomes follow in T07–T10.

Validation will distinguish engine-free policy tests from actual engine compilation/execution. No unperformed runtime test will be reported as passing.
