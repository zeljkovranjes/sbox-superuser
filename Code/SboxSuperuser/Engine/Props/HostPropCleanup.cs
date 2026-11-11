using SboxSuperuser.Core.Props;
using SboxSuperuser.Engine.Storage;

namespace SboxSuperuser.Engine.Props;

/// <summary>The server's prop cleanup delay, kept in storage; until it loads, the default of two minutes is used.</summary>
internal sealed class HostPropCleanup
{
	internal static StoreKey Key { get; } = new( "props", "cleanup" );

	private readonly AuditedDocument _document;

	internal PropCleanup Current { get; private set; } = PropCleanup.Default;
	internal Task Initialization => _document.Initialization;

	internal HostPropCleanup( IAdminStore store, Func<bool> available, Action<Exception> report, Func<DateTimeOffset> clock = null ) =>
		_document = new AuditedDocument( store, Key, available, state => Current = PropCleanupCodec.ReadState( state ), report, clock );

	internal void OnStorageChanged( StoreKey key ) => _document.OnStorageChanged( key );

	/// <summary>Saves a new delay; null keeps props until someone cleans up.</summary>
	internal Task<CommandResult> SetAsync( CommandContext context, string command, TimeSpan? delay )
	{
		if ( PropCleanup.Problem( delay ) is { } problem ) return Task.FromResult( new CommandResult( CommandStatus.InvalidArguments, problem ) );
		if ( delay == Current.Delay ) return Task.FromResult( new CommandResult( CommandStatus.NoChange, Describe( delay ) ) );
		return _document.SaveAsync( context, command, PropCleanupCodec.WriteState( new PropCleanup( delay ) ), Describe( delay ) );
	}

	/// <summary>The delay as players read it.</summary>
	internal static string Describe( TimeSpan? delay ) => delay is { } value
		? $"Props are removed {Core.Messages.MessageCatalog.Length( value )} after their owner leaves."
		: "Props stay after their owner leaves until someone cleans up.";
}
