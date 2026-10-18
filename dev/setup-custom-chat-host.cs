if ( !Networking.IsHost || !Game.IsEditor ) throw new InvalidOperationException( "Isolated editor host required." );
if ( Game.ActiveScene.GetAllComponents<SuperuserCompatibilityLab.Components.IndependentChatExample>().Any() )
	throw new InvalidOperationException( "Custom chat backend already exists; inspect before repeating setup." );
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() )
	.OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
bootstrap.Enabled = false;
bootstrap.AllowEditorTesting = true;
bootstrap.CommandRoot = "admin";
bootstrap.Enabled = true;
var started = await SboxSuperuser.Superuser.RegisterModulesAsync( new SboxSuperuser.IAdminModule[]
{
	new SuperuserCompatibilityLab.Engine.Chat.ChatExampleModule()
}, cancellationToken );
if ( started.Status != SboxSuperuser.CommandStatus.Success ) throw new InvalidOperationException( started.Message );
var backend = new GameObject( true, "T14 Independent Chat Backend" );
backend.Components.Create<SuperuserCompatibilityLab.Components.IndependentChatExample>();
backend.NetworkSpawn();
return new { backend.Id, Health = SboxSuperuser.Superuser.GetDiagnostics(), NullSender = SboxSuperuser.Superuser.CheckOrdinaryChat( null ).Status };
