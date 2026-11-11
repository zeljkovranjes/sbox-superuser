using System.Text.Json;
using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>The vote in progress or just finished, as this player sees it; null when there has been none.</summary>
	public static VoteSnapshot CurrentVote => Game.ActiveScene?.GetSystem<Systems.VoteClientSystem>()?.Vote;

	/// <summary>Sends this player's ballot for the current vote; the host ignores it for anyone who can't vote in it.</summary>
	public static void CastVote( int option )
	{
		if ( CurrentVote is not { Finished: false } vote ) return;
		SubmitBallot( vote.Id, option );
	}

	[Rpc.Host]
	private static void SubmitBallot( Guid voteId, int option )
	{
		if ( Rpc.Caller is not { IsActive: true } caller ) return;
		FindAuthorityRuntime()?.Votes?.Cast( voteId, caller.SteamId.ToString(), option );
	}

	internal static void PublishVote( VoteSnapshot vote ) => ReceiveVote( JsonSerializer.Serialize( vote ) );

	[Rpc.Broadcast( NetFlags.HostOnly )]
	private static void ReceiveVote( string json )
	{
		if ( Rpc.Caller is not { IsHost: true } || json is null || json.Length > 64000 ) return;
		VoteSnapshot vote;
		try { vote = JsonSerializer.Deserialize<VoteSnapshot>( json ); }
		catch ( JsonException ) { return; }
		if ( vote is not null ) Game.ActiveScene?.GetSystem<Systems.VoteClientSystem>()?.Receive( vote );
	}
}
