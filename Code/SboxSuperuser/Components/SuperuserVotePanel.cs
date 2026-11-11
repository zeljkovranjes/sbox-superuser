using Sandbox;
using SboxSuperuser.Systems;

namespace SboxSuperuser.Components;

/// <summary>
/// Superuser's vote panel: slides in from the left while a vote runs, takes F1, F2 and so on (or a click) as
/// ballots, and lights up each voter as they choose. Leave it out of the scene to draw your own from
/// Superuser.CurrentVote and IVoteEvent, sending ballots with Superuser.CastVote.
/// </summary>
[Title( "Superuser Vote Panel" ), Category( "Superuser" ), Icon( "how_to_vote" )]
public sealed class SuperuserVotePanel : Component
{
	private readonly UI.VotePanelRenderer _renderer = new();

	protected override void OnUpdate()
	{
		if ( Scene.GetSystem<VoteClientSystem>() is not { Vote: { } vote } votes || Scene.Camera is not { } camera ) return;
		if ( !vote.Finished && !ChatOpen() )
			for ( var option = 0; option < vote.Options.Length; option++ )
				if ( Input.Keyboard.Pressed( UI.VotePanelRenderer.Keys[ option ] ) ) Superuser.CastVote( option );
		var clicked = _renderer.Draw( camera, vote, votes.SinceStarted, votes.SinceFinished );
		if ( clicked is { } choice && !vote.Finished ) Superuser.CastVote( choice );
	}

	// Typing in chat shouldn't vote; F keys still work while the chat box is closed.
	private bool ChatOpen() => Scene.GetSystem<ChatBoxSystem>()?.IsOpen == true;
}
