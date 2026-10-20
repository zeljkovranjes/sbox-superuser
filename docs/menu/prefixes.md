# Prefixes and documentation

## Prefixes

**Server › Prefixes** appears for staff with `admin.prefixes.manage`. The left
side lists every prefix with a live, animated preview using your name; type an ID
and press **New** to add one. The right side has three tabs:

- **Prefix**: a large preview, the name, tag, template and palette (click a color
  to change it, right-click to remove it, **+** to add one), then **Apply**. Below,
  a picker for the tag effect and the name effect, each with **Edit code**, and
  **Duplicate** and **Delete**.
- **Groups**: every group on the server with its protection level, and a prefix
  picker for each. `owner` stands for every owner. A player in several groups gets
  the one with the highest protection. Groups created in **Permissions** appear
  after **Refresh**.
- **Players**: online players and everyone with a prefix of their own. **Group
  prefix** hands a player back to their groups.

Edits are sent one at a time as ordinary audited commands, so they show up in
**Activity**.

## The effect editor

**Edit code** opens a separate window:

- the code editor, with line numbers, coloring, a swatch under every color, and a
  red underline under each mistake;
- a preview of the whole line with your name, animating as you type;
- the problem bar under the editor, with the line and a plain-English message;
- built-ins on the right: click one to insert it, hover for help;
- **New from template** (solid color, gradient, rainbow scroll, wipe, scanner,
  blink, typewriter), **Copy a preset**, a color picker with **Insert color**, and
  **Reference**, which opens the effect reference.

Ctrl+Z and Ctrl+Y undo and redo, Ctrl+A selects everything, Ctrl+C, Ctrl+X and
Ctrl+V use the clipboard, Tab and Shift+Tab indent, Enter keeps the indent, and
Esc leaves the editor without closing the menu. **Save** is disabled until the
effect has no problems.

## Documentation

**Superuser › Documentation** lists every module you can see, with its version.
Each one opens to the same four parts: **Overview**, the module's own guides,
**Commands** (the ones you may run, with their syntax and an **Open** button for
the Commands window) and **Permissions**. Superuser's own pages are always there;
another module's pages appear for owners and for anyone who may run one of its
commands. Modules add pages with `AddDocumentation`; see
***[Modules](../reference/modules.md)***.
