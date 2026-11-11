using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.Moderation;

namespace SboxSuperuser.Contracts.Tests;

// Orchestration fixture with a storage port and disconnect observer; it does not simulate Sandbox networking.
internal sealed class ModerationRig : IDisposable
{
	internal DispatcherRig Rig { get; } = new();
	internal HostModerationService Moderation { get; }
	internal AdministrationCommands Commands { get; }
	internal DateTimeOffset Now = new( 2026, 10, 18, 0, 0, 0, TimeSpan.Zero );
	internal readonly List<string> Announcements=new();
	internal RecordingMessenger Messages { get; } = new();
	internal int DisconnectRequests;
	internal bool RecordExistedBeforeDisconnect;

	internal ModerationRig()
	{
		Moderation = new( Rig.Store, () => Rig.Active, Rig.Dispatcher.CanUse,
			Rig.Dispatcher.CanTarget, Rig.Errors.Add, () => Now );
		Commands = new( Rig.Policy, null, Rig.Registry, Rig.Dispatcher, () => new CommandResult( CommandStatus.Success, "Test" ),
			moderation: Moderation, disconnect: (context, target, permission, ban) =>
			{
				DisconnectRequests++;
				RecordExistedBeforeDisconnect = Rig.Store.Documents.ContainsKey( MemoryPolicyStore.Id( HostModerationService.Key ) );
				return Task.FromResult( new CommandResult( CommandStatus.Success, "Disconnect observer invoked" ) );
			}, announce: Announcements.Add, playerName: id => id == DispatcherRig.OwnerId ? "Owner" : "Target", messenger: Messages );
	}

	internal Task<CommandResult> Run( string command, params string[] arguments ) => Rig.Submit( command, arguments: arguments );
	public void Dispose()
	{
		Commands.Dispose();
		Rig.Dispose();
	}
}

// Records what staff messages would have shown; Online lists who is connected.
internal sealed class RecordingMessenger : SboxSuperuser.Engine.Messaging.IPlayerMessenger
{
	internal readonly List<string> Connected = new() { DispatcherRig.OwnerId, DispatcherRig.UserId, DispatcherRig.TargetId };
	internal readonly List<(string SteamId, string Text)> Private = new();
	internal readonly List<string> Chat = new();
	internal readonly List<string> Center = new();

	public IReadOnlyList<string> Online => Connected;

	public bool SendPrivate( string steamId, string text )
	{
		if ( !Connected.Contains( steamId ) ) return false;
		Private.Add( (steamId, text) );
		return true;
	}

	public void SendChat( string text ) => Chat.Add( text );

	public void SendCenter( string text ) => Center.Add( text );
}
