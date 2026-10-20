using System.Text.Json;
using SboxSuperuser.Core.Prefixes;

namespace SboxSuperuser.Engine.Prefixes;

/// <summary>
/// The shipped Prefix module. It uses only the public module API (commands, permissions, documentation and the
/// store), like any third-party module, and can be disabled with admin modules disable superuser.prefixes.
/// </summary>
internal sealed partial class PrefixModule : IAdminModule
{
	internal const string Id = "superuser.prefixes";
	internal const string ManagePermission = "admin.prefixes.manage";
	internal const string OwnerGroup = "owner";
	internal static readonly StoreKey Key = new( Id, "catalog" );

	private readonly PrefixHost _host;
	private PrefixCatalog _catalog = PrefixCatalog.Empty;
	private long _revision;
	private string _problem;
	private bool _running;

	internal PrefixModule( PrefixHost host ) => _host = host;

	public ModuleDescriptor Descriptor { get; } = new( Id, "Styled, animated name prefixes for groups and players", 1 )
	{
		Version = Core.Contracts.ProductInfo.Version,
		Author = Core.Contracts.ProductInfo.Author,
		Website = Core.Contracts.ProductInfo.Website
	};

	internal PrefixCatalog Catalog => _catalog;

	public async Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		if ( !_host.PermissionKnown( ManagePermission ) )
			registration.RegisterPermission( new PermissionDescriptor( ManagePermission, "Create, edit and assign name prefixes" ) );
		RegisterCommands( registration );
		RegisterDocumentation( registration );
		await LoadAsync( cancellation );
		_running = true;
	}

	public Task StopAsync( CancellationToken cancellation )
	{
		_running = false;
		return Task.CompletedTask;
	}

	/// <summary>What clients should see now: the prefixes plus each online player's resolved prefix.</summary>
	internal PrefixSnapshot Snapshot()
	{
		if ( !_running ) return PrefixSnapshot.Disabled;
		var players = new Dictionary<string, string>( StringComparer.Ordinal );
		foreach ( var steamId in _host.OnlinePlayers().Take( PrefixCodec.MaximumSnapshotPlayers ) )
		{
			var prefix = _catalog.Resolve( steamId, _host.GroupsOf( steamId ) );
			if ( prefix is not null ) players[ steamId ] = prefix.Id;
		}
		return new PrefixSnapshot( true, _host.Chat, _catalog.Prefixes, players );
	}

	private async Task LoadAsync( CancellationToken cancellation )
	{
		_problem = null;
		var read = await _host.Store.ReadAsync( Key, PrefixCodec.Schema, cancellation );
		switch ( read.Status )
		{
			case StoreReadStatus.Missing:
				_catalog = PrefixStarter.Catalog();
				_revision = 0;
				return;
			case StoreReadStatus.Found:
				try
				{
					using var json = JsonDocument.Parse( read.Document.PayloadJson );
					_catalog = PrefixCodec.ReadState( json.RootElement.GetProperty( "state" ) );
					_revision = read.Document.Revision;
				}
				catch ( Exception error ) when ( error is FormatException or ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException )
				{
					_problem = "Stored prefixes are unreadable; they were left untouched. Restore a backup to edit them.";
				}
				return;
			default:
				_problem = $"Prefix storage is {read.Status}; prefixes can't be changed until it recovers.";
				return;
		}
	}

	// Writes a changed catalog as one audited store transaction; the expected revision rejects lost updates.
	private async Task<CommandResult> SaveAsync( CommandContext context, string command, PrefixCatalog candidate, string done )
	{
		if ( _problem is not null ) return new CommandResult( CommandStatus.StorageUnavailable, _problem );
		string state;
		try
		{
			state = PrefixCodec.WriteState( candidate );
		}
		catch ( InvalidOperationException error )
		{
			return new CommandResult( CommandStatus.InvalidArguments, error.Message );
		}

		var intent = new AuditIntent( Guid.NewGuid(), context.RequestId, context.Actor.Kind, context.Actor.SubjectId,
			context.Origin, command, DateTimeOffset.UtcNow );
		var transaction = new PolicyTransaction( state, new[] { intent } );
		var result = await _host.Store.WriteAsync( transaction.CreateWriteRequest( Key, _revision ), context.Cancellation );
		switch ( result.Status )
		{
			case StoreWriteStatus.Committed:
				_catalog = candidate;
				_revision = result.CommittedRevision ?? _revision + 1;
						return new CommandResult( CommandStatus.Success, done );
			case StoreWriteStatus.Conflict:
				await LoadAsync( CancellationToken.None );
				return new CommandResult( CommandStatus.Conflict, "Prefixes changed at the same time; reload and try again." );
			case StoreWriteStatus.Unknown:
				return new CommandResult( CommandStatus.Unknown, "The change may or may not have been saved; check the prefix list before retrying." );
			default:
				return new CommandResult( CommandStatus.StorageUnavailable, $"The change was not saved: storage is {result.Status}." );
		}
	}
}
