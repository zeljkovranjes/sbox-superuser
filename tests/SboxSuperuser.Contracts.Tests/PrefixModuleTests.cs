using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;
using SboxSuperuser.Engine.Prefixes;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PrefixModuleTests
{
	private static async Task<(ModuleFixture Fixture, PrefixModule Module)> Start()
	{
		var fixture = new ModuleFixture();
		var ranks = new Dictionary<string, GroupRank[]>
		{
			[ DispatcherRig.TargetId ] = new[] { new GroupRank( "admin", 50 ), new GroupRank( "player", 0 ) }
		};
		var module = new PrefixModule( new PrefixHost( fixture.Rig.Store, () => new[] { DispatcherRig.TargetId },
			steamId => ranks.GetValueOrDefault( steamId ) ?? Array.Empty<GroupRank>(),
			() => new[] { new GroupRank( "owner", 100 ), new GroupRank( "admin", 50 ), new GroupRank( "player", 0 ) },
			source => EffectCompiler.Compile( source ).Diagnostics.FirstOrDefault()?.Describe( source ),
			key => fixture.Rig.Policy.RegisteredPermissions.Contains( key ), true, "# For game code" ) );
		fixture.Manager.Install( new IAdminModule[] { module } );
		var started = await fixture.Manager.StartAllAsync( CancellationToken.None );
		Assert.True( started.Status == CommandStatus.Success, string.Join( "; ", fixture.Manager.Statuses.Select( status => status.Message )
			.Append( started.Message ).Concat( fixture.Rig.Errors.Select( error => error.ToString() ) ) ) );
		return (fixture, module);
	}

	[Fact]
	public async Task ANewServerStartsWithTheStarterPrefixes()
	{
		var (fixture, module) = await Start();
		using var _ = fixture;

		Assert.Equal( "admin", module.Snapshot().Players[ DispatcherRig.TargetId ] );
		Assert.True( module.Snapshot().Chat );
	}

	[Fact]
	public async Task PrefixesAreCreatedStyledAndAssignedThroughAuditedCommands()
	{
		var (fixture, module) = await Start();
		using var _ = fixture;

		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "prefix.create", arguments: new[] { "vip", "[VIP]" } )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "prefix.update", arguments: new[] { "vip", "tag-effect", "preset:rain" } )).Status );
		var script = "// two lines\ncolor = palette[i]";
		var update = new[] { "vip", "name-effect" }.Concat( PrefixStyleText.Arguments( new PrefixStyle( PrefixStyleKind.Script, script ) ) ).ToArray();
		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "prefix.update", arguments: update )).Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "prefix.user.set", arguments: new[] { DispatcherRig.TargetId, "vip" } )).Status );

		var vip = module.Catalog.Find( "vip" );
		Assert.Equal( "rain", vip.TagStyle.Value );
		Assert.Equal( script, vip.NameStyle.Value );
		Assert.Equal( "vip", module.Snapshot().Players[ DispatcherRig.TargetId ] );
		Assert.Contains( fixture.Rig.Store.Documents.Keys, key => key.Contains( PrefixModule.Id ) );
	}

	[Fact]
	public async Task BrokenScriptsAndUnknownGroupsAreRefusedWithAReason()
	{
		var (fixture, _) = await Start();
		using var __ = fixture;

		var broken = await fixture.Rig.Submit( "prefix.update", arguments: new[] { "admin", "tag-effect", "color = rde" } );
		Assert.Equal( CommandStatus.InvalidArguments, broken.Status );
		Assert.Contains( "did you mean red?", broken.Message );

		var group = await fixture.Rig.Submit( "prefix.group.set", arguments: new[] { "nobody", "admin" } );
		Assert.Equal( CommandStatus.NotFound, group.Status );
	}

	[Fact]
	public async Task TheExportListsEveryGroupForTheMenu()
	{
		var (fixture, _) = await Start();
		using var __ = fixture;

		var export = await fixture.Rig.Submit( "prefix.export" );

		Assert.Equal( "owner:100,admin:50,player:0", export.Data[ "groups" ] );
		var id = Guid.NewGuid();
		var model = new Engine.Admin.PrefixEditorModel( _ => id );
		model.Request();
		Assert.True( model.Receive( Engine.Admin.HostAdminQueries.FromResult( id, export ) ) );
		Assert.Equal( new[] { "owner", "admin", "player" }, model.Groups.Select( group => group.GroupId ) );
		Assert.NotNull( model.Catalog.Find( "admin" ) );
	}

	[Fact]
	public async Task ChangesNeedThePermission()
	{
		var (fixture, _) = await Start();
		using var __ = fixture;

		var denied = await fixture.Rig.Submit( "prefix.create", DispatcherRig.User, new[] { "vip" } );
		Assert.Equal( CommandStatus.Denied, denied.Status );
		Assert.Equal( CommandStatus.Success, (await fixture.Rig.Submit( "prefix.list", DispatcherRig.User )).Status );
	}

	[Fact]
	public async Task DisablingTheModuleSendsPlainNames()
	{
		var (fixture, module) = await Start();
		using var _ = fixture;

		Assert.Equal( CommandStatus.Success, (await fixture.Disable( PrefixModule.Id )).Status );

		Assert.False( module.Snapshot().Enabled );
		Assert.Null( module.Snapshot().For( DispatcherRig.TargetId ) );
	}
}
