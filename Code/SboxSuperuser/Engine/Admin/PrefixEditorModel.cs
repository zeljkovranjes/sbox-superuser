using System.Text.Json;
using SboxSuperuser.Core.Prefixes;

namespace SboxSuperuser.Engine.Admin;

/// <summary>
/// What the Prefixes window edits: the full catalog (read with prefix.export) and a queue of prefix commands sent
/// one at a time, so a multi-field save never trips the host's request limits. A failed command drops the rest.
/// </summary>
internal sealed class PrefixEditorModel
{
	private readonly Func<AdminCommandRequest, Guid> _command;
	private readonly Queue<AdminCommandRequest> _queue = new();
	private Guid _exportRequest;
	private Guid _editRequest;
	private bool _stale = true;

	internal PrefixEditorModel( Func<AdminCommandRequest, Guid> command ) => _command = command;

	/// <summary>The catalog as the host last reported it, or null before the first answer.</summary>
	internal PrefixCatalog Catalog { get; private set; }

	/// <summary>Every group on the server with its protection level, owner first, highest protection next.</summary>
	internal IReadOnlyList<GroupRank> Groups { get; private set; } = Array.Empty<GroupRank>();

	/// <summary>Why the catalog couldn't be read, or null.</summary>
	internal string Problem { get; private set; }

	/// <summary>The most recent edit's outcome.</summary>
	internal AdminReply LastResult { get; private set; }

	internal bool Busy => _editRequest != Guid.Empty || _queue.Count > 0;

	/// <summary>Reads the catalog the first time and after edits; call every frame the window shows.</summary>
	internal void Request()
	{
		if ( !_stale || _exportRequest != Guid.Empty || Busy ) return;
		_stale = false;
		_exportRequest = Send( new AdminCommandRequest( "prefix.export" ) );
	}

	internal void Refresh() => _stale = true;

	/// <summary>Adds a prefix command to the queue; arguments that are too long are reported, not sent.</summary>
	internal void Queue( string command, IEnumerable<string> arguments )
	{
		try
		{
			_queue.Enqueue( new AdminCommandRequest( command, arguments ) );
		}
		catch ( ArgumentException error )
		{
			LastResult = new AdminReply { Status = CommandStatus.InvalidArguments, Message = error.Message };
			return;
		}
		SendNext();
	}

	internal bool Receive( AdminReply reply )
	{
		if ( reply.RequestId == Guid.Empty ) return false;
		if ( reply.RequestId == _exportRequest )
		{
			_exportRequest = Guid.Empty;
			Read( reply );
			return true;
		}
		if ( reply.RequestId != _editRequest ) return false;
		_editRequest = Guid.Empty;
		LastResult = reply;
		if ( reply.Status is not (CommandStatus.Success or CommandStatus.NoChange) ) _queue.Clear();
		SendNext();
		if ( !Busy ) _stale = true;
		return true;
	}

	private void Read( AdminReply reply )
	{
		if ( reply.Status != CommandStatus.Success || !reply.Data.TryGetValue( "catalog", out var state ) )
		{
			Problem = reply.Message;
			return;
		}
		try
		{
			using var json = JsonDocument.Parse( state );
			Catalog = PrefixCodec.ReadState( json.RootElement );
			Groups = ReadGroups( reply.Data.GetValueOrDefault( "groups" ) ?? "" );
			Problem = null;
		}
		catch ( Exception error ) when ( error is JsonException or FormatException or ArgumentException or KeyNotFoundException or InvalidOperationException )
		{
			Problem = "The host sent prefixes this version can't read.";
		}
	}

	private static GroupRank[] ReadGroups( string text ) => text
		.Split( ',', StringSplitOptions.RemoveEmptyEntries )
		.Select( pair => pair.Split( ':' ) )
		.Where( parts => parts.Length == 2 && int.TryParse( parts[ 1 ], out _ ) )
		.Select( parts => new GroupRank( parts[ 0 ], int.Parse( parts[ 1 ] ) ) )
		.OrderByDescending( group => group.Protection )
		.ThenBy( group => group.GroupId, StringComparer.Ordinal )
		.ToArray();

	private void SendNext()
	{
		if ( _editRequest != Guid.Empty || _queue.Count == 0 ) return;
		_editRequest = Send( _queue.Dequeue() );
		if ( _editRequest == Guid.Empty ) _queue.Clear();
	}

	private Guid Send( AdminCommandRequest request )
	{
		try
		{
			return _command( request );
		}
		catch ( InvalidOperationException error )
		{
			LastResult = new AdminReply { Status = CommandStatus.Conflict, Message = error.Message };
			return Guid.Empty;
		}
	}
}
