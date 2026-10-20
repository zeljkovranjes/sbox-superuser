using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class AdminDocumentationTests
{
	private static Task<AdminReply> Read( ModerationRig rig, AdminQuery query, ActorIdentity actor )
		=> new HostAdminQueries( rig.Rig.Policy, rig.Moderation, null, () => rig.Rig.Players, registry: rig.Rig.Registry )
			.QueryAsync( () => actor, query, Guid.NewGuid(), CancellationToken.None );

	private static void RegisterDocumented( ModerationRig rig ) =>
		rig.Rig.Register( "docs.ping", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "Pong" ) ),
			module: "example.docs", services: scope =>
			{
				scope.AddDocumentation( "Guide", "# Guide\n\nHello." );
				scope.AddDocumentation( "More", "Second page." );
			} );

	[Fact]
	public void PagesKeepTheirOrderAndAppearInTheModuleList()
	{
		using var rig = new ModerationRig();
		RegisterDocumented( rig );
		var list = Read( rig, new AdminQuery( AdminQueryKind.Documentation ), DispatcherRig.Owner ).Result;
		var module = Assert.Single( list.Modules, row => row.Id == "example.docs" );
		Assert.Equal( new[] { "Guide", "More" }, module.Pages );
		Assert.Contains( module.Permissions, permission => permission.Key == "test.use" );
		Assert.Equal( "Registered directly by game code.", module.Message );
	}

	[Fact]
	public async Task PlayersSeeOnlyModulesTheyCanUseAndAlwaysSuperuser()
	{
		using var rig = new ModerationRig();
		RegisterDocumented( rig );
		var user = await Read( rig, new AdminQuery( AdminQueryKind.Documentation ), DispatcherRig.User );
		Assert.DoesNotContain( user.Modules, row => row.Id == "example.docs" );
		var superuser = Assert.Single( user.Modules, row => row.Id == "superuser.administration" );
		Assert.Contains( "Getting started", superuser.Pages );
		Assert.False( string.IsNullOrEmpty( superuser.Version ) );

		await rig.Rig.Edit( PolicyChange.SetUserPermission( DispatcherRig.UserId, "test.use", PermissionEffect.Allow ) );
		var allowed = await Read( rig, new AdminQuery( AdminQueryKind.Documentation ), DispatcherRig.User );
		Assert.Contains( allowed.Modules, row => row.Id == "example.docs" );
	}

	[Fact]
	public async Task PagesAreReadByModuleAndIndex()
	{
		using var rig = new ModerationRig();
		RegisterDocumented( rig );
		var page = await Read( rig, new AdminQuery( AdminQueryKind.Documentation, "example.docs", 1 ), DispatcherRig.Owner );
		Assert.Equal( "More", page.DocumentTitle );
		Assert.Equal( "Second page.", page.Document );

		var missing = await Read( rig, new AdminQuery( AdminQueryKind.Documentation, "example.docs", 5 ), DispatcherRig.Owner );
		Assert.Equal( CommandStatus.NotFound, missing.Status );
		var hidden = await Read( rig, new AdminQuery( AdminQueryKind.Documentation, "example.docs", 0 ), DispatcherRig.User );
		Assert.Equal( CommandStatus.NotFound, hidden.Status );
	}

	[Fact]
	public void RegistrationEnforcesDocumentationLimits()
	{
		using var rig = new ModerationRig();
		Assert.Throws<ArgumentException>( () => rig.Rig.Register( "docs.many", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "" ) ),
			module: "example.many", services: scope => { for ( var i = 0; i < 33; i++ ) scope.AddDocumentation( $"Page {i}", "text" ); } ) );
		Assert.Throws<ArgumentException>( () => rig.Rig.Register( "docs.path", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "" ) ),
			module: "example.path", services: scope => scope.AddDocumentationFile( "Guide", "../secrets.md" ) ) );
		Assert.Throws<NotSupportedException>( () => rig.Rig.Register( "docs.file", ( _, _ ) => Task.FromResult( new CommandResult( CommandStatus.Success, "" ) ),
			module: "example.file", services: scope => scope.AddDocumentationFile( "Guide", "docs/guide.md" ) ) );
	}

	[Fact]
	public void DescriptorMetadataIsOptionalAndBounded()
	{
		var descriptor = new ModuleDescriptor( "example.meta", "Example", 1 ) { Version = "1.2.0", Author = "chomnr", Website = "https://example.com" };
		Assert.Equal( "1.2.0", descriptor.Version );
		Assert.Equal( "", new ModuleDescriptor( "example.plain", "Example", 1 ).Author );
		Assert.Throws<ArgumentException>( () => new ModuleDescriptor( "example.bad", "Example", 1 ) { Author = "line\nbreak" } );
		Assert.Throws<ArgumentException>( () => new ModuleDescriptor( "example.long", "Example", 1 ) { Version = new string( '1', 33 ) } );
	}
}
