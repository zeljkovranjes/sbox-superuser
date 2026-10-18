# T11: Native console and operator commands

Status: **In progress.** Implement the console-first administration slice using T09 dispatch and T10 durable audit. Distinguish dedicated-local and authenticated player entry, return private results, add owner-only access management, diagnostics and permission-filtered descriptor help. Do not start T12 until acceptance evidence is complete.

Engine inspection on 26.10.13a confirms ConsoleSystem exposes Run/GetValue/SetValue, while Command, ManagedCommand and ConVarSystem are internal. Runtime native-name registration is unavailable through the public API. Use the T01 fallback: fixed native transport with a configurable logical root. The default native `admin` convenience entry can be used when no visible managed-name collision exists; a namespaced transport remains available for another configured root. Report collisions instead of overwriting commands dynamically. Native engine enumeration and autocomplete are not promised.

Current goal continues through T20 in dependency order and prepares an unpublished release candidate. `/` remains the chat prefix; visual UI stays deferred.
