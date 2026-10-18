using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser.Components;

/// <summary>Optional client moderation menu; disable or remove it to use your own UI.</summary>
[Title( "Superuser Admin Menu" ), Category( "Superuser" )]
public sealed class SuperuserAdminMenu : Component, IAdminResultEvent
{
	private AdminMenuSystem _menu;

	/// <summary>Keyboard key that opens/closes the menu; re-enable this component after changing it.</summary>
	[Property]
	public string OpenKey { get; set; } = "f6";

	/// <summary>Gets whether the optional local menu is open.</summary>
	public bool IsOpen => _menu?.IsOpen == true;

	/// <summary>Opens/closes an enabled menu from a game button; this grants no moderation permission.</summary>
	public void SetOpen( bool open ) => _menu?.SetOpen( open );

	protected override void OnEnabled()
	{
		if ( Application.IsDedicatedServer ) return;
		_menu = Scene.GetSystem<AdminMenuSystem>();
		_menu.Attach( Id, OpenKey );
	}

	protected override void OnDisabled()
	{
		_menu?.Detach( Id );
		_menu = null;
	}

	/// <summary>Passes matching private results to the optional view.</summary>
	public void OnAdminResult( AdminReply reply ) => _menu?.Receive( reply );
}
