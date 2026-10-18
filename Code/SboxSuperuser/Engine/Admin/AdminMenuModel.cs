namespace SboxSuperuser.Engine.Admin;

internal sealed class AdminMenuModel
{
	private readonly Func<AdminQuery, Guid> _query;
	private readonly Func<AdminActionRequest, Guid> _action;
	private Guid _playersRequest;
	private Guid _detailsRequest;
	private Guid _activityRequest;
	private Guid _actionRequest;
	private DateTimeOffset _refreshAfter;
	internal AdminReply Players { get; private set; }
	internal AdminReply Details { get; private set; }
	internal AdminReply Activity { get; private set; }
	internal AdminActionRequest Confirmation { get; private set; }
	internal string Feedback { get; private set; } = "";
	/// <summary>Outcome behind Feedback; null while an action waits for the host.</summary>
	internal CommandStatus? FeedbackStatus { get; private set; }
	internal string Selected { get; private set; } = "";
	internal bool Busy => _actionRequest != Guid.Empty;
	internal int PlayerPage;
	internal int HistoryPage;
	internal string Search = "";
	internal string Lookup = "";
	internal string Reason = "";
	internal string Duration = "30m";

	internal AdminMenuModel( Func<AdminQuery, Guid> query, Func<AdminActionRequest, Guid> action )
	{
		_query = query;
		_action = action;
	}

	internal void Tick( DateTimeOffset now )
	{
		if ( now >= _refreshAfter ) Refresh();
	}

	internal void Refresh()
	{
		_refreshAfter = DateTimeOffset.UtcNow.AddSeconds( 5 );
		Try( () =>
		{
			if ( _playersRequest == Guid.Empty ) _playersRequest = _query( new AdminQuery( AdminQueryKind.Players, Search, PlayerPage ) );
			if ( Selected.Length != 0 && _detailsRequest == Guid.Empty )
				_detailsRequest = _query( new AdminQuery( AdminQueryKind.Player, Selected, HistoryPage ) );
			if ( _activityRequest == Guid.Empty ) _activityRequest = _query( new AdminQuery( AdminQueryKind.Activity ) );
		} );
	}

	internal void Select( string steamId )
	{
		Try( () =>
		{
			var request = new AdminQuery( AdminQueryKind.Player, steamId );
			Selected = steamId;
			Details = null;
			HistoryPage = 0;
			Confirmation = null;
			_detailsRequest = _query( request );
		} );
	}

	internal void Prepare( AdminAction action )
	{
		if ( Busy || Details?.Actions.Contains( action ) != true ) return;
		Try( () => Confirmation = new AdminActionRequest( action, Selected, Reason,
			action is AdminAction.Ban or AdminAction.Mute ? Duration : "" ) );
	}

	internal void Cancel() => Confirmation = null;

	internal void Confirm()
	{
		if ( Confirmation is null || Busy ) return;
		Try( () =>
		{
			_actionRequest = _action( Confirmation );
			Confirmation = null;
			Feedback = "Waiting for the host...";
			FeedbackStatus = null;
		} );
	}

	internal void Receive( AdminReply reply )
	{
		if ( reply.RequestId == _playersRequest )
		{
			_playersRequest = Guid.Empty;
			Players = reply;
			if ( reply.Status != CommandStatus.Success )
			{
				Details = null;
				Confirmation = null;
				_detailsRequest = Guid.Empty;
			}
		}
		else if ( reply.RequestId == _detailsRequest )
		{
			_detailsRequest = Guid.Empty;
			Details = reply;
			if ( reply.Status != CommandStatus.Success ) Confirmation = null;
		}
		else if ( reply.RequestId == _activityRequest )
		{
			_activityRequest = Guid.Empty;
			Activity = reply;
		}
		else if ( reply.RequestId == _actionRequest )
		{
			_actionRequest = Guid.Empty;
			Feedback = $"{reply.Status}: {reply.Message}  Request: {reply.RequestId:N}";
			FeedbackStatus = reply.Status;
			Details = null;
			_refreshAfter = DateTimeOffset.MinValue;
		}
	}

	private void Try( Action action )
	{
		try { action(); }
		catch ( ArgumentException error ) { Fail( error.Message ); }
		catch ( InvalidOperationException error ) { Fail( error.Message ); }
	}

	private void Fail( string message )
	{
		Feedback = message;
		FeedbackStatus = CommandStatus.InvalidArguments;
	}
}
