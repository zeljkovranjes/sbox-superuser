using SboxSuperuser.Engine.ChatIntegration;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ChatIngressTests
{
	[Fact]
	public async Task MissingAndNonPlayerIdentitiesNeverBecomeConsole()
	{
		using var rig = new AdministrationRig();
		var ingress = new ChatCommandIngress( "admin", rig.Dispatcher, rig.Audit, () => true );
		Assert.Equal( CommandStatus.Denied, (await ingress.ExecuteAsync( () => null, "/admin version", Guid.NewGuid(), default )).Status );
		Assert.Equal( CommandStatus.Denied, (await ingress.ExecuteAsync( () => DispatcherRig.ConsoleActor, "/admin version", Guid.NewGuid(), default )).Status );
		Assert.Equal( 0, rig.Store.Writes );
	}

	[Fact]
	public async Task ChatHelpIsFilteredAndPolicyWritesRetainOwnerChecks()
	{
		using var rig = new AdministrationRig();
		var ingress = new ChatCommandIngress( "admin", rig.Dispatcher, rig.Audit, () => true );
		var help = await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin", Guid.NewGuid(), default );
		Assert.Equal( CommandStatus.Success, help.Status );
		Assert.DoesNotContain( "group.create", string.Join( "", help.Data.Values ) );
		Assert.Equal( CommandStatus.Denied, (await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin group create hidden", Guid.NewGuid(), default )).Status );
		Assert.False( rig.Policy.ReadPolicy( DispatcherRig.Owner ).Groups.ContainsKey( "hidden" ) );
	}

	[Fact]
	public async Task MalformedInputIsAuditedWithoutCopyingRawText()
	{
		using var rig = new AdministrationRig();
		var ingress = new ChatCommandIngress( "operators", rig.Dispatcher, rig.Audit, () => true );
		var id = Guid.NewGuid();
		var result = await ingress.ExecuteAsync( () => DispatcherRig.User, "/operators \"private-sentinel", id, default );
		Assert.Equal( CommandStatus.InvalidArguments, result.Status );
		Assert.Contains( "/operators help", result.Message );
		var records = await rig.Audit.QueryAsync( () => DispatcherRig.Owner, _ => true, _ => false, new AuditQuery( requestId: id ), default );
		var row = Assert.Single( records.Entries );
		Assert.Equal( CommandOrigin.Chat, row.Origin );
		Assert.Equal( CommandStatus.InvalidArguments, row.Outcome );
		Assert.Empty( row.SafeArguments );
		Assert.DoesNotContain( "private-sentinel", string.Join( "", rig.Store.Files.Files.Values ) );
	}

	[Fact]
	public async Task RepeatedRequestIdExecutesOnceAndRecordsChatOrigin()
	{
		using var rig = new AdministrationRig();
		using var scope = new CommandRegistrationScope( rig.Registry, "chat.test" );
		var calls = 0;
		scope.RegisterCommand( new CommandDescriptor( "chat.run", "Count a call", "admin.help", "chat.test" ), ( _, _ ) =>
		{
			calls++;
			return Task.FromResult( new CommandResult( CommandStatus.Success, "Executed" ) );
		} );
		scope.Commit();
		var ingress = new ChatCommandIngress( "admin", rig.Dispatcher, rig.Audit, () => true );
		var id = Guid.NewGuid();
		var first = await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin chat.run", id, default );
		Assert.Same( first, await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin chat.run", id, default ) );
		Assert.Equal( 1, calls );
		var records = await rig.Audit.QueryAsync( () => DispatcherRig.Owner, _ => true, _ => false, new AuditQuery( requestId: id ), default );
		Assert.Equal( CommandOrigin.Chat, Assert.Single( records.Entries ).Origin );
		Assert.Equal( CommandStatus.Conflict, (await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin help", id, default )).Status );
	}

	[Fact]
	public async Task UnownedTextAndInactiveRuntimeDoNotDispatch()
	{
		using var rig = new AdministrationRig();
		var active = true;
		var ingress = new ChatCommandIngress( "admin", rig.Dispatcher, rig.Audit, () => active );
		Assert.Equal( CommandStatus.Unsupported, (await ingress.ExecuteAsync( () => DispatcherRig.User, "!admin help", Guid.NewGuid(), default )).Status );
		active = false;
		Assert.Equal( CommandStatus.Unsupported, (await ingress.ExecuteAsync( () => DispatcherRig.User, "/admin help", Guid.NewGuid(), default )).Status );
		Assert.Equal( 0, rig.Store.Writes );
	}
}
