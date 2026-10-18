// Vendored from zeljkovranjes/sbox-imgui; see LICENSE and PROVENANCE.md in this folder.
using Sandbox;
#pragma warning disable CS0649 // Upstream reserves fields for optional widgets and features.
namespace SboxSuperuser.Engine.Vendor.ImGui;

[Flags]
internal enum ImDrawFlags
{
	None = 0,
	Closed = 1 << 0,
	RoundCornersTopLeft = 1 << 4,
	RoundCornersTopRight = 1 << 5,
	RoundCornersBottomLeft = 1 << 6,
	RoundCornersBottomRight = 1 << 7,
	RoundCornersNone = 1 << 8,
	RoundCornersTop = RoundCornersTopLeft | RoundCornersTopRight,
	RoundCornersBottom = RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersLeft = RoundCornersBottomLeft | RoundCornersTopLeft,
	RoundCornersRight = RoundCornersBottomRight | RoundCornersTopRight,
	RoundCornersAll = RoundCornersTopLeft | RoundCornersTopRight | RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersDefault_ = RoundCornersAll,
	RoundCornersMask_ = RoundCornersAll | RoundCornersNone,
}
