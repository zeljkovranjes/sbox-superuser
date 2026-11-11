using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class ModerationServiceCapabilityTests
{
	static CommandService Service(ModerationRig fixture, int? ceiling, string[] permissions) => new("test.moderator", permissions,
		(service, request, cancellation) => fixture.Rig.Dispatcher.SubmitAsync(
			() => service.IsActive ? new ActorIdentity(ActorKind.InternalService, service.Id) : null,
			CommandOrigin.InternalService, request, cancellation, service), ceiling);

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)]
	[InlineData(99, true)]
	public async Task PunishmentRequiresExplicitTargetCapability(int? ceiling, bool allowed)
	{
		using var fixture = new ModerationRig();
		using var service = Service(fixture, ceiling, new[]{"admin.players.warn"});
		var result = await service.ExecuteAsync(new("warn",new[]{DispatcherRig.TargetId,"Test warning"},Guid.NewGuid()));
		Assert.Equal(allowed ? CommandStatus.Success : CommandStatus.Denied, result.Status);
	}

	[Fact]
	public async Task ServiceCannotPunishOwnersOrUseUnassignedCommands()
	{
		using var fixture = new ModerationRig();
		using var service = Service(fixture,99,new[]{"admin.players.warn"});
		Assert.Equal(CommandStatus.Denied,(await service.ExecuteAsync(new("warn",new[]{DispatcherRig.OwnerId,"Test"},Guid.NewGuid()))).Status);
		Assert.Equal(CommandStatus.Denied,(await service.ExecuteAsync(new("ban",new[]{DispatcherRig.TargetId,"3d","Test"},Guid.NewGuid()))).Status);
		service.Dispose();
		Assert.Equal(CommandStatus.Denied,(await service.ExecuteAsync(new("warn",new[]{DispatcherRig.TargetId,"Test"},Guid.NewGuid()))).Status);
	}

	[Fact]
	public void OwnerLevelCannotBeIssuedToService()
	{
		using var fixture = new ModerationRig();
		Assert.Throws<ArgumentOutOfRangeException>(()=>Service(fixture,100,new[]{"admin.players.warn"}));
	}
}
