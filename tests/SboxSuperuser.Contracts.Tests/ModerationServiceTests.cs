using SboxSuperuser.Core.Moderation;
using SboxSuperuser.Engine.Moderation;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ModerationServiceTests
{
	[Fact]
	public async Task BanCommitsBeforeInvokingDisconnect()
	{
		using var fixture = new ModerationRig();
		var release = new TaskCompletionSource();
		fixture.Rig.Store.BeforeCommit = (_, _) => release.Task;
		var command = fixture.Run( "ban", DispatcherRig.TargetId, "2h", "Test ban" );
		Assert.False( command.IsCompleted );
		Assert.Equal( 0, fixture.DisconnectRequests );
		release.SetResult();
		Assert.Equal( CommandStatus.Success, (await command).Status );
		Assert.True( fixture.RecordExistedBeforeDisconnect );
		Assert.Equal( 1, fixture.DisconnectRequests );
		Assert.Equal( CommandStatus.Denied, fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.Ban ).Status );
	}

	[Fact]
	public async Task StoreFailureDoesNotBanOrDisconnect()
	{
		using var fixture = new ModerationRig();
		fixture.Rig.Store.WriteOverride = StoreWriteStatus.Unavailable;
		Assert.Equal( CommandStatus.StorageUnavailable, (await fixture.Run( "ban", DispatcherRig.TargetId, "permanent", "Test" )).Status );
		Assert.Equal( 0, fixture.DisconnectRequests );
		Assert.Equal( CommandStatus.Success, fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.Ban ).Status );
		Assert.Empty( fixture.Rig.Store.Documents );
	}

	[Fact]
	public async Task RepeatedRevocationIsNoChangeAndKeepsTheOriginalHistory()
	{
		using var fixture = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "mute", DispatcherRig.TargetId, "permanent", "Test" )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "unmute", DispatcherRig.TargetId )).Status );
		var writes = fixture.Rig.Store.WriteCount;
		Assert.Equal( CommandStatus.NoChange, (await fixture.Run( "unmute", DispatcherRig.TargetId )).Status );
		Assert.Equal( writes, fixture.Rig.Store.WriteCount );
		var document = fixture.Rig.Store.Documents[ MemoryPolicyStore.Id( HostModerationService.Key ) ];
		var ledger = ModerationCodec.Read( document.PayloadJson, document.Revision );
		Assert.Equal( 2, ledger.Entries.Count );
		Assert.Equal( ledger.Entries[ 0 ].Id, ledger.Entries[ 1 ].Revokes );
	}

	[Fact]
	public async Task GagRestrictsVoiceUntilUngaggedWithoutMuting()
	{
		using var fixture = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "gag", DispatcherRig.TargetId, "permanent", "Mic spam" )).Status );
		Assert.True( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.Gag ) );
		Assert.False( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.TextMute ) );
		Assert.Equal( "Gag", fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.Gag ).Data["restriction"] );
		Assert.Equal( CommandStatus.Conflict, (await fixture.Run( "gag", DispatcherRig.TargetId, "30m", "Again" )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "ungag", DispatcherRig.TargetId )).Status );
		Assert.False( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.Gag ) );
	}

	[Fact]
	public async Task GimpIsItsOwnRestrictionAndLinesAreStable()
	{
		using var fixture = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "gimp", DispatcherRig.TargetId, "10m", "Spam" )).Status );
		Assert.True( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.Gimp ) );
		Assert.False( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.TextMute ) );
		Assert.Equal( "Owner gimped Target for 10 minutes for Spam.", fixture.Announcements[ ^1 ] );
		Assert.Equal( GimpLines.For( "hello" ), GimpLines.For( "hello" ) );
		Assert.Contains( GimpLines.For( "anything" ), GimpLines.Lines );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "ungimp", DispatcherRig.TargetId )).Status );
		Assert.False( fixture.Moderation.IsRestricted( DispatcherRig.TargetId, ModerationKind.Gimp ) );
	}

	[Fact]
	public async Task ExpiredMuteStopsEnforcementWithoutATimerOrDeletion()
	{
		using var fixture = new ModerationRig();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "mute", DispatcherRig.TargetId, "30m", "Test" )).Status );
		var restriction = fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.TextMute );
		Assert.Equal( CommandStatus.Denied, restriction.Status );
		Assert.Equal( "TextMute", restriction.Data["restriction"] );
		fixture.Now = fixture.Now.AddMinutes( 30 );
		Assert.Equal( CommandStatus.Success, fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.TextMute ).Status );
		Assert.Equal( CommandStatus.NoChange, (await fixture.Run( "unmute", DispatcherRig.TargetId )).Status );
		Assert.Single( fixture.Rig.Store.Documents );
	}

	[Fact]
	public async Task OfflineWarningPersistsButOfflineKickDoesNotRun()
	{
		using var fixture = new ModerationRig();
		const string offline = "76561198000000004";
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "warn", offline, "Offline warning" )).Status );
		var writes = fixture.Rig.Store.WriteCount;
		Assert.Equal( CommandStatus.InvalidArguments, (await fixture.Run( "kick", offline, "Offline kick" )).Status );
		Assert.Equal( writes, fixture.Rig.Store.WriteCount );
		Assert.Equal( 0, fixture.DisconnectRequests );
	}

	[Fact]
	public async Task EqualRankAndSelfPunishmentHaveNoPersistentEffect()
	{
		using var fixture = new ModerationRig();
		Assert.Equal( CommandStatus.Denied, (await fixture.Run( "ban", DispatcherRig.OwnerId, "permanent", "Self" )).Status );
		await fixture.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.UserId, "admin" ) );
		await fixture.Rig.Edit( PolicyChange.AddUserGroup( DispatcherRig.TargetId, "admin" ) );
		var writes = fixture.Rig.Store.WriteCount;
		var result = await fixture.Rig.Submit( "ban", DispatcherRig.User, new[] { DispatcherRig.TargetId, "permanent", "Equal" } );
		Assert.Equal( CommandStatus.Denied, result.Status );
		Assert.Equal( writes, fixture.Rig.Store.WriteCount );
		Assert.Equal( 0, fixture.DisconnectRequests );
	}

	[Fact]
	public async Task LostHistoryFailsClosedInsteadOfResettingRestrictions()
	{
		using var fixture = new ModerationRig();
		await fixture.Run( "ban", DispatcherRig.TargetId, "permanent", "Test" );
		fixture.Rig.Store.Documents.Remove( MemoryPolicyStore.Id( HostModerationService.Key ) );
		fixture.Moderation.OnStorageChanged( HostModerationService.Key );
		Assert.Equal( StoreReadStatus.Corrupt, fixture.Moderation.Status );
		Assert.Equal( CommandStatus.StorageUnavailable, fixture.Moderation.CheckRestriction( DispatcherRig.TargetId, ModerationKind.Ban ).Status );
	}

	[Fact]
	public async Task UnauthorizedCallerCannotReadModerationReasons()
	{
		using var fixture = new ModerationRig();
		await fixture.Run( "warn", DispatcherRig.TargetId, "Private note" );
		var result = await fixture.Rig.Submit( "moderation.history", DispatcherRig.User, new[] { DispatcherRig.TargetId } );
		Assert.Equal( CommandStatus.Denied, result.Status );
		Assert.Empty( result.Data );
	}
}
