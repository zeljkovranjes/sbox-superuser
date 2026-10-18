# Bundled ImGui

Source: [zeljkovranjes/sbox-imgui](https://github.com/zeljkovranjes/sbox-imgui), revision `165a6dc5adda0672438ac0341dac6ea3d33baf18`.

The upstream project is a C# port of Dear ImGui, forked from [chrisspieler/sbox-imgui](https://github.com/chrisspieler/sbox-imgui). Its MIT license is preserved in [LICENSE](LICENSE). Dear ImGui is by Omar Cornut and contributors.

Widget and drawing code is internal to Superuser. Namespaces are isolated; demos, inspectors and automatic scene systems are omitted. Superuser owns rendering and input only while its optional menu is open. `ImGuiHost` provides the small keyboard/clipboard bridge used by the widgets.

Regenerate with `python dev/vendor-imgui.py <upstream-checkout>`. No native binding or external runtime is required.

Local patches remove an upstream self-assignment and suppress unused reserved-field warnings in vendored code only. The menu's input panel is adapted from the upstream panel under the same MIT license.
