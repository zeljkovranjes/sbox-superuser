namespace SboxSuperuser;

/// <summary>Target-protection result only; command permission and live target availability still require independent checks.</summary>
public sealed class TargetProtectionResult
{
	/// <summary>Gets whether protection allows this actor to punish this target.</summary>
	public bool IsAllowed { get; }

	/// <summary>Gets the actor's effective inherited protection level.</summary>
	public int ActorLevel { get; }

	/// <summary>Gets the target's effective protection level, including stored offline roles.</summary>
	public int TargetLevel { get; }

	/// <summary>Gets the decision's caller-safe reason.</summary>
	public string Reason { get; }

	internal TargetProtectionResult( bool isAllowed, int actorLevel, int targetLevel, string reason )
	{
		IsAllowed = isAllowed;
		ActorLevel = actorLevel;
		TargetLevel = targetLevel;
		Reason = reason;
	}
}
