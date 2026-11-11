using SboxSuperuser.Engine.Players;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PlayerCommandTests
{
	// Records each call as "Method target value" and returns whatever Reply says.
	private sealed class RecordingActions : IPlayerActions
	{
		internal readonly List<string> Calls = new();
		internal CommandResult Reply = new( CommandStatus.Success, "Done." );

		private CommandResult Record( string call ) { Calls.Add( call ); return Reply; }
		public CommandResult Kill( string steamId, bool silent ) => Record( $"Kill {steamId} {silent}" );
		public CommandResult Slap( string steamId, float damage ) => Record( $"Slap {steamId} {damage}" );
		public CommandResult SetFrozen( string steamId, bool frozen ) => Record( $"Frozen {steamId} {frozen}" );
		public CommandResult SetBlind( string steamId, bool blind ) => Record( $"Blind {steamId} {blind}" );
		public CommandResult SetIgnited( string steamId, bool ignited ) => Record( $"Ignited {steamId} {ignited}" );
		public CommandResult SetCloaked( string steamId, bool cloaked ) => Record( $"Cloaked {steamId} {cloaked}" );
		public CommandResult SetGod( string steamId, bool god ) => Record( $"God {steamId} {god}" );
		public CommandResult SetHealth( string steamId, int health ) => Record( $"Health {steamId} {health}" );
		public CommandResult SetArmor( string steamId, int armor ) => Record( $"Armor {steamId} {armor}" );
		public CommandResult Strip( string steamId ) => Record( $"Strip {steamId}" );
		public CommandResult SetRagdoll( string steamId, bool ragdoll ) => Record( $"Ragdoll {steamId} {ragdoll}" );
		public CommandResult SetNoclip( string steamId, bool noclip ) => Record( $"Noclip {steamId} {noclip}" );
		public CommandResult Spectate( string spectatorSteamId, string targetSteamId ) => Record( $"Spectate {spectatorSteamId} {targetSteamId}" );
		public CommandResult MoveTo( string steamId, string destinationSteamId ) => Record( $"MoveTo {steamId} {destinationSteamId}" );
		public CommandResult MoveToAim( string steamId, string aimingSteamId ) => Record( $"MoveToAim {steamId} {aimingSteamId}" );
		public CommandResult Return( string steamId ) => Record( $"Return {steamId}" );
		public CommandResult SetJailed( string steamId, bool jailed ) => Record( $"Jailed {steamId} {jailed}" );
		public CommandResult PlaySound( string sound ) => Record( $"Sound {sound}" );
		public CommandResult ChangeMap( string map ) => Record( $"Map {map}" );
	}

	private sealed class Fixture : IDisposable
	{
		internal readonly DispatcherRig Rig = new();
		internal readonly RecordingActions Actions = new();
		internal readonly List<string> Announcements = new();
		internal IPlayerActions Configured;
		internal int Waits;
		internal readonly PlayerCommands Commands;

		internal Fixture()
		{
			Configured = Actions;
			Rig.Players.Add( new CommandPlayer( DispatcherRig.OwnerId, "Owner" ) );
			Rig.Players.Add( new CommandPlayer( DispatcherRig.UserId, "User" ) );
			Commands = new PlayerCommands( Rig.Registry, () => Configured, () => Rig.Players.Select( player => player.SteamId ).ToArray(),
				Rig.Dispatcher.CanTarget, id => id, Announcements.Add, _ => { Waits++; return Task.CompletedTask; } );
		}

		internal Task<CommandResult> Run( string command, params string[] arguments ) => Rig.Submit( command, arguments: arguments );

		public void Dispose()
		{
			Commands.Dispose();
			Rig.Dispose();
		}
	}

	[Fact]
	public async Task SlayAnnouncesButSilentSlayDoesNot()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "slay", DispatcherRig.TargetId )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "sslay", DispatcherRig.TargetId )).Status );
		Assert.Equal( new[] { $"Kill {DispatcherRig.TargetId} False", $"Kill {DispatcherRig.TargetId} True" }, fixture.Actions.Calls );
		Assert.Equal( $"{DispatcherRig.OwnerId} slayed {DispatcherRig.TargetId}.", Assert.Single( fixture.Announcements ) );
	}

	[Fact]
	public async Task PlayersCannotPunishStaffOrUseStaffCommands()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "slay", DispatcherRig.User, new[] { DispatcherRig.TargetId } )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "freeze", DispatcherRig.User, new[] { DispatcherRig.OwnerId } )).Status );
		Assert.Empty( fixture.Actions.Calls );
	}

	[Fact]
	public async Task GodAndHealthWorkOnYourselfButStillProtectOthers()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "god", DispatcherRig.OwnerId )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "hp", DispatcherRig.OwnerId, "500" )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "cloak", DispatcherRig.TargetId )).Status );
		Assert.Equal( CommandStatus.Denied, (await fixture.Rig.Submit( "hp", DispatcherRig.User, new[] { DispatcherRig.OwnerId, "1" } )).Status );
		Assert.Equal( new[] { $"God {DispatcherRig.OwnerId} True", $"Health {DispatcherRig.OwnerId} 500", $"Cloaked {DispatcherRig.TargetId} True" }, fixture.Actions.Calls );
	}

	[Fact]
	public async Task WhipSlapsTheRequestedTimesWithPauses()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "whip", DispatcherRig.TargetId, "4", "5" )).Status );
		Assert.Equal( 4, fixture.Actions.Calls.Count( call => call == $"Slap {DispatcherRig.TargetId} 5" ) );
		Assert.Equal( 3, fixture.Waits );
		Assert.Equal( CommandStatus.InvalidArguments, (await fixture.Run( "whip", DispatcherRig.TargetId, "51" )).Status );
	}

	[Fact]
	public async Task MaulBeatsThenKills()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "maul", DispatcherRig.TargetId )).Status );
		Assert.Equal( $"Kill {DispatcherRig.TargetId} False", fixture.Actions.Calls[ ^1 ] );
		Assert.True( fixture.Actions.Calls.Count( call => call.StartsWith( "Slap" ) ) > 1 );
	}

	[Fact]
	public async Task TogglesTurnOffWithTheirPair()
	{
		using var fixture = new Fixture();
		foreach ( var (on, off, call) in new[] { ("freeze", "unfreeze", "Frozen"), ("jail", "unjail", "Jailed"), ("god", "ungod", "God"),
			("cloak", "uncloak", "Cloaked"), ("blind", "unblind", "Blind"), ("ignite", "unignite", "Ignited"), ("ragdoll", "unragdoll", "Ragdoll") } )
		{
			Assert.Equal( CommandStatus.Success, (await fixture.Run( on, DispatcherRig.TargetId )).Status );
			Assert.Equal( CommandStatus.Success, (await fixture.Run( off, DispatcherRig.TargetId )).Status );
			Assert.Equal( new[] { $"{call} {DispatcherRig.TargetId} True", $"{call} {DispatcherRig.TargetId} False" }, fixture.Actions.Calls.TakeLast( 2 ) );
		}
	}

	[Fact]
	public async Task MovementUsesTheCallerAsReference()
	{
		using var fixture = new Fixture();
		await fixture.Run( "bring", DispatcherRig.TargetId );
		await fixture.Run( "goto", DispatcherRig.TargetId );
		await fixture.Run( "send", DispatcherRig.TargetId, DispatcherRig.UserId );
		await fixture.Run( "teleport", DispatcherRig.TargetId );
		await fixture.Run( "return", DispatcherRig.TargetId );
		await fixture.Run( "jailtp", DispatcherRig.TargetId );
		Assert.Equal( new[]
		{
			$"MoveTo {DispatcherRig.TargetId} {DispatcherRig.OwnerId}", $"MoveTo {DispatcherRig.OwnerId} {DispatcherRig.TargetId}",
			$"MoveTo {DispatcherRig.TargetId} {DispatcherRig.UserId}", $"MoveToAim {DispatcherRig.TargetId} {DispatcherRig.OwnerId}",
			$"Return {DispatcherRig.TargetId}", $"MoveToAim {DispatcherRig.TargetId} {DispatcherRig.OwnerId}", $"Jailed {DispatcherRig.TargetId} True"
		}, fixture.Actions.Calls );
	}

	[Fact]
	public async Task NoclipTogglesTheCallerAndHealthTakesAnAmount()
	{
		using var fixture = new Fixture();
		await fixture.Run( "noclip" );
		await fixture.Run( "noclip" );
		await fixture.Run( "hp", DispatcherRig.TargetId, "250" );
		await fixture.Run( "armor", DispatcherRig.TargetId, "50" );
		Assert.Equal( new[] { $"Noclip {DispatcherRig.OwnerId} True", $"Noclip {DispatcherRig.OwnerId} False",
			$"Health {DispatcherRig.TargetId} 250", $"Armor {DispatcherRig.TargetId} 50" }, fixture.Actions.Calls );
	}

	[Fact]
	public async Task UnsupportedAndMissingAdaptersAreReported()
	{
		using var fixture = new Fixture();
		fixture.Actions.Reply = new CommandResult( CommandStatus.Unsupported, "This game has no health." );
		Assert.Equal( CommandStatus.Unsupported, (await fixture.Run( "hp", DispatcherRig.TargetId, "10" )).Status );
		fixture.Configured = null;
		Assert.Equal( CommandStatus.Unsupported, (await fixture.Run( "slay", DispatcherRig.TargetId )).Status );
		Assert.Empty( fixture.Announcements );
	}

	[Fact]
	public async Task WorldCommandsRejectUnsafeNames()
	{
		using var fixture = new Fixture();
		Assert.Equal( CommandStatus.Success, (await fixture.Run( "map", "facepunch.flatgrass" )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await fixture.Run( "playsound", "../secret.sound" )).Status );
		Assert.Equal( "Map facepunch.flatgrass", Assert.Single( fixture.Actions.Calls ) );
	}

	[Fact]
	public void DefaultPolicyGrantsEveryPlayerCommandToStaff()
	{
		var granted = PolicySnapshot.CreateDefault().Groups.SelectMany( group => group.Permissions ).Select( entry => entry.Key ).ToHashSet();
		using var fixture = new Fixture();
		var commands = fixture.Rig.Registry.Descriptors.Where( descriptor => descriptor.ModuleId == PlayerCommands.Module ).ToArray();
		Assert.True( commands.Length >= 30 );
		Assert.All( commands, descriptor => Assert.Contains( descriptor.Permission, granted ) );
	}
}
