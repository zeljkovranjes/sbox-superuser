// Development method body: kill only this dedicated test process after the checkpoint.
if ( !Application.IsDedicatedServer || !Networking.IsHost )
{
	throw new InvalidOperationException( "Use the isolated dedicated test server." );
}

using var candidate = FileSystem.Data.OpenWrite( "superuser-t01/dedicated/candidate.txt" );
var prefix = System.Text.Encoding.UTF8.GetBytes( "T01:" );
candidate.Write( prefix );
candidate.Flush();
Log.Info( "[Superuser T01] INTERRUPTION CHECKPOINT: incomplete candidate open; previous baseline untouched" );
await Task.Delay( 60000, cancellationToken );
return "Interruption window expired; no crash observation was made.";
