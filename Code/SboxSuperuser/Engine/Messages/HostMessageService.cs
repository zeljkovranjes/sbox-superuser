using SboxSuperuser.Core.Messages;
using SboxSuperuser.Engine.Storage;

namespace SboxSuperuser.Engine.Messages;

/// <summary>The server's message wording, kept in storage; until it loads, the built-in wording is used.</summary>
internal sealed class HostMessageService
{
	internal static StoreKey Key { get; } = new( "messages", "templates" );

	private readonly AuditedDocument _document;

	internal MessageCatalog Catalog { get; } = new();
	internal Task Initialization => _document.Initialization;

	internal HostMessageService( IAdminStore store, Func<bool> available, Action<Exception> report, Func<DateTimeOffset> clock = null ) =>
		_document = new AuditedDocument( store, Key, available, state => Catalog.Load( MessageCodec.ReadState( state ) ), report, clock );

	internal void OnStorageChanged( StoreKey key ) => _document.OnStorageChanged( key );

	/// <summary>Saves a new template for the key, or the built-in one again when template is null.</summary>
	internal Task<CommandResult> SetAsync( CommandContext context, string command, string key, string template )
	{
		if ( !MessageCatalog.Defaults.ContainsKey( key ) ) return Task.FromResult( new CommandResult( CommandStatus.NotFound, $"There is no message called {key}." ) );
		if ( template is not null && MessageCatalog.Problem( key, template ) is { } problem ) return Task.FromResult( new CommandResult( CommandStatus.InvalidArguments, problem ) );
		var next = Catalog.Overrides.ToDictionary( pair => pair.Key, pair => pair.Value, StringComparer.Ordinal );
		// Saving the built-in wording is the same as resetting, so storage only ever holds real changes.
		if ( template is null || template == MessageCatalog.Defaults[ key ].Default ) next.Remove( key );
		else next[ key ] = template;
		var wording = template ?? MessageCatalog.Defaults[ key ].Default;
		return _document.SaveAsync( context, command, MessageCodec.WriteState( next ), template is null ? $"{key} is back to: {wording}" : $"{key} is now: {wording}" );
	}
}
