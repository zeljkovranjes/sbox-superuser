using SboxSuperuser.Core.Content;
using SboxSuperuser.Engine.Storage;

namespace SboxSuperuser.Engine.Content;

/// <summary>The server's banned maps, addons and entities, kept in storage and changed with an audit trail.</summary>
internal sealed class HostContentService
{
	internal static StoreKey Key { get; } = new( "content", "bans" );
	private const int MaximumReasonLength = 256;

	private readonly AuditedDocument _document;

	internal ContentBanList Bans { get; } = new();
	internal Task Initialization => _document.Initialization;

	internal HostContentService( IAdminStore store, Func<bool> available, Action<Exception> report, Func<DateTimeOffset> clock = null ) =>
		_document = new AuditedDocument( store, Key, available, state => Bans.Load( ContentBanCodec.ReadState( state ) ), report, clock );

	internal void OnStorageChanged( StoreKey key ) => _document.OnStorageChanged( key );

	/// <summary>Whether this item may be used here; a banned map, addon or entity is not.</summary>
	internal bool IsAllowed( string kind, string id ) => Bans.Find( kind, id ) is null;

	internal Task<CommandResult> BanAsync( CommandContext context, string kind, string pattern, string reason )
	{
		kind = kind?.ToLowerInvariant();
		if ( ContentBanList.Problem( kind, pattern ) is { } problem ) return Invalid( problem );
		reason = reason?.Trim() ?? "";
		if ( reason.Length > MaximumReasonLength || reason.Any( char.IsControl ) ) return Invalid( $"Reasons are printable text of at most {MaximumReasonLength} characters." );
		var bans = Bans.Bans.Where( ban => !Same( ban, kind, pattern ) ).Append( new ContentBan( kind, pattern, reason ) ).ToArray();
		if ( bans.Length > ContentBanList.MaximumBans ) return Invalid( $"At most {ContentBanList.MaximumBans} content bans." );
		return _document.SaveAsync( context, "content.ban", ContentBanCodec.WriteState( bans ), $"Banned {kind} {pattern}." );
	}

	internal Task<CommandResult> UnbanAsync( CommandContext context, string kind, string pattern )
	{
		kind = kind?.ToLowerInvariant();
		if ( !Bans.Bans.Any( ban => Same( ban, kind, pattern ) ) )
			return Task.FromResult( new CommandResult( CommandStatus.NoChange, $"{kind} {pattern} isn't banned." ) );
		return _document.SaveAsync( context, "content.unban", ContentBanCodec.WriteState( Bans.Bans.Where( ban => !Same( ban, kind, pattern ) ) ), $"Unbanned {kind} {pattern}." );
	}

	private static bool Same( ContentBan ban, string kind, string pattern ) => ban.Kind == kind && string.Equals( ban.Pattern, pattern, StringComparison.OrdinalIgnoreCase );

	private static Task<CommandResult> Invalid( string problem ) => Task.FromResult( new CommandResult( CommandStatus.InvalidArguments, problem ) );
}
