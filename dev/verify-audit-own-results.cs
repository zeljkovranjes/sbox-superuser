if ( !Game.IsEditor || !Networking.IsHost || FileSystem.Data.FileExists( "superuser/editor-owners.json" ) )
	throw new InvalidOperationException( "Isolated editor with no configured owners required." );
var bootstrap = Game.ActiveScene.GetAllComponents<SboxSuperuser.Components.SuperuserBootstrap>().Single();
var originalSwitch = bootstrap.AllowEditorTesting;
SboxSuperuser.CommandRegistrationScope registration = null;
SboxSuperuser.CommandService service = null;
try
{
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = true;
	bootstrap.Enabled = true;
	var probe = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DispatchProbe" ).Create<SuperuserCompatibilityLab.IDispatchProbe>();
	registration = SboxSuperuser.Superuser.BeginCommandRegistration( "test.dispatch" );
	registration.RegisterService<SuperuserCompatibilityLab.IDispatchProbe>( probe );
	registration.DiscoverCommands();
	registration.Commit();
	var request = new SboxSuperuser.CommandRequest( "probe.increment", Array.Empty<string>(), Guid.NewGuid() );
	await SboxSuperuser.Superuser.ExecuteCommandAsync( Connection.Local, request, SboxSuperuser.CommandOrigin.Chat );
	service = SboxSuperuser.Superuser.CreateCommandService( "audit.test", new[] { "admin.help" } );
	await service.ExecuteAsync( new SboxSuperuser.CommandRequest( "probe.increment", Array.Empty<string>(), Guid.NewGuid() ) );
	var own = await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery() );
	if ( own.Status != SboxSuperuser.CommandStatus.Success || own.Entries.Count != 1 || own.Entries.Single().RequestId != request.RequestId || probe.Count != 2 )
		throw new InvalidOperationException( "Own-result query exposed service data or omitted the caller." );
	if ( (await SboxSuperuser.Superuser.QueryAuditAsync( Connection.Local, new SboxSuperuser.AuditQuery( actorSubject: "audit.test" ) )).Status != SboxSuperuser.CommandStatus.Denied
		|| (await SboxSuperuser.Superuser.QueryAuditAsync( null, new SboxSuperuser.AuditQuery() )).Status != SboxSuperuser.CommandStatus.Denied
		|| (await SboxSuperuser.Superuser.ConfigureAuditRetentionAsync( Connection.Local, new SboxSuperuser.AuditRetentionOptions() )).Status != SboxSuperuser.CommandStatus.Denied )
		throw new InvalidOperationException( "Audit access boundary failed." );
	return new { Pass = true, OwnRows = own.Entries.Count, Mutations = probe.Count, Owner = SboxSuperuser.Superuser.IsOwner( Connection.Local ) };
}
finally
{
	service?.Dispose();
	registration?.Dispose();
	bootstrap.Enabled = false;
	bootstrap.AllowEditorTesting = originalSwitch;
	bootstrap.Enabled = true;
}
