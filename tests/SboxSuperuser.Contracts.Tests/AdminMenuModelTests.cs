using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminMenuModelTests
{
	[Fact]
	public void LateReplyCannotReplaceNewSelection()
	{
		var ids = new List<Guid>();
		var model = new AdminMenuModel( _ => { var id = Guid.NewGuid(); ids.Add( id ); return id; }, _ => Guid.NewGuid() );
		model.Select( DispatcherRig.TargetId );
		model.Select( DispatcherRig.UserId );
		model.Receive( new AdminReply { RequestId = ids[ 0 ], Status = CommandStatus.Success, SteamId = DispatcherRig.TargetId } );
		Assert.Null( model.Details );
		model.Receive( new AdminReply { RequestId = ids[ 1 ], Status = CommandStatus.Success, SteamId = DispatcherRig.UserId } );
		Assert.Equal( DispatcherRig.UserId, model.Details.SteamId );
	}

	[Fact]
	public void ConfirmationFreezesInputAndPendingActionCannotBeSubmittedTwice()
	{
		var queryId = Guid.NewGuid();
		var actionId = Guid.NewGuid();
		var actions = new List<AdminActionRequest>();
		var model = new AdminMenuModel( _ => queryId, request => { actions.Add( request ); return actionId; } );
		model.Select( DispatcherRig.TargetId );
		model.Receive( new AdminReply { RequestId = queryId, Status = CommandStatus.Success, Actions = new[] { AdminAction.Ban } } );
		model.Reason = "Original";
		model.Duration = "1h";
		model.Prepare( AdminAction.Ban );
		model.Reason = "Changed";
		model.Duration = "permanent";
		model.Confirm();
		model.Confirm();
		Assert.Equal( "Original", Assert.Single( actions ).Reason );
		Assert.Equal( "1h", actions[ 0 ].Duration );
		Assert.True( model.Busy );
		Assert.Null( model.FeedbackStatus );
		model.Receive( new AdminReply { RequestId = actionId, Status = CommandStatus.Unknown, Message = "Inspect before retrying." } );
		Assert.False( model.Busy );
		Assert.Contains( "Unknown", model.Feedback );
		Assert.Equal( CommandStatus.Unknown, model.FeedbackStatus );
		Assert.Null( model.Confirmation );
		Assert.Null( model.Details );
	}

	[Fact]
	public void DeniedRefreshClearsPrivateSelectionAndConfirmation()
	{
		var id = Guid.NewGuid();
		var model = new AdminMenuModel( _ => id, _ => Guid.NewGuid() );
		model.Select( DispatcherRig.TargetId );
		model.Receive( new AdminReply { RequestId = id, Status = CommandStatus.Success, Actions = new[] { AdminAction.Warn } } );
		model.Reason = "Reason";
		model.Prepare( AdminAction.Warn );
		Assert.NotNull( model.Confirmation );
		model.Refresh();
		model.Receive( new AdminReply { RequestId = id, Status = CommandStatus.Denied } );
		Assert.Null( model.Details );
		Assert.Null( model.Confirmation );
	}

	[Fact]
	public void ServerBoundsConcurrentAndBurstRequestsIndependently()
	{
		var gate = new AdminRequestGate();
		var client = Guid.NewGuid();
		var now = DateTimeOffset.UtcNow;
		Assert.True( gate.TryEnter( client, now ) );
		Assert.True( gate.TryEnter( client, now ) );
		Assert.False( gate.TryEnter( client, now ) );
		gate.Exit( client );
		gate.Exit( client );
		for ( var i = 0; i < 6; i++ ) { Assert.True( gate.TryEnter( client, now ) ); gate.Exit( client ); }
		Assert.False( gate.TryEnter( client, now ) );
		Assert.True( gate.TryEnter( client, now.AddSeconds( 2 ) ) );
	}
}
