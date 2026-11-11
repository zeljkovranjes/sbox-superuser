using SboxSuperuser.Core.Messages;
using SboxSuperuser.Engine.Messages;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	internal const string MessagesPermission = "admin.messages.manage";

	private void RegisterMessageTemplates( HostMessageService messages )
	{
		Register( "messages.list", "List every message players see and its current wording", MessagesPermission,
			new[] { Page() }, (_, arguments) => Task.FromResult( Rows( MessageCatalog.Defaults.Keys.OrderBy( key => key, StringComparer.Ordinal )
				.Select( key => $"{key}: {messages.Catalog.Template( key )}" ), PageNumber( arguments ), "Messages; change one with admin messages set <key> <wording>." ) ) );
		Register( "messages.set", "Change a message's wording; admin messages list shows each key and its placeholders", MessagesPermission,
			new[] { Key(), new CommandArgumentDescriptor( "wording", $"One printable line of at most {MessageCatalog.MaximumLength} characters",
				CommandArgumentKind.String, isRemainder: true ) },
			(context, arguments) => messages.SetAsync( context, "messages.set", arguments.Get<string>( "key" ), arguments.Get<string>( "wording" ) ), mutation: true );
		Register( "messages.reset", "Put a message back to Superuser's own wording", MessagesPermission,
			new[] { Key() }, (context, arguments) => messages.SetAsync( context, "messages.reset", arguments.Get<string>( "key" ), null ), mutation: true );
	}

	// Keys are a fixed set of short words, safe to keep in the audit log.
	private static CommandArgumentDescriptor Key() => new( "key", "Message key such as ban, kick or tag", CommandArgumentKind.String );
}
