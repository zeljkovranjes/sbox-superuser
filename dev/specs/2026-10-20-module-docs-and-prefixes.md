# Module documentation and the Prefix module

Agreed with the user on 2026-10-20. Two pieces, built in this order: module
documentation first, then the Prefix module, which documents itself through it.

## Part A: module documentation

### Authoring API

- `ModuleDescriptor` gains init-only `Version`, `Author` and `Website`.
  Existing constructors keep working.
- `IModuleRegistration.AddDocumentation( string title, string markdown )` adds a
  page; pages keep the order they were added. Direct registration scopes
  (`BeginCommandRegistration`) get it too, because they implement the same
  interface.
- `IModuleRegistration.AddDocumentationFile( string title, string path )` reads a
  Markdown file from the mounted file system, for modules that ship `.md` files.
- Limits: 32 pages per module, 64-character titles, 32 KB per page.

### Generated sections

Every module's documentation has the same fixed structure, so they all read
alike:

1. **Overview**: description, version, author, website, state, API version,
   dependencies. Generated.
2. **Guides**: the module's own pages, in order.
3. **Commands**: syntax, arguments and permission for each command the reader
   may run. Generated from registrations.
4. **Permissions**: key and description for each permission the module
   registers. Generated.

A module with no written pages still has useful documentation.

### Reading it

- New query `AdminQueryKind.Documentation`: an empty search lists the modules
  visible to the caller; `search = moduleId, page = n` returns one page.
- A module is visible to owners, and to anyone who may run at least one of its
  commands. Superuser's own module is always visible.
- Menu: **Superuser › Documentation** window. The left side lists modules with
  their version and a page tree; the right side shows the page.
- Markdown subset: headings, paragraphs, bullet and numbered lists, code blocks,
  tables, inline code and bold. Parsed in Core and drawn in UI. Effect-script
  code blocks are syntax highlighted once Part B lands.

## Part B: the Prefix module

### What a prefix is

- An ID, a display name and a template, default `%prefix% %name%`. Literal text
  in the template is allowed.
- Tag text, a tag effect, a name effect and an optional palette.
- An effect is either an **effect script** written in the menu, a built-in
  preset, or a C# `ITagEffect` registered by game code.

### Assignment

- Groups can have a prefix, and so can individual players. A player's own
  prefix wins; among groups, the highest protection level wins.
- Host-side storage: one versioned, recoverable document through
  `Superuser.GetStore()`, written with audit intents.
- Changes are audited commands needing `admin.prefixes.manage` (admins by
  default): `prefix.create`, `prefix.update`, `prefix.delete`,
  `prefix.group.set` and `prefix.group.clear`, `prefix.user.set` and
  `prefix.user.clear`. Reading prefixes needs no permission.

### Syncing

- The host sends every client the definitions plus a `steamId → prefixId` map
  for online players, on join and after each change. Clients never learn group
  membership.
- Clients compile scripts themselves and animate locally from UTC
  milliseconds, so every player sees the same frame.
- The encoded catalog is capped at 48 KB and checked on save.

### Shipped by default

- Superuser Bootstrap installs the module unless its new **Prefixes** option is
  off. Owners can also run `admin modules disable superuser.prefixes`. While it is
  disabled, the API returns plain names.
- It is built like a third-party module: commands, permissions, store,
  services and documentation pages, all through the same API games use.

### Effect scripts

- Run once per letter per frame. Variables, `if/else`, ternaries, arrays,
  math, comments. No loops, no I/O, an instruction budget, size limits.
- Inputs: `t` (ms), `i`, `L`, `ch`, `part` (`"tag"`, `"name"`, `"text"`),
  `palette`, `seed`, named colors including the WaW `^0`–`^7` palette.
- Outputs: `color`, `alpha`, `char`, `offset`, `bold`, `italic`, `hidden`.
- Built-ins: `floor ceil round abs sin cos min max clamp lerp mod rgb hsv mix
  len upper lower pingpong wave`.
- Errors in plain English with suggestions. A runtime failure renders plain
  white text for that frame.
- Presets are written in the same language and are read-only; **Duplicate**
  makes an editable copy. They port the waw-color-tags effects: `rain`,
  `cycl`, `cyln`/`krdr`, `move`, `....`, `****`, and the static colors. New
  effects start from templates: Solid color, Gradient, Rainbow scroll, Wipe,
  Scanner, Blink, Typewriter.

### Code editor

- Superuser's own widget: line numbers, coloring from the language tokenizer,
  color swatches next to hex colors, error underlines with a message bar,
  cursor and selection, word jumps, clipboard, undo and redo, Tab and
  auto-indent, vertical scroll. No word wrap, multiple cursors, find or
  autocomplete.
- Text buffer logic lives in Core with tests; UI only draws it and feeds keys.

### API (one `Superuser` facade)

- `GetPrefix( steamId )` returns a `PlayerPrefix`: ID, plain tag, template,
  whether it is animated.
- `RenderPrefix( steamId, name, time )` returns a `StyledLine` of runs, with
  `ToMarkup()` for Razor and `ToPlain()` for logs.
- `PrefixLabel` Razor panel animates itself.
- `RegisterTagEffect( ITagEffect )` adds a C# effect.
- `IPrefixChangedEvent` fires when a player's prefix changes.

### Menu

- **Server › Prefixes** window.
  - Left: every prefix with an animated preview, plus New, Duplicate and Delete.
  - Right: name, tag text, template, palette editor, and a tag effect and a name
    effect, each picked from presets, custom scripts and C# effects, with
    **Edit code** to open the editor.
  - Tabs: **Groups** (a prefix per group) and **Players** (explicit
    assignments).
- **Effect editor** pop-up window: the code editor, a live preview of the whole
  line with your name, an error bar, a list of built-ins to click and insert,
  New-from-template, a color picker that inserts hex, and Save (which checks
  first) and Cancel.
- **Players** window: a **Prefix** section in the selected player's details
  for staff with `admin.prefixes.manage`.
- The module's documentation includes the full effect-script reference, opened
  from the editor's built-ins list.

## Changes made while building

- **Chat.** The built-in s&box chat box draws names as plain text, so prefixes
  can't appear inside it. At the user's request prefixes still show in chat by
  default: Superuser hides the built-in box (the project setting
  `ChatShowUI`, flipped at runtime and restored) and draws its own in the same
  place. A new bootstrap option, **Prefixed Chat**, turns that off.
- **Starter prefixes.** A server with no stored catalog shows `[OWNER]`,
  `[ADMIN]` and `[MOD]`, kept in memory until the first save.
- **Owner group.** `owner` is a pseudo-group at protection 100 for prefix
  assignment, so every owner can share a prefix without joining a group.
- **Script size.** Scripts cap at 2048 characters instead of 4096: admin
  commands carry at most 16 arguments of 256 printable characters, so the menu
  sends scripts as `b64:` chunks.
- **Permission.** `admin.prefixes.manage` is in the default admin group, which
  pre-registers it; the module registers it only when it isn't already known.
