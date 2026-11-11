using Xunit;
namespace SboxSuperuser.Contracts.Tests;

public class ModerationAnnouncementTests
{
	[Theory]
	[InlineData("ban","banned")][InlineData("kick","kicked")][InlineData("warn","warned")]
	[InlineData("mute","muted")][InlineData("unban","unbanned")][InlineData("unmute","unmuted")]
	[InlineData("gag","gagged")][InlineData("ungag","ungagged")]
	[InlineData("voicemute","voice-muted")][InlineData("voiceunmute","voice-unmuted")]
	public void PublicActionNamesAreExplicit(string action,string verb)=>Assert.Equal($"Target has been {verb} by Owner for Spam.",ModerationAnnouncement.Format(action,"Owner","Target","Spam"));
	[Fact] public void DurationsAndPrivateNotes()
	{
		Assert.Null(ModerationAnnouncement.Format("note","Owner","Target","private"));
		Assert.Contains("(1d 2h 3m)",ModerationAnnouncement.Format("ban","Owner","Target","Spam",new CommandDuration(new TimeSpan(1,2,3,0))));
		Assert.Contains("(permanent)",ModerationAnnouncement.Format("ban","Owner","Target","Spam",CommandDuration.Permanent));
	}
	[Fact] public void ReasonAndDurationRemainOneSentence()
	{
		Assert.Equal("Player has been banned by chomnr for harassment (3d).",ModerationAnnouncement.Format("ban","chomnr","Player","harassment.",new CommandDuration(TimeSpan.FromDays(3))));
		Assert.Equal("Player's physgun access has been restricted by chomnr for trolling (1h).",ModerationAnnouncement.Format("physgunrestrict","chomnr","Player","trolling",new CommandDuration(TimeSpan.FromHours(1))));
	}

	[Fact] public async Task OnlyConfirmedActionsAnnounceAndDuplicatesDoNotRepeat()
	{
		using var rig=new ModerationRig();
		var id=Guid.NewGuid();var args=new[]{DispatcherRig.TargetId,"30m","Spam"};
		Assert.Equal(CommandStatus.Success,(await rig.Rig.Submit("mute",arguments:args,id:id)).Status);
		await rig.Rig.Submit("mute",arguments:args,id:id);
		Assert.Single(rig.Announcements);
		Assert.Equal("Owner muted Target for 30 minutes for Spam.",rig.Announcements[0]);
		await rig.Run("note",DispatcherRig.TargetId,"Private note");
		await rig.Rig.Submit("ban",DispatcherRig.User, new[]{DispatcherRig.TargetId,"30m","Forbidden"});
		Assert.Single(rig.Announcements);
	}
}
