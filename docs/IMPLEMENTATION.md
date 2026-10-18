# Implementation ledger

The specification is [version 0.2](sbox-admin-specification.docx). Current authorization is to implement T05–T10 in order and stop before T11. Everything ships inside one library; production is dedicated-server only, editor tests opt in, and chat uses `/`.

| Task | Status | Record |
|---|---|---|
| T01 Engine compatibility | Complete with dedicated-client deployment evidence blocked by the unpublished test-game package | [T01](T01-COMPATIBILITY.md) |
| T02 Public contracts | Complete | [T02](T02-CONTRACTS.md) |
| T03 Bootstrap and identity | Complete | [T03](T03-BOOTSTRAP.md) |
| T04 Versioned durable storage | Complete | [T04](T04-STORAGE.md) |
| T05 Permission resolver and groups | Complete | [T05](T05-PERMISSIONS.md) |
| T06 Target protection and policy administration | Complete | [T06](T06-AUTHORITY.md) |
| T07 Command registration and discovery | Complete | [T07](T07-COMMANDS.md) |
| T08 Argument parsing and targeting | Complete | [T08](T08-PARSING.md) |
| T09 Authoritative dispatcher | Complete | [T09](T09-DISPATCH.md) |
| T10 Audit and durable outcomes | Complete | [T10](T10-AUDIT.md) |
| T11–T20 | Outside current execution scope | Not started |

Current tests: 180 passing contract cases and 32 Core cases. Task records distinguish plain-.NET tests from actual engine execution, editor-host checks and actual dedicated-server recovery/restart. Historical task reports describe their completion point; this ledger tracks current progress. T05–T10 are complete; execution stops before T11. No generic native `admin` root, general chat dispatcher, moderation enforcement or UI is implemented yet.
