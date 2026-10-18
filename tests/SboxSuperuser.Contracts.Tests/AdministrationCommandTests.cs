using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdministrationCommandTests
{
	[Fact]
	public async Task HelpFiltersSecurityCommandsAndDescribesConstraints()
	{
		using var rig = new AdministrationRig();
		var ordinary = await rig.Run( Array.Empty<string>(), DispatcherRig.User );
		Assert.Equal( CommandStatus.Success, ordinary.Status );
		Assert.DoesNotContain( "group.create", string.Join( "", ordinary.Data.Values ) );
		Assert.Equal( CommandStatus.NotFound, (await rig.Run( new[] { "help", "group.create" }, DispatcherRig.User )).Status );
		var owner = await rig.Run( new[] { "help", "group.protection.set" } );
		Assert.Contains( "minimum 0", string.Join( "", owner.Data.Values ) );
		Assert.Contains( "admin.security.manage", string.Join( "", owner.Data.Values ) );
		Assert.Contains( "Example:", string.Join( "", owner.Data.Values ) );
	}

	[Fact]
	public async Task AccessCommandsPersistAndProtectReservedOwnerMembership()
	{
		using var rig = new AdministrationRig();
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "create", "helpers" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "helpers", "parents", "set", "player" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "helpers", "permission", "allow", "admin.permissions.explain" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "user", DispatcherRig.UserId, "group", "add", "helpers" } )).Status );
		Assert.True( rig.Policy.Explain( DispatcherRig.User, "admin.permissions.explain" ).IsAllowed );
		var groups = await rig.Run( new[] { "user", DispatcherRig.UserId, "group", "list" } );
		Assert.Contains( "helpers", groups.Data.Values );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Run( new[] { "user", DispatcherRig.UserId, "group", "add", "owner" } )).Status );
		Assert.Equal( CommandStatus.InvalidArguments, (await rig.Run( new[] { "group", "delete", "helpers" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "user", DispatcherRig.UserId, "group", "remove", "helpers" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "delete", "helpers" } )).Status );
	}

	[Fact]
	public async Task ManagementGrantDoesNotTurnOrdinaryStaffIntoOwner()
	{
		using var rig = new AdministrationRig();
		await rig.Run( new[] { "user", DispatcherRig.UserId, "permission", "allow", "admin.security.manage" } );
		Assert.Equal( CommandStatus.Denied, (await rig.Run( new[] { "group", "create", "elevated" }, DispatcherRig.User )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Run( new[] { "diagnostics" }, DispatcherRig.User )).Status );
		Assert.Equal( CommandStatus.NotFound, (await rig.Run( new[] { "help", "group.create" }, DispatcherRig.User )).Status );
		Assert.False( rig.Policy.ReadPolicy( DispatcherRig.Owner ).Groups.ContainsKey( "elevated" ) );
	}

	[Fact]
	public async Task ExplainRequiresPermissionForAnotherSubject()
	{
		using var rig = new AdministrationRig();
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "permissions", "explain", DispatcherRig.UserId, "admin.help" }, DispatcherRig.User )).Status );
		Assert.Equal( CommandStatus.Denied, (await rig.Run( new[] { "permissions", "explain", DispatcherRig.OwnerId, "admin.help" }, DispatcherRig.User )).Status );
		var owner = await rig.Run( new[] { "permissions", "explain", DispatcherRig.OwnerId, "admin.help" } );
		Assert.Contains( "ownerBypass=True", string.Join( "", owner.Data.Values ) );
	}

	[Fact]
	public async Task DedicatedConsoleAndPlayerUseSamePolicyRulesWithDistinctAuditActors()
	{
		using var rig = new AdministrationRig();
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "create", "first" } )).Status );
		Assert.Equal( CommandStatus.Success, (await rig.Run( new[] { "group", "create", "second" }, DispatcherRig.ConsoleActor )).Status );
		var rows = await rig.Audit.QueryAsync( () => DispatcherRig.Owner, _ => true, _ => false,
			new AuditQuery( command: "group.create" ), CancellationToken.None );
		Assert.Equal( new[] { ActorKind.DedicatedConsole, ActorKind.Player }, rows.Entries.Select( row => row.ActorKind ) );
		Assert.All( rows.Entries, row => Assert.Equal( CommandStatus.Success, row.Outcome ) );
	}

	[Fact]
	public async Task AuditQueriesDoNotInvalidateTheirOwnPagination()
	{
		using var rig = new AdministrationRig();
		for ( var index = 0; index < 7; index++ )
		{
			await rig.Run( new[] { "version" } );
		}
		var writes = rig.Store.Writes;
		var first = await rig.Run( new[] { "audit", "query", "version" } );
		Assert.Equal( CommandStatus.Success, first.Status );
		Assert.True( first.Data.ContainsKey( "cursor" ) );
		var second = await rig.Run( new[] { "audit", "query", "version", first.Data[ "cursor" ] } );
		Assert.Equal( CommandStatus.Success, second.Status );
		Assert.Equal( 2, second.Data.Count );
		Assert.Equal( writes, rig.Store.Writes );
		Assert.Equal( CommandStatus.Denied, (await rig.Run( new[] { "audit", "query" }, DispatcherRig.User )).Status );
	}
}
