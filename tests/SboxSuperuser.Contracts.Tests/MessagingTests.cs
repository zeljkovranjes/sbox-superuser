using Xunit;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Contracts.Tests;

public sealed class MessagingTests
{
	[Fact]
	public async Task PrivateMessageReachesOnlyTheTarget()
	{
		using var rig = new ModerationRig();
		var result = await rig.Rig.Submit( "psay", DispatcherRig.User, new[] { DispatcherRig.TargetId, "hello", "there" } );
		Assert.Equal( CommandStatus.Success, result.Status );
		var sent = Assert.Single( rig.Messages.Private );
		Assert.Equal( DispatcherRig.TargetId, sent.SteamId );
		Assert.EndsWith( ": hello there", sent.Text );
	}

	[Fact]
	public async Task MutedPlayersCannotWhisperOrWriteToStaff()
	{
		using var rig = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await rig.Run( "mute", DispatcherRig.UserId, "30m", "Spam" )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Rig.Submit( "psay", DispatcherRig.User, new[] { DispatcherRig.TargetId, "hi" } )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Rig.Submit( "asay", DispatcherRig.User, new[] { "hi" } )).Status );
		Assert.Empty( rig.Messages.Private );
	}

	[Fact]
	public async Task StaffMessageGoesToReadersAndTheSenderOnly()
	{
		using var rig = new ModerationRig();
		var result = await rig.Rig.Submit( "asay", DispatcherRig.User, new[] { "someone", "is", "cheating" } );
		Assert.Equal( CommandStatus.Success, result.Status );
		var recipients = rig.Messages.Private.Select( sent => sent.SteamId ).ToArray();
		Assert.Contains( DispatcherRig.OwnerId, recipients );
		Assert.Contains( DispatcherRig.UserId, recipients );
		Assert.DoesNotContain( DispatcherRig.TargetId, recipients );
		Assert.All( rig.Messages.Private, sent => Assert.StartsWith( "[STAFF] ", sent.Text ) );
	}

	[Fact]
	public async Task BroadcastsNeedTheirPermissionAndRejectBadText()
	{
		using var rig = new ModerationRig();
		Assert.Equal( CommandStatus.Denied, (await rig.Rig.Submit( "csay", DispatcherRig.User, new[] { "hi" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "csay", "Restart", "in", "5" )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "tsay", "Welcome" )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Run( "tsay", new string( 'x', 257 ) )).Status );
		Assert.Equal( "Restart in 5", Assert.Single( rig.Messages.Center ) );
		Assert.Equal( "Welcome", Assert.Single( rig.Messages.Chat ) );
	}

	[Fact]
	public async Task TheTimeShowsLocalAndUtc()
	{
		using var rig = new ModerationRig();
		var result = await rig.Rig.Submit( "thetime", DispatcherRig.User );
		Assert.Equal( CommandStatus.Success, result.Status );
		var fixedTime = AdministrationCommands.TheTime( new DateTimeOffset( 2026, 11, 11, 9, 30, 0, TimeSpan.FromHours( -5 ) ) );
		Assert.Equal( "Server time: 2026-11-11 09:30:00 (UTC-05:00); UTC 2026-11-11 14:30:00.", fixedTime.Message );
	}
}
