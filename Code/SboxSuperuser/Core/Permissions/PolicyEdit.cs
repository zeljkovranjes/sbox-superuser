namespace SboxSuperuser.Core.Permissions;

internal sealed record PolicyEdit( PolicyEditKind Kind, string Subject, string Value = null,
	IReadOnlyList<string> Parents = null, int Effect = 0, int ProtectionLevel = 0 );
