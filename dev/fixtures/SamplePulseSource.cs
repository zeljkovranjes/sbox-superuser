namespace SuperuserCompatibilityLab.Engine.Modules;

/// <summary>A game-owned signal used to demonstrate subscription cleanup without static state.</summary>
public sealed class SamplePulseSource
{
	private event Action Pulse;
	/// <summary>Gets the number of currently attached listeners.</summary>
	public int Listeners { get; private set; }

	/// <summary>Registers one listener and returns its lifetime handle.</summary>
	public IDisposable Subscribe( Action listener )
	{
		Pulse += listener;
		Listeners++;
		return new SamplePulseSubscription( () =>
		{
			Pulse -= listener;
			Listeners--;
		} );
	}

	/// <summary>Raises a game event once for every live listener.</summary>
	public void Raise() => Pulse?.Invoke();
}
