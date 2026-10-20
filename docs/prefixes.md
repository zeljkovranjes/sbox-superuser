# Prefixes

A prefix is a styled tag in front of a player's name, like **[ADMIN]**, that can
be colored and animated letter by letter. Prefixes ship with Superuser as an
ordinary module, `superuser.prefixes`, built on the same module API your game
uses. It is on unless you turn it off.

Every prefix has:

- a **tag**, the text in front of the name;
- a **template** that places it, `%prefix% %name%` unless you change it;
- a **tag effect** and a **name effect**, which color and animate each part;
- an optional **palette** of up to 16 colors the effects can use.

An effect is a shipped preset, a short script written in the menu, or a C# effect
your game registers.

## Who wears which

1. A player's own prefix, set with `admin prefix user set`, always wins.
2. Otherwise the prefix of their group with the highest protection level.
3. The group `owner` stands for every owner, above every real group.

A new server starts with `[OWNER]` (rain), `[ADMIN]` (scanner) and `[MOD]`
(cyan) for the owner, admin and moderator groups. They live in memory until the
first change is saved, and they are ordinary prefixes: change or delete them.

Changes need `admin.prefixes.manage`, which the default admin group has. Reading
prefixes needs no permission.

## In the menu

**Server › Prefixes** lists every prefix with a live preview. Pick one to edit
its name, tag, template and palette, choose its tag and name effects, duplicate
or delete it. The **Groups** tab gives groups a prefix; the **Players** tab gives
individual players their own. The selected player in **Players** also has a
**Prefix** section.

**Edit code** opens the effect editor: numbered lines, live coloring, swatches
under colors, red underlines with a plain-English message for every mistake, and
a preview of the whole line with your name that animates as you type. Click a
built-in on the right to insert it, start from a template, copy a preset, or pick
a color and press **Insert color**. **Save** stays disabled until the effect is
clean. Ctrl+Z and Ctrl+Y undo and redo, Tab indents, Esc leaves the editor.

## From the console

| Command | What it does |
|---|---|
| `admin prefix list [page]` | Every prefix with its tag and effects |
| `admin prefix create <id> [tag]` | A new prefix with a plain pink tag |
| `admin prefix update <id> <field> <value>` | Change `name`, `tag`, `template`, `palette`, `tag-effect` or `name-effect` |
| `admin prefix delete <id>` | Delete it and every assignment using it |
| `admin prefix group set/clear <group> [id]` | A group's prefix |
| `admin prefix user set/clear <user> [id]` | A player's own prefix |
| `admin prefix export` | The whole catalog as JSON |

Effects take `preset:<id>`, `code:<id>` or a one-line script:

```text
admin prefix create vip [VIP]
admin prefix update vip tag-effect preset:rain
admin prefix update vip name-effect hsv(t / 5 + i * 30, 1, 1)
admin prefix update vip palette #ff3333,#ffff00
admin prefix group set admin vip
```

Multi-line effects are written in the menu. A script with an error is refused
with the same message the editor shows.

## Effects

An effect runs once for every letter, every frame. A line that is only a color
colors every letter; `i` is the letter's position and `t` the time in
milliseconds, the same for every player:

```effect
// every letter steps to the next palette color 10 times a second
color = palette[floor(t / 100) + i]
```

The animated presets port the World at War clan-tag effects from
[waw-color-tags](https://github.com/zeljkovranjes/waw-color-tags) with the same
timings: `rain`, `cycl`, `cyln` (also `krdr`), `move`, `dots` (`....`) and
`stars` (`****`), plus the static WaW colors. The
***[effect reference](reference/effects.md)*** lists every input, output and
function. The same reference, generated from the compiler, is in the menu under
**Superuser › Documentation › superuser.prefixes**.

## Chat

The built-in s&box chat box draws names as plain text, so prefixes can't appear
in it. While prefixes are on, Superuser hides it and draws its own chat box in
the same place: lines fade after eight seconds, the chat key opens the input, and
names show their animated prefixes. Up and Down bring back messages you sent.
With the chat open, click a name to open that player in the admin menu. Turn
**Prefixed Chat** off on the Superuser
Bootstrap to keep the built-in box, for example when your game has its own chat
UI; show prefixes there with `Superuser.RenderPrefix`.

## In game code

Every machine gets the prefixes from the host, so these work on clients too:

```csharp
// The plain line, for logs and text-only UI.
var prefix = Superuser.GetPrefix( steamId );
Log.Info( prefix?.Format( name ) ?? name );

// The styled line for this frame, for a Label with IsRich set.
label.Text = Superuser.RenderPrefix( steamId, name ).ToMarkup();
```

`PrefixLabel` is a ready-made label (`SboxSuperuser.UI`): set `SteamId` and
`PlayerName` and it animates itself. Implement `IPrefixChangedEvent` on a
component to hear when a player's prefix changes. For effects scripts can't
express, implement `ITagEffect` and call `Superuser.RegisterTagEffect` on every
machine; prefixes then pick it as `code:<id>`.

## Multiplayer

The host stores the catalog and sends every client the prefixes plus each online
player's prefix ID, when a client joins and after every change. Clients never
learn group membership. Each client compiles the scripts itself and animates from
UTC time, so everyone sees the same frame. The stored catalog is capped at 48 KB.

## Turning it off

Untick **Prefixes** on the Superuser Bootstrap, or run
`admin modules disable superuser.prefixes` as an owner. While it is off, the API
returns plain names and the built-in chat box comes back. Stored prefixes are
kept.
