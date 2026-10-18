// Run after a real library source edit has compiled and hotloaded in the isolated editor.
var retained = (object[])AppDomain.CurrentDomain.GetData( "superuser.t17" );
if ( retained is null ) throw new InvalidOperationException( "Run setup-source-reload first." );
object Property( object value, string name ) => value.GetType().GetProperty( name ).GetValue( value );
void Require( bool value, string message ) { if ( !value ) throw new InvalidOperationException( message ); }
var diagnostics = SboxSuperuser.Superuser.GetDiagnostics();
Require( diagnostics.State == SboxSuperuser.BootstrapState.RestartRequired && diagnostics.RuntimeId is null, "Source reload did not require a fresh session: " + diagnostics.State );
Require( (int)Property( retained[ 0 ], "Listeners" ) == 0, "Old subscription remained active." );
Require( (int)Property( retained[ 1 ], "Stops" ) == 1, "Sample cleanup did not run exactly once." );
Require( (bool)Property( retained[ 2 ], "CancellationSeen" ), "Pending operation did not receive cancellation." );
retained[ 2 ].GetType().GetMethod( "Release" ).Invoke( retained[ 2 ], null );
var pending = (Task)retained[ 4 ];
for ( var i = 0; i < 100 && !pending.IsCompleted; i++ ) await Task.Delay( 25, cancellationToken );
// Arbitrary source replacement cannot preserve an already-running async state machine.
// Do not turn an unresolved old task into a success claim; restart is mandatory.
var pendingOutcome = pending.IsCompletedSuccessfully ? Property( Property( pending, "Result" ), "Status" ).ToString() : "UnresolvedAcrossSourceReload";
Require( pendingOutcome is "Unknown" or "UnresolvedAcrossSourceReload", "Interrupted operation reported a definite result." );
var bootstrap = Game.ActiveScene.GetAllObjects( true ).SelectMany( item => item.Components.GetAll() ).OfType<SboxSuperuser.Components.SuperuserBootstrap>().Single();
bootstrap.Enabled = false;
bootstrap.Enabled = true;
Require( SboxSuperuser.Superuser.GetDiagnostics().State == SboxSuperuser.BootstrapState.RestartRequired, "Bootstrap toggle bypassed restart requirement." );
Require( !SboxSuperuser.Superuser.TryGetPlayerIdentity( Connection.Local, out _ ), "Retired runtime still authenticated." );
Require( SboxSuperuser.Superuser.CheckOrdinaryChat( Connection.Local ).Status == SboxSuperuser.CommandStatus.Unsupported, "Custom chat failed open." );
var message = new ChatMessageEvent { Sender = Connection.Local, Message = "/admin ban private-lifecycle-reason" };
((IChatEvent)bootstrap).OnChatMessage( message );
Require( message.Suppress, "Retired runtime leaked command text to ordinary chat." );
return new { Pass = true, diagnostics.State, OldListeners = 0, InterruptedOutcome = pendingOutcome, OldRuntime = retained[ 6 ], message.Suppress };
