using Sandbox;
using SboxSuperuser.Core.Menu;

namespace SboxSuperuser.Engine.Admin;

/// <summary>Keeps the local player's admin menu layout and preferences in FileSystem.Data so they survive a restart.</summary>
internal static class MenuSettingsStore
{
	private const string Folder = "superuser";
	private const string LayoutPath = "superuser/menu-layout.json";
	private const string PreferencesPath = "superuser/menu-preferences.json";

	/// <summary>Returns the saved layout, or null when there is none or it cannot be read.</summary>
	internal static MenuLayout LoadLayout() => Load( LayoutPath, MenuLayout.MaximumCharacters, MenuLayout.Parse, "layout" );

	/// <summary>Returns the saved preferences, or null when there are none or they cannot be read.</summary>
	internal static MenuPreferences LoadPreferences() => Load( PreferencesPath, MenuPreferences.MaximumCharacters, MenuPreferences.Parse, "preferences" );

	internal static void SaveLayout( MenuLayout layout ) => Save( LayoutPath, layout.Write(), "layout" );

	internal static void SavePreferences( MenuPreferences preferences ) => Save( PreferencesPath, preferences.Write(), "preferences" );

	private static T Load<T>( string path, int maximumCharacters, Func<string, T> parse, string label ) where T : class
	{
		try
		{
			if ( !FileSystem.Data.FileExists( path ) ) return null;
			if ( FileSystem.Data.FileSize( path ) > maximumCharacters ) throw new ArgumentException( "Menu settings exceed the byte limit." );
			return parse( FileSystem.Data.ReadAllText( path ) );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Saved admin menu {label} ignored; defaults are used. type={error.GetType().Name}." );
			return null;
		}
	}

	private static void Save( string path, string json, string label )
	{
		try
		{
			FileSystem.Data.CreateDirectory( Folder );
			FileSystem.Data.WriteAllText( path, json );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Admin menu {label} not saved. type={error.GetType().Name}." );
		}
	}
}
