using Sandbox;
using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Settings;

/// <summary>Development-only native settings and a counter command; all values remain in the host process.</summary>
public static class NativeBindingExample
{
	[ConVar( "superuser_example_capacity" )]
	private static int Capacity { get; set; } = 8;
	[ConVar( "superuser_example_pending" )]
	private static int Pending { get; set; } = 2;
	[ConVar( "superuser_example_saved", ConVarFlags.Saved )]
	private static int Saved { get; set; } = 4;
	[ConVar( "superuser_example_protected", ConVarFlags.Protected )]
	private static int Protected { get; set; } = 7;
	[ConVar( "superuser_example_admin", ConVarFlags.Admin )]
	private static int Admin { get; set; } = 7;
	[ConVar( "superuser_example_cheat", ConVarFlags.Cheat )]
	private static int Cheat { get; set; } = 7;
	[ConVar( "superuser_example_clamp", Min = 1, Max = 5 )]
	private static int Clamped { get; set; } = 3;
	[ConVar( "superuser_example_sensitive" )]
	private static string Sensitive { get; set; } = "test-placeholder";
	/// <summary>Gets the actual compiled counter's execution count.</summary>
	public static int Executions { get; private set; }
	/// <summary>Gets the most recently passed quoted argument for test comparison.</summary>
	public static string LastArgument { get; private set; }

	[ConCmd( "superuser_example_increment" )]
	private static void Increment( string text )
	{
		Executions++;
		LastArgument = text;
	}

	/// <summary>Creates an explicitly allowlisted module; invoke from trusted host setup after bootstrap is ready.</summary>
	public static IAdminModule CreateModule() => Superuser.CreateNativeBindingsModule( new[]
	{
		Integer( "example.capacity", "superuser_example_capacity", "8", NativePersistence.Framework ),
		Integer( "example.pending", "superuser_example_pending", "2", NativePersistence.Framework, restart: true ),
		Integer( "example.saved", "superuser_example_saved", "4", NativePersistence.Engine ),
		Integer( "example.protected", "superuser_example_protected", "7" ),
		Integer( "example.admin", "superuser_example_admin", "7" ),
		Integer( "example.cheat", "superuser_example_cheat", "7" ),
		Integer( "example.clamp", "superuser_example_clamp", "3", NativePersistence.Framework ),
		Integer( "example.missing", "superuser_example_missing", "1" ),
		new NativeSettingDescriptor( "example.sensitive", "superuser_example_sensitive", "Test-only private placeholder", "example.settings",
			CommandArgumentKind.String, "test-placeholder", sensitive: true )
	}, new[]
	{
		new NativeCommandBindingDescriptor( "example.increment", "superuser_example_increment", "Increment the compiled test counter", "example.settings",
			new[] { new CommandArgumentDescriptor( "text", "Quoted test text", CommandArgumentKind.String ) } )
	} );

	private static NativeSettingDescriptor Integer( string key, string native, string defaultValue,
		NativePersistence persistence = NativePersistence.Session, bool restart = false ) => new(
		key, native, "Example native integer", "example.settings", CommandArgumentKind.Integer, defaultValue,
		persistence, minimum: 1, maximum: 32, requiresRestart: restart );
}
