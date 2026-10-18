using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminServerModelTests
{
	private sealed class Host
	{
		internal readonly List<(Guid Id, AdminQuery Query)> Queries = new();
		internal readonly List<(Guid Id, AdminCommandRequest Command)> Commands = new();

		internal Guid Query( AdminQuery query )
		{
			var id = Guid.NewGuid();
			Queries.Add( (id, query) );
			return id;
		}

		internal Guid Command( AdminCommandRequest command )
		{
			var id = Guid.NewGuid();
			Commands.Add( (id, command) );
			return id;
		}
	}

	[Fact]
	public void OnlyOneCommandRunsAtATimeAndItsReplyRefreshesViews()
	{
		var host = new Host();
		var model = new AdminServerModel( host.Query, host.Command );
		model.RequestPermissions();
		model.Receive( new AdminReply { RequestId = host.Queries[ 0 ].Id, Status = CommandStatus.Success } );
		model.RequestPermissions();
		Assert.Single( host.Queries );

		Assert.True( model.Run( "group.create", new[] { "helpers" } ) );
		Assert.False( model.Run( "group.create", new[] { "other" } ) );
		Assert.True( model.Receive( new AdminReply { RequestId = host.Commands[ 0 ].Id, Status = CommandStatus.Success, Message = "Created." } ) );
		Assert.Equal( "group.create", model.LastCommand );
		Assert.Equal( "Created.", model.LastResult.Message );

		model.RequestPermissions();
		Assert.Equal( 2, host.Queries.Count );
	}

	[Fact]
	public void RepliesForOtherViewsAreLeftAlone()
	{
		var model = new AdminServerModel( _ => Guid.NewGuid(), _ => Guid.NewGuid() );
		Assert.False( model.Receive( new AdminReply { RequestId = Guid.NewGuid() } ) );
	}

	[Fact]
	public void InvalidCommandInputBecomesALocalResult()
	{
		var host = new Host();
		var model = new AdminServerModel( host.Query, host.Command );
		Assert.False( model.Run( "warn", new[] { "bad\nline" } ) );
		Assert.Empty( host.Commands );
		Assert.Equal( CommandStatus.InvalidArguments, model.LastResult.Status );
	}

	[Fact]
	public void CommandListAnswersWhatTheCallerMayRun()
	{
		var host = new Host();
		var model = new AdminServerModel( host.Query, host.Command );
		model.RequestCommands();
		model.Receive( new AdminReply
		{
			RequestId = host.Queries[ 0 ].Id, Status = CommandStatus.Success,
			Commands = new[] { new AdminCommandRow { Name = "note" } }
		} );
		Assert.True( model.Can( "note" ) );
		Assert.False( model.Can( "ban" ) );
	}
}
