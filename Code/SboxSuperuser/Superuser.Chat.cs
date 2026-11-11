using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Sends printable server text to one active connection using the private Superuser chat style. Client calls are ignored; invalid text throws.</summary>
	public static void Notify( Connection recipient, string message )
	{
		if ( !Networking.IsHost ) return;
		if ( string.IsNullOrWhiteSpace( message ) || message.Length > 1024 || message.Any( char.IsControl ) )
			throw new ArgumentException( "Notifications must be a single printable line of at most 1024 characters.", nameof( message ) );
		Engine.ChatIntegration.Platform.ChatReplies.Send( recipient, Guid.NewGuid(), new CommandResult( CommandStatus.Success, message ) );
	}

	/// <summary>Consumes an owned chat command on the host and sends its eventual result privately; true is not execution success.</summary>
	public static bool TryHandleChat( Connection authenticatedSender, string message, Guid requestId )
	{
		ArgumentNullException.ThrowIfNull( message );
		if ( requestId == Guid.Empty )
		{
			throw new ArgumentException( "A stable nonempty submission ID is required.", nameof( requestId ) );
		}
		return FindAuthorityRuntime()?.Chat?.TryHandle( authenticatedSender, message, requestId ) == true;
	}

	/// <summary>Checks host authentication and framework text restrictions before ordinary-message broadcast; only Success permits delivery.</summary>
	public static CommandResult CheckOrdinaryChat( Connection authenticatedSender )
	{
		var runtime = FindAuthorityRuntime();
		return runtime?.CheckOrdinaryChat( authenticatedSender )
			?? new CommandResult( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
	}
}
