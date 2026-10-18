// Run on a freshly restarted dedicated fixture after verify-native-setting-lifetime.cs.
if ( !Application.IsDedicatedServer || !Networking.IsHost ) throw new InvalidOperationException( "Actual dedicated host required." );
var before = ConsoleSystem.GetValue( "superuser_example_pending" );
var started = await SboxSuperuser.Superuser.RegisterModulesAsync( new[] { SuperuserCompatibilityLab.Engine.Settings.NativeBindingExample.CreateModule() }, cancellationToken );
if ( started.Status != SboxSuperuser.CommandStatus.Success ) throw new InvalidOperationException( started.Message );
using var service = SboxSuperuser.Superuser.CreateCommandService( "t15.restart", new[] { "example.settings" } );
var result = await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "settings.get.example.pending", Array.Empty<string>(), Guid.NewGuid() ), cancellationToken );
if ( before != "2" || result.Status != SboxSuperuser.CommandStatus.Success || result.Data[ "active" ] != "6"
	|| result.Data[ "desired" ] != "6" || result.Data[ "pending" ] != "False" )
	throw new InvalidOperationException( "Dedicated process restart did not restore the pending setting." );
return new { Pass = true, Dedicated = true, BeforeRegistration = before, Active = result.Data[ "active" ], Desired = result.Data[ "desired" ] };
