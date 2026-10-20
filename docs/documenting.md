# Documenting your module

Every module gets its own pages under **Superuser › Documentation**, so the
server admins who install your module can learn it without leaving the game.
Superuser writes half of it for you: the commands and permissions are generated
from what you register. This guide adds the other half, using the
[ping module](../dev/fixtures/PingModule.cs) from the tutorial. The
[greeter example](../dev/examples/greeter/README.md) shows all of it in one file.

## What readers see

Every module's documentation has the same four parts, in this order:

1. **Overview**: your description, version, author, website, whether the module
   is running, and what it depends on.
2. **Your guides**: the pages you add, in the order you add them.
3. **Commands**: the syntax, arguments and description of every command the
   reader may run, with a button that opens it in the Commands window.
4. **Permissions**: every permission your module registers, with its description.

A module with no guides still shows the other three, so the steps below only add
to what is already there.

## 1. Say who made it

Set `Version`, `Author` and, if you have one, `Website` on your descriptor. They
show on the Overview.

```csharp
public ModuleDescriptor Descriptor { get; } = new( "example.ping", "A ping command", 1 )
{
	Version = "1.0.0",
	Author = "Your name"
};
```

The second argument of the descriptor is the description at the top of the
Overview. Write it for an admin, not a programmer: what the module lets them do.

## 2. Describe your commands and permissions well

The generated Commands and Permissions pages use the text you already pass when
registering, so that text is documentation too:

- the command's `Description` (`"Reply with Pong!"`) appears under its syntax;
- each argument's description appears next to the argument;
- each permission's description (`"Use ping"`) appears in the Permissions table.

Short, plain descriptions there do more than a long guide page.

## 3. Add a guide page

In `StartAsync`, call `AddDocumentation` with a title and some Markdown:

```csharp
registration.AddDocumentation( "Using ping", """
	# Using ping

	`ping` replies **Pong!** to whoever runs it. Use it to check that commands
	reach the server.
	""" );
```

A C# raw string (`"""`) keeps the Markdown readable and strips the indentation.
Call `AddDocumentation` once per page; pages appear in the order you add them.

## 4. Or ship the pages as files

Longer guides are easier to write as `.md` files. Put the file in your project's
`Assets` folder and pass its path relative to that folder:

```csharp
registration.AddDocumentationFile( "Setting up ping", "docs/ping-setup.md" );
```

Here the file is `Assets/docs/ping-setup.md`. It is read when the module starts;
if it is missing, the module doesn't start and says why, so you notice straight
away.

## 5. Check it in the game

Start the game, open the menu with **F6**, choose **Superuser › Documentation**
and pick your module. If a page changed, press **Refresh** at the top of the list.
Owners see every module; other players see a module once they may run one of its
commands.

## Markdown you can use

The menu draws a subset of Markdown:

| Write | Shows |
|---|---|
| `# Title`, `## Section`, `### Part` | Headings |
| A blank line between lines | A new paragraph |
| `- item` or `1. item` | Bullet or numbered lists |
| `**bold**`, `*italic*`, `` `code` `` | Inline styles |
| `[text](https://...)` | The text followed by the address |
| A fenced code block | A copyable code box |
| A table with a `\|---\|` line | A table |
| `---` | A divider |

A code block tagged `effect` is colored as a prefix effect script, and any other
tag is drawn plain. For example, this page shows a command and its reply:

````markdown
## Checking the server

Run this in the console:

```text
admin ping
```

The reply is **Pong!**. If nothing comes back, see *Troubleshooting*.
````

Images and HTML are not drawn.

## Limits

- At most 32 pages per module.
- Titles of up to 64 characters.
- Up to 32 KB of Markdown per page.

Pages that break a limit, or a missing file, stop the module from starting, the
same as any other registration mistake.

## Tips

- One task per page, titled after the task: "Setting up teams", not "Teams".
- Show the exact command to type, in a `text` code block, before explaining it.
- Say which permission a task needs, and which group has it by default.
- Keep the first page short. Readers open it first, often in the middle of a game.

Components that register commands directly with `Superuser.BeginCommandRegistration`
can add pages the same way: the scope has the same `AddDocumentation` and
`AddDocumentationFile` methods.
