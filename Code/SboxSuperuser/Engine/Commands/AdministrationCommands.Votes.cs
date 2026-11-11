using SboxSuperuser.Core.Moderation;
using SboxSuperuser.Core.Votes;
using SboxSuperuser.Engine.Votes;

namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	internal const string ForceMapVotePermission = "admin.votes.mapforce";
	private const string ForceWord = "force";
	private static readonly string[] YesNo = { "Yes", "No" };

	private void RegisterVotes( VoteSession votes, Func<string, CommandResult> changeMap )
	{
		Register( "vote", "Ask everyone a question; options are separated by spaces, Yes and No when there are none", "admin.votes.vote",
			new[] { Text( "question", "The question, quoted when it has spaces" ), new CommandArgumentDescriptor( "options", "Up to 9 one-word options",
				CommandArgumentKind.String, isOptional: true, isRemainder: true ) },
			async (context, arguments) =>
			{
				var options = arguments.Contains( "options" ) ? arguments.Get<string>( "options" ).Split( ' ', StringSplitOptions.RemoveEmptyEntries ) : YesNo;
				var outcome = await votes.RunAsync( arguments.Get<string>( "question" ), options, VoteRule.MajorityOfServer );
				return Decided( outcome, null );
			}, mutation: true );
		Register( "votekick", "Ask everyone whether to kick a player; it passes with more than half of the server", "admin.votes.kick",
			new[] { Punishable(), Reason() }, (context, arguments) => VoteToModerateAsync( votes, context, "votekick", "admin.votes.kick",
				ModerationKind.Kick, arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "reason" ), null ), mutation: true );
		Register( "voteban", "Ask everyone whether to ban a player; it passes with more than half of the server", "admin.votes.ban",
			new[] { Punishable(), new CommandArgumentDescriptor( "duration", "Explicit positive duration such as 30m, 2h, 7d, or permanent",
				CommandArgumentKind.Duration, audit: CommandArgumentAudit.SafeValue ), Reason() },
			(context, arguments) => VoteToModerateAsync( votes, context, "voteban", "admin.votes.ban", ModerationKind.Ban,
				arguments.Get<PlayerReference>( "user" ).SteamId, arguments.Get<string>( "reason" ), arguments.Get<CommandDuration>( "duration" ) ), mutation: true );
		Register( "votemap", "Ask everyone to change the map; with several maps they choose. 'votemap force <maps>' lets the most votes win",
			"admin.votes.map", new[] { new CommandArgumentDescriptor( "maps", "One or more map idents, or force followed by maps",
				CommandArgumentKind.String, isRemainder: true ) },
			(context, arguments) => VoteMapAsync( votes, changeMap, context, arguments.Get<string>( "maps" ) ), mutation: true );
		Register( "stopvote", "End the current vote now and decide it on the ballots so far", "admin.votes.stop",
			Array.Empty<CommandArgumentDescriptor>(), (_, _) => Task.FromResult( votes.Stop()
				? new CommandResult( CommandStatus.Success, "Vote stopped." ) : NoVote() ), mutation: true );
		Register( "veto", "Cancel the current vote; nothing it asked for happens", "admin.votes.veto",
			Array.Empty<CommandArgumentDescriptor>(), (_, _) => Task.FromResult( votes.Veto()
				? new CommandResult( CommandStatus.Success, "Vote vetoed." ) : NoVote() ), mutation: true );
	}

	/// <summary>Kicks or bans through the same path as the commands, with the vote's own permission, once Yes wins.</summary>
	private async Task<CommandResult> VoteToModerateAsync( VoteSession votes, CommandContext context, string command, string permission,
		ModerationKind kind, string target, string reason, CommandDuration duration )
	{
		var verb = kind == ModerationKind.Ban ? "Ban" : "Kick";
		var outcome = await votes.RunAsync( $"{verb} {_playerName( target )}{(duration is null ? "" : " " + Core.Messages.MessageCatalog.Phrase( duration.Value ))}? {reason}", YesNo, VoteRule.MajorityOfServer );
		if ( outcome?.Winner != 0 ) return Decided( outcome, null );
		return await ModerateAsync( context, command, permission, kind, target, $"Vote: {reason}", duration );
	}

	private async Task<CommandResult> VoteMapAsync( VoteSession votes, Func<string, CommandResult> changeMap, CommandContext context, string text )
	{
		var maps = text.Split( ' ', StringSplitOptions.RemoveEmptyEntries ).ToList();
		var forced = maps.Count > 0 && string.Equals( maps[ 0 ], ForceWord, StringComparison.OrdinalIgnoreCase );
		if ( forced )
		{
			if ( !_dispatcher.CanUse( context, ForceMapVotePermission ) ) return new CommandResult( CommandStatus.Denied, $"Forcing a map vote needs {ForceMapVotePermission}." );
			maps.RemoveAt( 0 );
		}
		if ( maps.Count is 0 or > VoteTally.MaximumOptions ) return new CommandResult( CommandStatus.InvalidArguments, $"Give 1 to {VoteTally.MaximumOptions} maps." );
		if ( _mapAllowed is not null && maps.FirstOrDefault( map => !_mapAllowed( map ) ) is { } banned )
			return new CommandResult( CommandStatus.Denied, $"{banned} is banned on this server." );
		// One map is a yes or no question; several maps are the choices themselves.
		var single = maps.Count == 1;
		var outcome = await votes.RunAsync( single ? $"Change the map to {maps[ 0 ]}?" : "Which map next?", single ? YesNo : maps,
			forced ? VoteRule.MostVotes : VoteRule.MajorityOfServer );
		if ( outcome?.Winner is not { } winner || single && winner != 0 ) return Decided( outcome, null );
		var map = single ? maps[ 0 ] : maps[ winner ];
		var changed = changeMap?.Invoke( map ) ?? new CommandResult( CommandStatus.Unsupported, "This game can't change maps." );
		return changed.Status == CommandStatus.Success ? new CommandResult( CommandStatus.Success, $"{outcome.Reason} Changing to {map}." ) : changed;
	}

	private static CommandResult Decided( VoteOutcome outcome, string action ) => outcome is null
		? new CommandResult( CommandStatus.Conflict, "Another vote is already running; wait for it to finish." )
		: new CommandResult( CommandStatus.Success, outcome.Reason + (action is null ? "" : " " + action) );

	private static CommandResult NoVote() => new( CommandStatus.NoChange, "No vote is running." );

	private static CommandArgumentDescriptor Punishable() => new( "user", "Exact Steam ID or unique online name", CommandArgumentKind.Player,
		targetPolicy: CommandTargetPolicy.PunitiveOnlinePlayer, audit: CommandArgumentAudit.SafeValue );

	private static CommandArgumentDescriptor Reason() => new( "reason", "Printable reason, at most 256 characters", CommandArgumentKind.String, isRemainder: true );
}
