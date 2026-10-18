// Trusted development script for the saved isolated bootstrap scene on an actual dedicated server.
if ( !Application.IsDedicatedServer || !Networking.IsHost )
{
	throw new InvalidOperationException( "Use the isolated dedicated bootstrap fixture." );
}

var systemType = typeof(SboxSuperuser.Superuser).Assembly.GetType( "SboxSuperuser.Systems.SuperuserSession" );
var accessor = typeof(Scene).GetMethods().Single( method => method.Name == "GetSystem"
	&& method.IsGenericMethodDefinition && method.GetParameters().Length == 0 ).MakeGenericMethod( systemType );
var runtimeField = systemType.GetField( "_runtime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
var runtimeIds = new System.Collections.Generic.HashSet<Guid>();

for ( var index = 0; index < 3; index++ )
{
	var oldScene = Game.ActiveScene;
	var oldSystem = accessor.Invoke( oldScene, null );
	var oldRuntime = runtimeField.GetValue( oldSystem );
	var before = SboxSuperuser.Superuser.GetDiagnostics();
	if ( before.RuntimeId is null || before.BootstrapCount != 1 || !runtimeIds.Add( before.RuntimeId.Value ) )
	{
		throw new InvalidOperationException( "Expected one fresh runtime before scene replacement." );
	}

	var options = new SceneLoadOptions();
	if ( !options.SetScene( "scenes/bootstrap.scene" ) ) throw new InvalidOperationException( "Fixture unavailable." );
	Game.ChangeScene( options );
	for ( var attempt = 0; attempt < 100 && (SboxSuperuser.Superuser.GetDiagnostics().RuntimeId is null
		|| SboxSuperuser.Superuser.GetDiagnostics().RuntimeId == before.RuntimeId); attempt++ )
	{
		await Task.Delay( 100, cancellationToken );
	}

	var after = SboxSuperuser.Superuser.GetDiagnostics();
	var disposedField = oldRuntime.GetType().GetField( "_disposed",
		System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
	// This build reuses the Scene/system container while replacing its contents.
	if ( after.RuntimeId is null || after.RuntimeId == before.RuntimeId
		|| after.BootstrapCount != 1 || !(bool)disposedField.GetValue( oldRuntime ) )
	{
		throw new InvalidOperationException( "Scene transition retained an old runtime or failed to initialize one replacement." );
	}

	var liveCount = Scene.All.Select( scene => accessor.Invoke( scene, null ) )
		.Count( system => system is not null && runtimeField.GetValue( system ) is not null );
	if ( liveCount != 1 ) throw new InvalidOperationException( $"Expected one runtime across scenes, found {liveCount}." );
	Log.Info( $"[Superuser T03] Scene replacement {index + 1}: PASS; old={before.RuntimeId}; new={after.RuntimeId}; runtimes={liveCount}" );
}

return "PASS: three real scene replacements disposed old runtimes and retained exactly one new runtime.";
