void Require( bool value, string message )
{
	if ( !value ) throw new InvalidOperationException( message );
}
const string first = "76561198000000001";
const string second = "76561198000000002";
var descriptor = new SboxSuperuser.CommandDescriptor( "test.parse", "Parsing", "test.use", "test", new[]
{
	new SboxSuperuser.CommandArgumentDescriptor( "target", "Target", SboxSuperuser.CommandArgumentKind.Player, targetPolicy: SboxSuperuser.CommandTargetPolicy.AnyPlayer ),
	new SboxSuperuser.CommandArgumentDescriptor( "duration", "Duration", SboxSuperuser.CommandArgumentKind.Duration ),
	new SboxSuperuser.CommandArgumentDescriptor( "reason", "Reason", SboxSuperuser.CommandArgumentKind.String, isRemainder: true )
} );
var tokens = SboxSuperuser.Superuser.TokenizeCommandText( "\"Jane Doe\" 15m \"quoted reason\" more" );
Require( tokens.IsSuccess, "Quoted tokenization failed." );
var result = SboxSuperuser.Superuser.ParseCommandArguments( descriptor, tokens.Tokens, new[] { new SboxSuperuser.CommandPlayer( first, "Jane Doe" ) } );
Require( result.IsSuccess && result.Arguments.Get<SboxSuperuser.PlayerReference>( "target" ).SteamId == first, "Name resolution failed." );
Require( result.Arguments.Get<SboxSuperuser.CommandDuration>( "duration" ).Value == TimeSpan.FromMinutes( 15 ), "Duration conversion failed." );
Require( result.Arguments.Get<string>( "reason" ) == "quoted reason more", "Quoted remainder changed." );
var ambiguous = SboxSuperuser.Superuser.ParseCommandArguments( descriptor, tokens.Tokens,
	new[] { new SboxSuperuser.CommandPlayer( second, "Jane Doe" ), new SboxSuperuser.CommandPlayer( first, "jane doe" ) } );
Require( !ambiguous.IsSuccess && ambiguous.PlayerChoices.Count == 2 && ambiguous.Arguments is null, "Ambiguous name selected a player." );
var offline = SboxSuperuser.Superuser.ParseCommandArguments( descriptor, new[] { second, "permanent", "offline reason" }, Array.Empty<SboxSuperuser.CommandPlayer>() );
Require( offline.IsSuccess && offline.Arguments.Get<SboxSuperuser.CommandDuration>( "duration" ).IsPermanent, "Offline explicit ID/permanent failed." );
foreach ( var input in new[] { "\"unterminated", "a\"b\"", "\"a\"b", "\"bad\\q\"", new string( 'x', 513 ), new string( ' ', 4097 ) } )
{
	var invalid = SboxSuperuser.Superuser.TokenizeCommandText( input );
	Require( !invalid.IsSuccess && invalid.Tokens.Count == 0, "Malformed text produced tokens." );
}
foreach ( var duration in new[] { "0s", "-1m", "1h30m", "9223372036854775807w" } )
{
	Require( !SboxSuperuser.Superuser.ParseCommandArguments( descriptor, new[] { first, duration, "reason" }, Array.Empty<SboxSuperuser.CommandPlayer>() ).IsSuccess,
		"Invalid duration was accepted." );
}
var integer = new SboxSuperuser.CommandDescriptor( "test.integer", "Integer", "test.use", "test", new[]
{
	new SboxSuperuser.CommandArgumentDescriptor( "value", "Value", SboxSuperuser.CommandArgumentKind.Integer, minimum: -1e300, maximum: 1e300 )
} );
Require( SboxSuperuser.Superuser.ParseCommandArguments( integer, new[] { "9223372036854775807" }, Array.Empty<SboxSuperuser.CommandPlayer>() )
	.Arguments.Get<long>( "value" ) == long.MaxValue, "Large integer rounded or overflowed." );
var syntax = SboxSuperuser.Superuser.GetCommandSyntax( descriptor );
Require( syntax.Syntax == "test.parse <target> <duration> <reason...>" && syntax.Arguments[ 1 ].Contains( "permanent" ), "Generated syntax differs from metadata." );
Log.Info( "[T08] parsing PASS: quoted reasons, exact/ambiguous names, offline IDs, durations, bounds and syntax" );
return new { Pass = true, Syntax = syntax.Syntax, AmbiguousChoices = ambiguous.PlayerChoices.Select( value => value.SteamId ).ToArray() };
