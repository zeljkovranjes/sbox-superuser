namespace SboxSuperuser.Core.Moderation;

internal sealed class ModerationLedger
{
	internal const int MaximumRecords = 512;
	private readonly Dictionary<(string Target, ModerationKind Kind), ModerationEntry> _active = new();
	internal IReadOnlyList<ModerationEntry> Entries { get; }
	internal long Revision { get; }
	internal int ReservedRevocations => Entries.Count == 0 ? 0 : _active.Values.Count( entry => !entry.ExpiresUtc.HasValue || entry.ExpiresUtc > Entries[ ^1 ].CreatedUtc );

	internal ModerationLedger( long revision, IEnumerable<ModerationEntry> entries )
	{
		if ( revision < 0 ) throw new ArgumentOutOfRangeException( nameof( revision ) );
		ArgumentNullException.ThrowIfNull( entries );
		var snapshot = entries.Take( MaximumRecords + 1 ).ToArray();
		if ( snapshot.Length > MaximumRecords || snapshot.Any( entry => entry is null ) )
			throw new ArgumentException( "Moderation history capacity exceeded or invalid entries supplied." );
		var ids = new HashSet<Guid>();
		foreach ( var entry in snapshot )
		{
			if ( !ids.Add( entry.Id ) ) throw new ArgumentException( "Duplicate moderation action ID." );
			if ( entry.Kind is ModerationKind.Ban or ModerationKind.TextMute )
			{
				if ( Active( entry.Target, entry.Kind, entry.CreatedUtc ) is not null )
					throw new ArgumentException( "An active restriction already exists." );
				_active[ (entry.Target, entry.Kind) ] = entry;
			}
			else if ( entry.Kind is ModerationKind.Unban or ModerationKind.Unmute )
			{
				var kind = entry.Kind == ModerationKind.Unban ? ModerationKind.Ban : ModerationKind.TextMute;
				var original = Active( entry.Target, kind, entry.CreatedUtc );
				if ( original is null || original.Id != entry.Revokes )
					throw new ArgumentException( "Revocation must link the target's active original restriction." );
				_active.Remove( (entry.Target, kind) );
			}
		}
		Revision = revision;
		Entries = Array.AsReadOnly( snapshot );
		// Every active restriction keeps room for its eventual revocation, even when other history fills up.
		if ( snapshot.Length + ReservedRevocations > MaximumRecords )
			throw new ArgumentException( "Moderation capacity reserves one revocation record per active restriction." );
	}

	internal ModerationEntry Active( string target, ModerationKind kind, DateTimeOffset now )
	{
		return _active.TryGetValue( (target, kind), out var entry )
			&& (!entry.ExpiresUtc.HasValue || entry.ExpiresUtc > now) ? entry : null;
	}

	internal ModerationLedger Append( ModerationEntry entry )
	{
		if ( Revision == long.MaxValue ) throw new InvalidOperationException( "Moderation revision limit reached." );
		return new( Revision + 1, Entries.Append( entry ) );
	}
}
