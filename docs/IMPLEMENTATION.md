# Implementation ledger

The specification is [version 0.2](sbox-admin-specification.docx). Current authorization is to implement T11–T20 in order and prepare an unpublished release candidate. Everything ships inside one library; production is dedicated-server only, editor tests opt in, and chat uses `/`.

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
| T11 Native console and help | Complete | [T11](T11-CONSOLE.md) |
| T12 Module lifecycle | Complete | [T12](T12-MODULES.md) |
| T13 Platform chat | Complete | [T13](T13-PLATFORM-CHAT.md) |
| T14 Custom chat hooks | Complete | [T14](T14-CUSTOM-CHAT.md) |
| T15 Native bindings | Complete | [T15](T15-NATIVE-BINDINGS.md) |
| T16 Moderation and admission | In progress | [T16](T16-MODERATION.md) |
| T17 Lifecycle validation | Not started | Depends on T12–T16 |
| T18 Two game integrations | Not started | Depends on T17 |
| T19 Security, recovery and load | Not started | Depends on T18 |
| T20 Unpublished release candidate | Not started | Depends on T19 |

Current baseline: 222 passing contract cases and 65 Core cases. Task records distinguish plain-.NET tests from actual engine execution, editor-host checks and actual dedicated-server recovery/restart. Historical task reports describe their completion point; this ledger tracks current progress. T01–T15 are implemented, retaining the recorded dedicated-client deployment limitation. T16 is the next executable task.
