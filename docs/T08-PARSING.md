# T08: Argument parsing and targeting

Status: **Verification complete; final package checks pending.** Continue with T09/T10; no Task 11 transport.

## Public parsing surface

`Superuser.TokenizeCommandText(text)` returns immutable tokens or a safe error with no partial tokens. It does not interpret a chat prefix or resolve a registered command. `ParseCommandArguments(descriptor, tokens, onlinePlayers)` converts argument tokens only, producing `CommandArguments` on complete success. `GetCommandSyntax(descriptor)` derives syntax, help, constraints and required permission from the same metadata. These pure APIs do not grant authority or execute handlers; the dispatcher must parse and authorize its own requests.

Native transport tokens must be passed directly to the argument parser. Rejoining and retokenizing them would lose literal quotes and explicit empty values verified in T01. Chat integration will remove its exact `/` root before tokenizing command text; it is not installed by T08.

## Grammar and limits

- At most 4096 characters of raw text, 64 tokens and 512 characters per token. Decoded token input has the same count/per-token bounds and a 4096-character aggregate budget including separators. Errors never echo supplied argument values.
- Whitespace separates tokens. Single or double quotes surround an entire token; `""` and `''` produce an explicit empty string. Inside quotes, a backslash escapes only the matching quote or another backslash. Unknown escapes, unterminated quotes and adjacent quoted/unquoted fragments fail. Control characters are rejected; tab may separate raw tokens.
- Integers use signed 64-bit invariant decimal parsing. Numbers use finite invariant doubles, including exponent notation. Thousands separators, whitespace inside numeric tokens, overflow and nonfinite values fail. Bounds are inclusive; integer comparison preserves exact large-integer distinctions instead of rounding the value to double. Discovered `int`/`float` parameters include their CLR range bounds.
- Booleans accept `true` or `false`, ignoring case. Enums accept only explicitly declared choices, ignoring case, and return the canonical choice string; discovery binds it to the actual enum value.
- Durations accept a positive integer followed by one unit: `s`, `m`, `h`, `d`, `w`, ignoring case. `permanent` is explicit. Zero, signs, leading zeroes, fractions, compound durations and overflow fail; none becomes permanent accidentally.
- Missing required arguments and extra tokens fail. Missing trailing optional arguments remain absent so handlers can apply their defaults. A final string remainder joins decoded tokens with single spaces while preserving whitespace inside each quoted token.

## Target resolution

The host supplies at most 1024 online `CommandPlayer` entries. Numeric tokens are interpreted as canonical nonzero decimal IDs, never as display names. Exact IDs are authoritative even when another player's display name equals that ID. Offline IDs are accepted only for `AnyPlayer`/`PunitiveAnyPlayer`; online policies require a matching online entry.

Names match the entire online display name ignoring case. A unique match becomes a stable `PlayerReference`; zero matches fails and multiple matches return no arguments, with at most 16 choices sorted by Steam ID/name. No substring guessing, mass selector or implicit self selector exists. Offline names cannot be resolved. Parsing performs no rank check; T09 must recheck online presence and T06 punitive protection before execution.

## Verification

138 contract cases and 20 Core grammar cases passed: 48 new cases over T07. Existing tests remain unchanged. The previously empty original Core test project now tests malformed/escaped input, limits, duration overflow, exact integer bounds and 2000 deterministic arbitrary grammar inputs. Contract tests cover culture-independent conversion, enums, ambiguity, authoritative IDs, offline restrictions, explicit empty/optional values, native-token preservation and generated syntax.

Engine 26.10.13a editor PID 21804 executed `dev/verify-command-parsing.cs` successfully through the public facade. `dev/verify-parsed-handler.cs` discovered the compiled dog fixture, tokenized and converted all scalar types, invoked its typed async handler, and rejected an integer beyond the CLR `int` range before invocation. Result: `parsed text|3|4000000000|1.5|2.5|True|Playful|True`. Development reflection only constructs the test registry; shipped parsing and discovery use supported code.

No dedicated-client transport, public chat input or player moderation execution is claimed. These checks require no storage mutations. T01's unpublished adopting-game deployment limitation remains; it does not block pure parsing.
