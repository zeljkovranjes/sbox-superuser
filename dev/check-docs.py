"""Check Markdown links and extract the documented C# into an engine build fixture."""

import pathlib
import re

root = pathlib.Path(__file__).resolve().parent.parent
files = [root / "README.md", root / "CHANGELOG.md"]
files += list((root / "docs").glob("*.md"))
files += list((root / "dev" / "fixtures").glob("*.md"))
files += list((root / "dev" / "samples").rglob("*.md"))
files += list((root / "dev" / "ui-source").rglob("*.md"))
errors = []
blocks = {}
for path in files:
    text = path.read_text(encoding="utf-8-sig")
    if re.search(r"\bT\d{2}\b|\b[Tt]asks?[ -]+\d+", text):
        errors.append(f"Task label: {path.relative_to(root)}")
    for target in re.findall(r"\[[^\]]*\]\(([^)]+)\)", text):
        if re.match(r"https?://|#", target):
            continue
        target = target.split("#", 1)[0]
        if not (path.parent / target).exists():
            errors.append(f"Broken link: {path.relative_to(root)} -> {target}")
    blocks[str(path.relative_to(root)).replace("\\", "/")] = re.findall(
        r"```csharp\s*\n(.*?)```", text, re.S
    )

normalize = lambda text: re.sub(r"\s+", "", text)
ping = (root / "dev/fixtures/PingModule.cs").read_text(encoding="utf-8-sig")
for path in ("README.md", "docs/commands-tutorial.md"):
    if normalize(blocks[path][0]) not in normalize(ping):
        errors.append(f"Ping excerpt differs from compiled source: {path}")

methods = [
    ("async Task RegisterAsync()", blocks["docs/commands-tutorial.md"][1]),
    ("void SubmitChat( string text )", blocks["docs/chat.md"][0]),
    ("void HideOverlay()", blocks["docs/chat.md"][1]),
    ("void RouteChat( Connection sender, string text, Guid requestId )", blocks["docs/chat.md"][2]),
    ("void ArenaDiagnostics()", blocks["dev/samples/arena/README.md"][0]),
    ("void ColonyDiagnostics()", blocks["dev/samples/colony/README.md"][0]),
    ("void SearchPlayers()", blocks["docs/admin-menu.md"][0]),
    ("void SubmitBan()", blocks["docs/admin-menu.md"][1]),
    ("void ConfigureStorage()", blocks["docs/storage-tutorial.md"][0]),
]
source = "using Sandbox;\nusing SboxSuperuser;\n\nnamespace SuperuserExamples.Engine;\n\n"
source += "/// <summary>Compiles the exact Markdown snippets for engine verification.</summary>\n"
source += "public static class DocumentationExamples\n{\n"
for signature, body in methods:
    source += "\t/// <summary>Runs a documented example.</summary>\n"
    source += f"\tpublic static {signature}\n\t{{\n"
    source += "\n".join("\t\t" + line for line in body.rstrip().splitlines()) + "\n\t}\n\n"
source += "}\n"
if errors:
    raise SystemExit("\n".join(errors))
output = root / "dev/out/DocumentationExamples.cs"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(source, encoding="utf-8")
print(f"Checked {len(files)} Markdown files; {len(methods) + 2} C# blocks covered by source or generated fixture.")
print(output)
