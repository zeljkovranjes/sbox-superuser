using Sandbox;

namespace SboxSuperuser.Components;

public sealed partial class CompatibilityProbe : Component.INetworkListener
{
	/// <summary>Optional test account to reject at admission; empty allows every connection.</summary>
	[Property, Group( "Testing" )]
	public string RejectedTestSteamId { get; set; } = "";

	bool Component.INetworkListener.AcceptConnection( Connection connection, ref string reason )
	{
		if ( !CanRun || FindActiveProbe() != this )
		{
			return true;
		}

		var reject = connection.SteamId.ToString() == RejectedTestSteamId;
		Log.Info( $"[Superuser T01] AcceptConnection steamId={connection.SteamId}; rejected={reject}" );
		if ( reject )
		{
			reason = "Superuser T01 admission probe: this test account is deliberately rejected.";
		}

		return !reject;
	}

	void Component.INetworkListener.OnConnected( Connection connection )
	{
		if ( CanRun && FindActiveProbe() == this )
		{
			Log.Info( $"[Superuser T01] OnConnected steamId={connection.SteamId}" );
		}
	}

	void Component.INetworkListener.OnActive( Connection connection )
	{
		if ( CanRun && FindActiveProbe() == this )
		{
			Log.Info( $"[Superuser T01] OnActive steamId={connection.SteamId}" );
		}
	}
}
