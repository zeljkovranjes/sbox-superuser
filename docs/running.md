# Running Superuser

The host runtime starts only where authority belongs. This chapter covers the two
places that is true, and what to check when the runtime does not start or a
command does not behave.

- ***[In the editor](running/editor.md)*** &mdash; testing on the editor host,
  editor owners, and trying a sample from a clean checkout.

- ***[On a dedicated server](running/dedicated.md)*** &mdash; production owners,
  local console tools, and what has and has not been verified.

- ***[When something fails](running/troubleshooting.md)*** &mdash; each status
  you might see and what to check first.

Whichever you use, keep exactly one enabled **Superuser Bootstrap** in the scene
that starts your network session, and stop Play or the server before editing
source. Managed module reload is supported; arbitrary live source replacement is
not.
