using Sandbox;

namespace SboxSuperuser;

/// <summary>Raised on every player's machine whenever the current vote changes, for games drawing their own vote panel.</summary>
public interface IVoteEvent : ISceneEvent<IVoteEvent>
{
	/// <summary>The vote as it is now; Finished is true once it has been decided.</summary>
	void OnVoteChanged( VoteSnapshot vote );
}
