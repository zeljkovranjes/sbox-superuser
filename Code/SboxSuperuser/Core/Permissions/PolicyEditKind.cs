namespace SboxSuperuser.Core.Permissions;

internal enum PolicyEditKind
{
	CreateGroup,
	DeleteGroup,
	SetParents,
	SetGroupPermission,
	SetGroupProtection,
	AddUserGroup,
	RemoveUserGroup,
	SetUserPermission
}
