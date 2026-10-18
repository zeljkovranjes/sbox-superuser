using Sandbox;

namespace SuperuserCompatibilityLab.Engine.Chat;

internal sealed class ExampleChatGate
{
	private readonly Dictionary<Guid, double> _next = new();

	internal bool Accept( Connection sender, Guid requestId, string text )
	{
		if ( sender is null || !sender.IsActive || requestId == Guid.Empty || string.IsNullOrWhiteSpace( text )
			|| text.Length > 512 || text.Any( char.IsControl ) )
		{
			return false;
		}
		var now = RealTime.Now;
		foreach ( var id in _next.Where( item => item.Value + 30 < now ).Select( item => item.Key ).ToArray() )
		{
			_next.Remove( id );
		}
		if ( _next.TryGetValue( sender.Id, out var next ) ? now < next : _next.Count >= 128 )
		{
			return false;
		}
		_next[ sender.Id ] = now + 0.75;
		return true;
	}
}
