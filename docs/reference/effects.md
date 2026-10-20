# Effect scripts

An effect runs once for every letter, every frame. It reads the inputs, sets any
outputs it wants, and the letter is drawn with them. Outputs you don't set keep
their starting value. The in-game copy of this page under **Superuser ›
Documentation** is generated from the compiler itself.

## Inputs

| Name | What it is | Example |
|---|---|---|
| `t` | Time in milliseconds. Every player sees the same value at the same moment. | `floor(t / 100)` |
| `i` | Which letter this is, starting at 0. | `i == 0` |
| `L` | How many letters this part has. | `i == L - 1` |
| `ch` | This letter, as text. | `ch == "a"` |
| `part` | Which part is drawing: `"tag"` or `"name"`. | `part == "name"` |
| `text` | The whole text of this part. | `len(text)` |
| `palette` | The prefix's colors as a list. Indexes wrap around. | `palette[i]` |
| `seed` | A number fixed per player, for effects that differ between players. | `hash(i, seed)` |

## Outputs

| Name | What it does | Example |
|---|---|---|
| `color` | This letter's color. Starts white. | `color = red` |
| `alpha` | Opacity from 0 to 1. Starts at 1. | `alpha = wave(t, 1000)` |
| `char` | Replace this letter with other text. | `char = "o"` |
| `offset` | Push this letter, and every letter after it, right by this many letter widths. | `offset = 2` |
| `bold` | Draw this letter bold. | `bold = true` |
| `italic` | Draw this letter slanted. | `italic = i % 2 == 0` |
| `hidden` | Hide this letter. | `hidden = i > 3` |

## Placeholders

| Name | What it is |
|---|---|
| `%name%` | The player's name. |
| `%prefix%` | The prefix's tag text. |

## Functions

| Function | What it does |
|---|---|
| `floor(x)`, `ceil(x)`, `round(x)` | Round down, up, or to the nearest whole number |
| `abs(x)`, `sign(x)`, `sqrt(x)`, `pow(x, y)` | Absolute value, sign, square root, power |
| `sin(x)`, `cos(x)`, `tan(x)` | Trigonometry, in radians |
| `min(a, b)`, `max(a, b)`, `clamp(x, low, high)` | Limits |
| `lerp(a, b, amount)` | Blend two numbers; 0 gives `a`, 1 gives `b` |
| `mod(x, n)` | Remainder that never goes negative |
| `pingpong(x, n)` | Counts 0, 1 ... n-1 and back down, forever |
| `wave(t, ms)` | A smooth 0 to 1 to 0 wave lasting `ms` milliseconds |
| `hash(a, b)` | A fixed random-looking number from 0 to 1 |
| `rgb(r, g, b)`, `rgb(r, g, b, a)` | A color from 0-255 channels |
| `hsv(hue, saturation, value)` | A color from hue in degrees and 0-1 saturation and value |
| `mix(first, second, amount)` | Blend two colors |
| `fade(color, alpha)` | A color with another opacity |
| `len(x)`, `upper(text)`, `lower(text)`, `at(text, n)` | Text and list helpers; `at` wraps around |

## Colors

Write a name, a hex color (`#f80`, `#ff8800`, or `#ff8800cc` with alpha), or
use `rgb` and `hsv`. The first eight names are the World at War palette `^0` to
`^7`: `black` `#000000`, `red` `#ff3333`, `green` `#00ff00`, `yellow` `#ffff00`,
`blue` `#0000ff`, `cyan` `#00ffff`, `pink` `#ff5cff`, `white` `#ffffff`. Also
`orange` `#ff8800`, `purple` `#9933ff` and `gray` `#888888`. A prefix without a
palette uses WaW's `^1` to `^5`.

## Rules

- One statement per line, or separate them with `;`. `//` starts a comment and
  `/* */` wraps one.
- A line that is only a color, like `red`, sets `color`.
- `name = value` stores a value. New names are your own variables; they start
  at 0 for every letter. Inputs, functions and color names can't be assigned.
- `if condition { ... } else if condition { ... } else { ... }`, and
  `condition ? a : b` for a choice inside one value.
- Operators: `+ - * / %`, `== != < <= > >=`, `and` (`&&`), `or` (`||`),
  `not` (`!`). `+` joins text.
- Lists use `[a, b, c]`. `list[n]` and `text[n]` wrap around, so `palette[i]`
  never runs off the end.
- Dividing by zero gives 0. There are no loops; an effect is at most 2048
  characters.
- Mistakes are reported with the line, the exact spot and a suggestion, for
  example `"rde" isn't defined; did you mean red?`. If an effect fails while
  running, that part is drawn plain white for the frame.

## Examples

```effect
// A gradient from the first palette color to the second.
color = mix(palette[0], palette[1], i / max(1, L - 1))
```

```effect
// A smooth rainbow; raise speed to scroll faster.
speed = 0.2
color = hsv(t * speed + i * 30, 1, 1)
```

```effect
// WaW's cyln: one red letter sweeps back and forth.
eye = floor((cos(t * 0.004) + 1) * 0.5 * L)
color = i == eye ? red : white
```
