using SboxSuperuser.Core.Commands;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Tokenizes bounded text without interpreting a chat prefix, resolving a command or executing anything.</summary>
	public static CommandTokenization TokenizeCommandText( string text ) => new( CommandTokenizer.Tokenize( text ) );

	/// <summary>Converts argument tokens against metadata and a host-supplied online snapshot; parsing grants no authority.</summary>
	public static CommandParseResult ParseCommandArguments( CommandDescriptor descriptor, IReadOnlyList<string> tokens,
		IReadOnlyList<CommandPlayer> onlinePlayers ) => CommandArgumentParser.Parse( descriptor, tokens, onlinePlayers );

	/// <summary>Generates syntax and constraints from one descriptor; caller-specific visibility belongs to the dispatcher.</summary>
	public static CommandSyntax GetCommandSyntax( CommandDescriptor descriptor ) => CommandSyntaxBuilder.Build( descriptor );
}
