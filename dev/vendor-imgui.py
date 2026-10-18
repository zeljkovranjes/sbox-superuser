"""Import the pinned C# ImGui widgets; omit demos, inspectors and global scene drivers."""

import pathlib
import re
import subprocess
import sys

source = pathlib.Path(sys.argv[1]).resolve()
root = pathlib.Path(__file__).resolve().parent.parent
revision = "165a6dc5adda0672438ac0341dac6ea3d33baf18"
head = subprocess.check_output(
    ["git", "-c", f"safe.directory={source.as_posix()}", "-C", str(source), "rev-parse", "HEAD"], text=True
).strip()
if head != revision:
    raise SystemExit(f"Expected {revision}; found {head}")
namespace = "SboxSuperuser.Engine.Vendor.ImGui"
destination = root / "Code/SboxSuperuser/Engine/Vendor/ImGui"
destination.mkdir(parents=True, exist_ok=True)
code = source / "Code/Duccsoft.ImGui"
files = list(code.glob("ImGui.*.cs")) + list((code / "Api").glob("*.cs")) + list((code / "Engine").glob("*.cs"))
count = 0
for path in sorted(files):
    if path.name.startswith("ImGui.Demo") or path.name in ("ImGui.Tools.cs", "ComponentExtensions.cs"):
        continue
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"using Duccsoft\.ImGui(?:\.[A-Za-z]+)?;\n", "", text)
    text = re.sub(r"namespace Duccsoft\.ImGui(?:\.[A-Za-z]+)?;", f"namespace {namespace};", text)
    text = re.sub(r"^public (?=(?:(?:static|sealed|partial|readonly) )*(?:class|struct|enum|delegate)\b)", "internal ", text, flags=re.M)
    text = text.replace("ImGuiSystem", "ImGuiHost")
    text = text.replace("\t\tif ( g.NavWindow is not null )\n\t\t\tg.NavWindow.DC.MenuBarAppending = g.NavWindow.DC.MenuBarAppending;", "")
    header = "// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.\nusing Sandbox;\n"
    header += "#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.\n"
    (destination / path.name).write_text(header + text, encoding="utf-8")
    count += 1
(destination / "LICENSE").write_text((source / "LICENSE").read_text(), encoding="utf-8")
print(f"Imported {count} widget files from {revision}")
