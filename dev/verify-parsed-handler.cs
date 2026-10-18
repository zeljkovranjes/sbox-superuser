// Trusted development setup only; actual metadata discovery, parsing and handler binding run in shipped code.
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var assembly = typeof( SboxSuperuser.Superuser ).Assembly;
object Create( Type type, params object[] arguments ) => Activator.CreateInstance( type, flags, null, arguments, null );
var registryType = assembly.GetType( "SboxSuperuser.Engine.Commands.CommandRegistry", true );
var registry = Create( registryType, new[] { "dog.spawn" }, (Action<System.Collections.Generic.IEnumerable<string>>)(_ => { }), (Func<bool>)(() => true) );
var discover = (Action<SboxSuperuser.CommandRegistrationScope>)assembly.GetType( "SboxSuperuser.Engine.CommandDiscovery.AttributeCommandDiscovery", true )
	.GetMethod( "Discover", flags ).CreateDelegate( typeof( Action<SboxSuperuser.CommandRegistrationScope> ) );
using var scope = (SboxSuperuser.CommandRegistrationScope)Create( typeof( SboxSuperuser.CommandRegistrationScope ), registry, "sample.dogs", discover );
scope.DiscoverCommands();
scope.Commit();
var registered = registryType.GetMethod( "Find", flags ).Invoke( registry, new object[] { "dog.types" } );
var descriptor = (SboxSuperuser.CommandDescriptor)registered.GetType().GetProperty( "Descriptor", flags ).GetValue( registered );
var handler = (SboxSuperuser.CommandHandler)registered.GetType().GetProperty( "Handler", flags ).GetValue( registered );
var tokens = SboxSuperuser.Superuser.TokenizeCommandText( "\"parsed text\" 3 4000000000 1.5 2.5 true playful permanent" );
var parsed = SboxSuperuser.Superuser.ParseCommandArguments( descriptor, tokens.Tokens, Array.Empty<SboxSuperuser.CommandPlayer>() );
if ( !parsed.IsSuccess ) throw new InvalidOperationException( parsed.Error );
var result = await handler( null, parsed.Arguments );
if ( result.Message != "parsed text|3|4000000000|1.5|2.5|True|Playful|True" ) throw new InvalidOperationException( "Typed binding disagreed with parsed values." );
var invalidTokens = tokens.Tokens.ToArray();
invalidTokens[ 1 ] = "2147483648";
var invalid = SboxSuperuser.Superuser.ParseCommandArguments( descriptor, invalidTokens, Array.Empty<SboxSuperuser.CommandPlayer>() );
if ( invalid.IsSuccess ) throw new InvalidOperationException( "The parser accepted an integer outside the handler's CLR type range." );
Log.Info( "[T08] parsed handler PASS: discovered metadata, quoted input, every scalar type, enum, permanent and CLR bounds" );
return new { Pass = true, Result = result.Message, InvalidError = invalid.Error };
