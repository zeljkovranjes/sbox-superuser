// Trusted development script: reflection creates test-only internal dependencies; shipped discovery uses TypeLibrary.
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var assembly = typeof( SboxSuperuser.Superuser ).Assembly;
object Create( Type type, params object[] arguments ) => Activator.CreateInstance( type, flags, null, arguments, null );
void Require( bool pass, string message )
{
	if ( !pass ) throw new InvalidOperationException( message );
}
var registryType = assembly.GetType( "SboxSuperuser.Engine.Commands.CommandRegistry", true );
var registry = Create( registryType, new[] { "dog.spawn" }, (Action<System.Collections.Generic.IEnumerable<string>>)(_ => { }), (Func<bool>)(() => true) );
var discover = (Action<SboxSuperuser.CommandRegistrationScope>)assembly.GetType( "SboxSuperuser.Engine.CommandDiscovery.AttributeCommandDiscovery", true )
	.GetMethod( "Discover", flags ).CreateDelegate( typeof( Action<SboxSuperuser.CommandRegistrationScope> ) );
SboxSuperuser.CommandRegistrationScope Scope( string module ) => (SboxSuperuser.CommandRegistrationScope)Create( typeof( SboxSuperuser.CommandRegistrationScope ), registry, module, discover );
object Find( string name ) => registryType.GetMethod( "Find", flags ).Invoke( registry, new object[] { name } );
SboxSuperuser.CommandHandler Handler( object command ) => (SboxSuperuser.CommandHandler)command.GetType().GetProperty( "Handler", flags ).GetValue( command );
SboxSuperuser.CommandArguments Arguments( System.Collections.Generic.Dictionary<string, object> values ) => (SboxSuperuser.CommandArguments)Create( typeof( SboxSuperuser.CommandArguments ), values );
using var first = Scope( "sample.dogs" );
first.DiscoverCommands();
first.Commit();
Require( Find( "dog.spawn" ) is not null && ReferenceEquals( Find( "dog" ), Find( "dog.spawn" ) ), "Dog discovery/alias failed." );
var typed = await Handler( Find( "dog.types" ) )( null, Arguments( new System.Collections.Generic.Dictionary<string, object>
{
	[ "text" ] = "typed", [ "count" ] = 3L, [ "large" ] = 4000000000L, [ "small" ] = 1.5d,
	[ "number" ] = 2.5d, [ "enabled" ] = true, [ "mood" ] = "playful", [ "duration" ] = SboxSuperuser.CommandDuration.Permanent
} ) );
Require( typed.Status == SboxSuperuser.CommandStatus.Success && typed.Message == "typed|3|4000000000|1.5|2.5|True|Playful|True", "Typed invocation failed: " + typed.Message );
var gameAssembly = TypeLibrary.GetType( "SuperuserCompatibilityLab.Engine.Commands.DogCommands" ).TargetType.Assembly;
var services = (SboxSuperuser.ICommandServices)Create( gameAssembly.GetType( "SuperuserCompatibilityLab.Engine.Commands.EmptyCommandServices", true ) );
var actor = Create( typeof( SboxSuperuser.ActorIdentity ), SboxSuperuser.ActorKind.DedicatedConsole, "dedicated-console" );
var context = (SboxSuperuser.CommandContext)Create( typeof( SboxSuperuser.CommandContext ), actor, SboxSuperuser.CommandOrigin.Console, Guid.NewGuid(), CancellationToken.None, services );
var dog = await Handler( Find( "dog.spawn" ) )( context, Arguments( new System.Collections.Generic.Dictionary<string, object> { [ "target" ] = new SboxSuperuser.PlayerReference( "76561198000000001" ) } ) );
Require( dog.Status == SboxSuperuser.CommandStatus.Unsupported, "Missing game capability should return Unsupported." );
var invalid = new System.Collections.Generic.List<string>();
foreach ( var module in new[] { "invalid.instance", "invalid.void", "invalid.type", "invalid.generic", "invalid.ref" } )
{
	using var scope = Scope( module );
	try
	{
		scope.DiscoverCommands();
		throw new InvalidOperationException( "Invalid handler was not rejected: " + module );
	}
	catch ( ArgumentException error )
	{
		Require( error.Message.Contains( module ) && error.Message.Contains( "InvalidCommandSamples" ), "Failure must identify module and method." );
		invalid.Add( module + ": " + error.Message );
	}
}
var old = Find( "dog.spawn" );
using var replacement = Scope( "sample.dogs" );
replacement.DiscoverCommands();
replacement.Commit();
first.Dispose();
Require( Find( "dog.spawn" ) is not null && !ReferenceEquals( old, Find( "dog.spawn" ) ), "Replacement retained a stale registration." );
replacement.Dispose();
Require( Find( "dog" ) is null, "Disposal retained an alias." );
Log.Info( "[T07] command discovery PASS: typed invocation, optional defaults, missing capability, invalid signatures, replacement and disposal" );
return new { Pass = true, TypedResult = typed.Message, DogResult = dog.Status.ToString(), Invalid = invalid };
