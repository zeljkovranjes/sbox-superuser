using SboxSuperuser.Engine.Admin;
using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public class ModuleRestrictionTests
{
 sealed class Provider : IAdminRestrictions
 {
  public int Reads;
  public bool Empty;
  public IReadOnlyList<AdminRestrictionType> Types {get;}=new[]{new AdminRestrictionType("voice","Voice Chat Mute","module.mute","module.restore","test.read")};
  public IReadOnlyList<AdminModerationRow> ReadActive(string kind)
  {
   Reads++;
   if(Empty)return Array.Empty<AdminModerationRow>();
   return new[]{new AdminModerationRow{SteamId=DispatcherRig.TargetId,Kind=kind,RequestId=Guid.NewGuid(),CreatedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(1),Reason="Private reason",Active=true}};
  }
 }
 static HostAdminQueries Queries(ModerationRig rig)=>new(rig.Rig.Policy,rig.Moderation,null,()=>rig.Rig.Players,registry:rig.Rig.Registry);
 static Provider Register(ModerationRig rig)
 {
  var provider=new Provider();
  rig.Rig.Register("module.mute",(c,a)=>Task.FromResult(new CommandResult(CommandStatus.Success,"Muted")),module:"test.restrictions",services:s=>
  {
   s.RegisterPermission(new("test.read","Read restrictions"));
   s.RegisterService<IAdminRestrictions>(provider);
   s.RegisterCommand(new("module.restore","Restore","test.use","test.restrictions"),(c,a)=>Task.FromResult(new CommandResult(CommandStatus.Success,"Restored")));
  });
  return provider;
 }
 [Fact] public async Task ProviderIsNeverReadWithoutPermission()
 {
  using var rig=new ModerationRig();var provider=Register(rig);
  var reply=await Queries(rig).QueryAsync(()=>DispatcherRig.User,new(AdminQueryKind.Player,DispatcherRig.TargetId),Guid.NewGuid(),default);
  Assert.Equal(CommandStatus.Denied,reply.Status);Assert.Equal(0,provider.Reads);Assert.Empty(reply.ModuleActions);Assert.Empty(reply.Restrictions);
 }
 [Fact] public async Task ModulesAppearInBothPlayerAndGlobalRestrictionViews()
 {
  using var rig=new ModerationRig();Register(rig);
  foreach(var query in new[]{new AdminQuery(AdminQueryKind.Player,DispatcherRig.TargetId),new AdminQuery(AdminQueryKind.Restrictions)})
  {
   var reply=await Queries(rig).QueryAsync(()=>DispatcherRig.Owner,query,Guid.NewGuid(),default);
   var row=Assert.Single(reply.Restrictions);Assert.Equal("Voice Chat Mute",row.Kind);Assert.Equal("module.restore",row.RestoreCommand);
   if(query.Kind==AdminQueryKind.Player)Assert.Contains(reply.ModuleActions,a=>a.Command=="module.mute");
  }
 }
 [Fact] public async Task PunitiveModuleActionsRespectTargetProtection()
 {
  using var rig=new ModerationRig();Register(rig);
  var reply=await Queries(rig).QueryAsync(()=>DispatcherRig.Owner,new(AdminQueryKind.Player,DispatcherRig.OwnerId),Guid.NewGuid(),default);
  Assert.DoesNotContain(reply.ModuleActions,a=>!a.Restore);Assert.Contains(reply.ModuleActions,a=>a.Restore);
 }
 [Fact] public async Task ReadOnlyStaffCanReadAnEmptyRestrictionList()
 {
  using var rig=new ModerationRig();var provider=Register(rig);provider.Empty=true;
  await rig.Rig.Edit(PolicyChange.SetUserPermission(DispatcherRig.UserId,"test.read",PermissionEffect.Allow));
  var reply=await Queries(rig).QueryAsync(()=>DispatcherRig.User,new(AdminQueryKind.Restrictions),Guid.NewGuid(),default);
  Assert.Equal(CommandStatus.Success,reply.Status);Assert.Empty(reply.Restrictions);Assert.Empty(reply.ModuleActions);Assert.Equal(1,provider.Reads);
 }
}
