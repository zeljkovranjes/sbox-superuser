namespace SboxSuperuser;

/// <summary>Formats only public moderation actions; private notes have no announcement.</summary>
public static class ModerationAnnouncement
{
	/// <summary>Formats a public action, or returns null for private and unknown commands.</summary>
	public static string Format(string action,string actor,string target,string reason,CommandDuration duration=null)
	{
		var verb = action switch { "ban"=>"banned", "kick"=>"kicked", "warn"=>"warned", "mute"=>"muted", "unban"=>"unbanned", "unmute"=>"unmuted", "gag"=>"gagged", "ungag"=>"ungagged", "voicemute"=>"voice-muted", "voiceunmute"=>"voice-unmuted", "physgunrestrict"=>"restricted", "physgunrestore"=>"restored", _=>null };
		if(verb is null) return null;
		var timing = duration is null ? "" : duration.IsPermanent ? " (permanent)" : " (" + Duration(duration.Value.Value) + ")";
		var subject = Name(target) + (action is "physgunrestrict" or "physgunrestore" ? "'s physgun access" : "");
		return $"{subject} has been {verb} by {Name(actor)} for {reason?.Trim().TrimEnd('.', '!', '?')}{timing}.";
	}
	static string Name(string value) => new string((value??"Server").Take(128).Select(c=>char.IsControl(c)?' ':c).ToArray());
	static string Duration(TimeSpan time)
	{
		var parts = new List<string>();
		if(time.Days>0) parts.Add(time.Days+"d");
		if(time.Hours>0) parts.Add(time.Hours+"h");
		if(time.Minutes>0) parts.Add(time.Minutes+"m");
		if(time.Seconds>0 || parts.Count==0) parts.Add(Math.Max(1,time.Seconds)+"s");
		return string.Join(" ",parts);
	}
}
