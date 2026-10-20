namespace SboxSuperuser.Engine.Admin;

/// <summary>
/// Client state for the server-wide admin views: permitted commands, active restrictions, permission policy and the
/// result of the last command run from the menu. Requests go out only when a view asks; replies are matched by ID.
/// </summary>
internal sealed class AdminServerModel
{
	private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds( 5 );
	private static readonly TimeSpan ChatInterval = TimeSpan.FromSeconds( 2 );

	private readonly Func<AdminQuery, Guid> _query;
	private readonly Func<AdminCommandRequest, Guid> _command;
	private Guid _commandsRequest;
	private Guid _restrictionsRequest;
	private Guid _permissionsRequest;
	private Guid _chatRequest;
	private DateTimeOffset _chatAfter;
	private Guid _runRequest;
	private DateTimeOffset _restrictionsAfter;
	private bool _permissionsStale = true;
	private Guid _documentationRequest;
	private Guid _menuWindowsRequest;
	private readonly Dictionary<Guid, string> _liveRequests = new();
	private readonly Dictionary<string, (AdminReply Reply, DateTimeOffset At)> _live = new( StringComparer.Ordinal );
	private readonly Dictionary<Guid, (string Module, int Page)> _pageRequests = new();
	private readonly Dictionary<(string Module, int Page), AdminReply> _pages = new();

	internal AdminReply Commands { get; private set; }
	internal AdminReply Restrictions { get; private set; }
	internal AdminReply Permissions { get; private set; }
	internal AdminReply Chat { get; private set; }
	internal AdminReply Documentation { get; private set; }

	/// <summary>The windows modules added to the menu, filtered by the host to what this viewer may use.</summary>
	internal AdminReply MenuWindows { get; private set; }

	/// <summary>Reply to the last command run from the menu, and which command it was.</summary>
	internal AdminReply LastResult { get; private set; }
	internal string LastCommand { get; private set; } = "";
	internal bool Running => _runRequest != Guid.Empty;

	internal string RestrictionSearch = "";
	internal int RestrictionPage;

	internal AdminServerModel( Func<AdminQuery, Guid> query, Func<AdminCommandRequest, Guid> command )
	{
		_query = query;
		_command = command;
	}

	/// <summary>True once the command list says the caller may run this command.</summary>
	internal bool Can( string command ) => Commands?.Commands.Any( row => row.Name == command ) == true;

	internal void RequestCommands()
	{
		if ( Commands is not null || _commandsRequest != Guid.Empty ) return;
		Try( () => _commandsRequest = _query( new AdminQuery( AdminQueryKind.Commands ) ) );
	}

	/// <summary>Keeps the restrictions list fresh while its window is open.</summary>
	internal void RequestRestrictions( DateTimeOffset now, bool force = false )
	{
		if ( _restrictionsRequest != Guid.Empty || !force && now < _restrictionsAfter ) return;
		_restrictionsAfter = now + RefreshInterval;
		Try( () => _restrictionsRequest = _query( new AdminQuery( AdminQueryKind.Restrictions, RestrictionSearch, RestrictionPage ) ) );
	}

	/// <summary>Keeps recent chat fresh while a view shows it; chat moves faster than restrictions.</summary>
	internal void RequestChat( DateTimeOffset now )
	{
		if ( _chatRequest != Guid.Empty || now < _chatAfter ) return;
		_chatAfter = now + ChatInterval;
		Try( () => _chatRequest = _query( new AdminQuery( AdminQueryKind.Chat ) ) );
	}

	/// <summary>Loads the documented-module list once; Refresh reloads it with every page.</summary>
	internal void RequestDocumentation()
	{
		if ( Documentation is not null || _documentationRequest != Guid.Empty ) return;
		Try( () => _documentationRequest = _query( new AdminQuery( AdminQueryKind.Documentation ) ) );
	}

	internal void RefreshDocumentation()
	{
		Documentation = null;
		_pages.Clear();
		RequestDocumentation();
	}

	/// <summary>Returns a cached documentation page, asking the host for it the first time; null while it loads.</summary>
	internal AdminReply DocumentationPage( string module, int page )
	{
		if ( _pages.TryGetValue( (module, page), out var reply ) ) return reply;
		if ( !_pageRequests.ContainsValue( (module, page) ) )
			Try( () => _pageRequests[ _query( new AdminQuery( AdminQueryKind.Documentation, module, page ) ) ] = (module, page) );
		return null;
	}

	/// <summary>Loads the module windows once; Refresh reloads them, for example when the menu opens.</summary>
	internal void RequestMenuWindows()
	{
		if ( MenuWindows is not null || _menuWindowsRequest != Guid.Empty ) return;
		Try( () => _menuWindowsRequest = _query( new AdminQuery( AdminQueryKind.MenuWindows ) ) );
	}

	internal void RefreshMenuWindows()
	{
		MenuWindows = null;
		_live.Clear();
		RequestMenuWindows();
	}

	/// <summary>
	/// A live element's latest reply; null while the first one loads. It asks again every few seconds while shown and
	/// right after RefreshLive, keeping the old reply on screen until the new one arrives.
	/// </summary>
	internal AdminReply Live( string key, string command, IReadOnlyList<string> arguments, DateTimeOffset now )
	{
		var cached = _live.TryGetValue( key, out var entry );
		if ( (!cached || now - entry.At >= RefreshInterval) && !_liveRequests.ContainsValue( key ) )
			Try( () => _liveRequests[ _command( new AdminCommandRequest( command, arguments ) ) ] = key );
		return cached ? entry.Reply : null;
	}

	/// <summary>Makes every live element ask again now; called after any command runs from the menu.</summary>
	internal void RefreshLive()
	{
		foreach ( var key in _live.Keys.ToArray() ) _live[ key ] = (_live[ key ].Reply, DateTimeOffset.MinValue);
	}

	/// <summary>Reloads the policy when it is first shown and after every command that may have changed it.</summary>
	internal void RequestPermissions()
	{
		if ( !_permissionsStale || _permissionsRequest != Guid.Empty ) return;
		_permissionsStale = false;
		Try( () => _permissionsRequest = _query( new AdminQuery( AdminQueryKind.Permissions ) ) );
	}

	/// <summary>Runs one command; returns false while another is still waiting for the host.</summary>
	internal bool Run( string command, IEnumerable<string> arguments )
	{
		if ( Running ) return false;
		LastCommand = command;
		LastResult = null;
		Try( () => _runRequest = _command( new AdminCommandRequest( command, arguments ) ) );
		return Running;
	}

	/// <summary>Takes a reply meant for this model; returns false when the reply belongs to someone else.</summary>
	internal bool Receive( AdminReply reply )
	{
		if ( reply.RequestId == _commandsRequest )
		{
			_commandsRequest = Guid.Empty;
			Commands = reply;
		}
		else if ( reply.RequestId == _restrictionsRequest )
		{
			_restrictionsRequest = Guid.Empty;
			Restrictions = reply;
		}
		else if ( reply.RequestId == _permissionsRequest )
		{
			_permissionsRequest = Guid.Empty;
			Permissions = reply;
		}
		else if ( reply.RequestId == _chatRequest )
		{
			_chatRequest = Guid.Empty;
			Chat = reply;
		}
		else if ( reply.RequestId == _documentationRequest )
		{
			_documentationRequest = Guid.Empty;
			Documentation = reply;
		}
		else if ( reply.RequestId == _menuWindowsRequest )
		{
			_menuWindowsRequest = Guid.Empty;
			MenuWindows = reply;
		}
		else if ( _liveRequests.Remove( reply.RequestId, out var live ) )
		{
			_live[ live ] = (reply, DateTimeOffset.UtcNow);
		}
		else if ( _pageRequests.Remove( reply.RequestId, out var page ) )
		{
			_pages[ page ] = reply;
		}
		else if ( reply.RequestId == _runRequest )
		{
			_runRequest = Guid.Empty;
			LastResult = reply;
			// Any command may have changed restrictions or policy, so both views reload.
			_restrictionsAfter = DateTimeOffset.MinValue;
			_permissionsStale = true;
		}
		else
		{
			return false;
		}
		return true;
	}

	private void Try( Action action )
	{
		try { action(); }
		catch ( ArgumentException error ) { Fail( error.Message ); }
		catch ( InvalidOperationException error ) { Fail( error.Message ); }
	}

	private void Fail( string message ) => LastResult = new AdminReply { Status = CommandStatus.InvalidArguments, Message = message };
}
