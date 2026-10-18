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
| T08 Argument parsing and targeting | Next | Not started |
| T09 Authoritative dispatcher | Pending T07/T08 | Not started |
| T10 Audit and durable outcomes | Pending T09 | Not started |
| T11–T20 | Outside current execution scope | Not started |

Current tests: 110 passing contract/Core cases. Task records distinguish plain-.NET tests from actual engine execution, editor-host checks and actual dedicated-server recovery/restart. Historical task reports describe their completion point; this ledger tracks current progress. No generic native `admin` root, general chat dispatcher, moderation enforcement or UI is implemented yet.
