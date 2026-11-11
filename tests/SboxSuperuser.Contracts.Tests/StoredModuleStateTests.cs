using Xunit;
namespace SboxSuperuser.Contracts.Tests;

public class StoredModuleStateTests
{
	static readonly StoreKey Key=new("test.module","state");
	static StoredModuleState<string[]> Adapter(MemoryPolicyStore store)=>new(store,Key,value=>{if(value is null)throw new ArgumentException("Missing state");});
	[Fact] public async Task PersistsWithAuditAndReloads()
	{
		using var rig=new DispatcherRig();var state=Adapter(rig.Store);await state.LoadAsync(Array.Empty<string>(),default);
		rig.Register("save",(context,_)=>state.SaveAsync(new[]{"blocked"},context,"save",()=>rig.Dispatcher.CanUse(context,"test.use")));
		Assert.Equal(CommandStatus.Success,(await rig.Submit("save")).Status);
		var reloaded=Adapter(rig.Store);await reloaded.LoadAsync(Array.Empty<string>(),default);
		Assert.Equal("blocked",Assert.Single(reloaded.Value));
		Assert.Contains("auditIntents",rig.Store.Documents[MemoryPolicyStore.Id(Key)].PayloadJson);
	}
	[Theory][InlineData(StoreWriteStatus.Unknown)][InlineData(StoreWriteStatus.Conflict)][InlineData(StoreWriteStatus.Unavailable)]
	public async Task UnconfirmedWritesFailClosed(StoreWriteStatus status)
	{
		using var rig=new DispatcherRig();var state=Adapter(rig.Store);await state.LoadAsync(Array.Empty<string>(),default);
		rig.Store.WriteOverride=status;
		rig.Register("save",(context,_)=>state.SaveAsync(new[]{"blocked"},context,"save",()=>true));
		Assert.Equal(CommandStatus.StorageUnavailable,(await rig.Submit("save")).Status);Assert.False(state.Ready);
	}
	[Fact] public async Task CorruptionDoesNotBecomeEmptyDefaults()
	{
		var store=new MemoryPolicyStore();store.ReadOverrides[MemoryPolicyStore.Id(Key)]=StoreReadStatus.Corrupt;
		var state=Adapter(store);await Assert.ThrowsAsync<InvalidOperationException>(()=>state.LoadAsync(Array.Empty<string>(),default));Assert.False(state.Ready);
	}
	[Fact] public async Task RevokedAuthorityDoesNotWrite()
	{
		using var rig=new DispatcherRig();var state=Adapter(rig.Store);await state.LoadAsync(Array.Empty<string>(),default);
		rig.Register("save",(context,_)=>state.SaveAsync(new[]{"blocked"},context,"save",()=>false));
		Assert.Equal(CommandStatus.Denied,(await rig.Submit("save")).Status);Assert.Empty(rig.Store.Documents);Assert.True(state.Ready);
	}
}
