using Sandbox;

namespace SboxSuperuser.Systems;

/// <summary>This machine's copy of the current vote, as the host last published it.</summary>
internal sealed class VoteClientSystem : GameObjectSystem<VoteClientSystem>
{
	public VoteClientSystem( Scene scene ) : base( scene ) { }

	internal VoteSnapshot Vote { get; private set; }

	/// <summary>When this machine first saw the current vote, for the panel's slide-in.</summary>
	internal RealTimeSince SinceStarted { get; private set; }

	/// <summary>When the current vote finished, for the panel's slide-out.</summary>
	internal RealTimeSince SinceFinished { get; private set; }

	internal void Receive( VoteSnapshot vote )
	{
		if ( Vote?.Id != vote.Id ) SinceStarted = 0;
		if ( vote.Finished && Vote?.Finished != true ) SinceFinished = 0;
		Vote = vote;
		Scene.RunEvent<IVoteEvent>( listener => listener.OnVoteChanged( vote ) );
	}
}
