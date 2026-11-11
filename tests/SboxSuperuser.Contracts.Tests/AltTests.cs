using SboxSuperuser.Engine.Admin;
using SboxSuperuser.Engine.Alts;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AltTests
{
	private const string Main = "76561198000000011", Alt = "76561198000000012", Banned = "76561198000000013";

	[Fact]
	public async Task LinksBanEvadersOnTheDashboardShowsIpHistoryToOwnersAndSurvivesReload()
	{
		using var rig = new ModerationRig();
		var alts = new HostAltService( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add, _ => Task.CompletedTask );
		await alts.Initialization;
		alts.Record( Main, "Bob", Main, "203.0.113.7", "" );
		alts.Record( Alt, "B0b", Main, "198.51.100.2", "" );
		alts.Record( Banned, "Evil", Banned, "203.0.113.7", "" );
		await Task.Delay( 50 );
		Assert.Equal( CommandStatus.Success, (await rig.Run( "ban", Banned, "permanent", "Cheating" )).Status );

		var queries = new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, alts: alts );
		Task<AdminReply> Query( AdminQuery query, ActorIdentity actor ) => queries.QueryAsync( () => actor, query, Guid.NewGuid(), CancellationToken.None );

		var linked = await Query( new AdminQuery( AdminQueryKind.Alts, Main ), DispatcherRig.Owner );
		Assert.Equal( new[] { Alt, Banned }, linked.Alts.Select( row => row.SteamId ) );
		Assert.Equal( "Family share", linked.Alts[ 0 ].Evidence[ 0 ] );
		Assert.True( linked.Alts[ 1 ].Banned );

		var dashboard = await Query( new AdminQuery( AdminQueryKind.AltSuspects ), DispatcherRig.Owner );
		Assert.Equal( Main, dashboard.Alts[ 0 ].SteamId );
		Assert.True( dashboard.Alts[ 0 ].LinkedToBanned );
		Assert.Equal( CommandStatus.Denied, (await Query( new AdminQuery( AdminQueryKind.AltSuspects ), DispatcherRig.User )).Status );

		Assert.Equal( "203.0.113.7", Assert.Single( linked.Addresses ).Address );
		Assert.Empty( (await Query( new AdminQuery( AdminQueryKind.Alts, Main ), DispatcherRig.User )).Addresses );

		var reloaded = new HostAltService( rig.Rig.Store, () => rig.Rig.Active, rig.Rig.Errors.Add, _ => Task.CompletedTask );
		await reloaded.Initialization;
		Assert.Equal( 2, reloaded.Ledger.AltsOf( Main ).Count );
		Assert.Equal( "203.0.113.7", reloaded.Ledger.Find( Main ).Addresses[ 0 ].Address );
		Assert.Empty( rig.Rig.Errors );
	}
}
