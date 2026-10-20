using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser.Components;

/// <summary>Enables one trusted library runtime for the active dedicated session, or an explicit editor test.</summary>
[Title( "Superuser Bootstrap" ), Category( "Superuser" )]
public sealed partial class SuperuserBootstrap : Component
{
	private SuperuserSession _session;

	/// <summary>Allows editor-host testing only; apply changes by disabling and enabling the component.</summary>
	[Property, Group( "Testing" )]
	public bool AllowEditorTesting { get; set; }

	/// <summary>Sets the logical console root and slash-chat prefix; re-enable to apply.</summary>
	[Property, Group( "Commands" )]
	public string CommandRoot { get; set; } = "admin";

	/// <summary>Installs the shipped Prefix module; re-enable to apply.</summary>
	[Property, Group( "Prefixes" )]
	public bool Prefixes { get; set; } = true;

	/// <summary>Replaces the built-in chat box with Superuser's, which shows prefixes; re-enable to apply.</summary>
	[Property, Group( "Prefixes" )]
	public bool PrefixedChat { get; set; } = true;

	protected override void OnEnabled() => Attach();

	protected override void OnStart() => Attach();

	private void Attach()
	{
		_session = Scene.GetSystem<SuperuserSession>();
		_session?.Attach( Id, AllowEditorTesting, CommandRoot, Prefixes, PrefixedChat );
	}

	protected override void OnDisabled()
	{
		_session?.Detach( Id );
		_session = null;
	}
}
