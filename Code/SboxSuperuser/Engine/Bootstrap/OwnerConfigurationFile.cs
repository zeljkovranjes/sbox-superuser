using Sandbox;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Engine.Bootstrap;

internal sealed class OwnerConfigurationFile
{
	internal string Path { get; }
	internal OwnerConfiguration Configuration { get; }
	internal BootstrapState State { get; }
	internal string Message { get; }

	internal OwnerConfigurationFile( bool dedicated )
	{
		Path = dedicated ? "superuser/owners.json" : "superuser/editor-owners.json";
		try
		{
			if ( !FileSystem.Data.FileExists( Path ) )
			{
				State = BootstrapState.OwnersMissing;
				Message = "Owner file missing; no owners granted. Create the explicit host-local configuration and restart bootstrap.";
				return;
			}

			if ( FileSystem.Data.FileSize( Path ) > OwnerConfiguration.MaximumCharacters )
			{
				throw new ArgumentException( "Owner configuration exceeds the byte limit." );
			}

			Configuration = OwnerConfiguration.Parse( FileSystem.Data.ReadAllText( Path ) );
			State = BootstrapState.Ready;
			Message = "Explicit owner configuration loaded. No roles or native claims were imported.";
		}
		catch ( Exception exception )
		{
			State = BootstrapState.OwnersInvalid;
			Message = "Owner configuration unreadable or invalid; no owners granted. Existing file preserved.";
			Log.Warning( $"[Superuser] {Message} Failure type: {exception.GetType().Name}" );
		}
	}
}
