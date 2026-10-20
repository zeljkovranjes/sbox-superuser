using SboxSuperuser.Core.Contracts;

namespace SboxSuperuser;

/// <summary>
/// Describes a window a module adds to the admin menu, under Modules. Every button and form runs one of the
/// module's commands through the usual permission check, confirmation and audit; no module code runs on clients.
/// Each viewer sees only the elements whose commands they may run.
/// </summary>
public sealed class AdminMenuWindow
{
	internal const int MaximumTitleLength = 48;
	internal const int MaximumElements = 64;
	internal const int MaximumTextLength = 1024;

	private readonly List<AdminMenuElementRow> _elements = new();

	internal AdminMenuWindow( string title )
	{
		ContractGuard.Text( title, nameof( title ) );
		if ( title.Length > MaximumTitleLength || title.Any( char.IsControl ) )
			throw new ArgumentException( $"Window titles are one line of at most {MaximumTitleLength} characters.", nameof( title ) );
		Title = title.Trim();
	}

	internal string Title { get; }

	internal IReadOnlyList<AdminMenuElementRow> Elements => _elements;

	/// <summary>Adds a section title.</summary>
	public AdminMenuWindow Heading( string text ) => Add( AdminMenuElementKind.Heading, text );

	/// <summary>Adds a paragraph.</summary>
	public AdminMenuWindow Text( string text ) => Add( AdminMenuElementKind.Text, text );

	/// <summary>Adds a dividing line.</summary>
	public AdminMenuWindow Separator() => Add( AdminMenuElementKind.Separator, "" );

	/// <summary>Adds a button that runs a command with these arguments; commands that change something ask first.</summary>
	public AdminMenuWindow Button( string label, string command, params string[] arguments ) =>
		Add( AdminMenuElementKind.Button, label, command, arguments );

	/// <summary>Adds the command's generated form, with a field for each argument and a Run button.</summary>
	public AdminMenuWindow CommandForm( string command ) => Add( AdminMenuElementKind.Form, "", command );

	/// <summary>Shows the reply of a read-only command, such as a list, refreshed every few seconds and right after changes.</summary>
	public AdminMenuWindow Live( string command, params string[] arguments ) => Add( AdminMenuElementKind.Live, "", command, arguments );

	private AdminMenuWindow Add( AdminMenuElementKind kind, string text, string command = "", string[] arguments = null )
	{
		if ( _elements.Count >= MaximumElements ) throw new ArgumentException( $"A window can hold at most {MaximumElements} elements." );
		text ??= "";
		if ( text.Length > MaximumTextLength ) throw new ArgumentException( $"Window text can be at most {MaximumTextLength} characters.", nameof( text ) );
		if ( kind is AdminMenuElementKind.Heading or AdminMenuElementKind.Button && text.Any( char.IsControl ) )
			throw new ArgumentException( "Headings and button labels are one line.", nameof( text ) );
		if ( command.Length > 0 ) ContractGuard.Key( command, nameof( command ) );
		var copy = (arguments ?? Array.Empty<string>()).ToArray();
		// The same limits as a request from the menu, so a button can never build one the host refuses.
		_ = new AdminCommandRequest( command.Length > 0 ? command : "none", copy );
		_elements.Add( new AdminMenuElementRow { Kind = kind, Text = text, Command = command, Arguments = copy } );
		return this;
	}
}
