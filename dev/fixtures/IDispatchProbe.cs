using SboxSuperuser;
using System.Threading.Tasks;

namespace SuperuserCompatibilityLab;

/// <summary>Disposable game-owned counter and asynchronous barrier for dispatcher verification.</summary>
public interface IDispatchProbe
{
	/// <summary>Gets the number of observed counter mutations.</summary>
	int Count { get; }
	/// <summary>Creates a fresh hold barrier before a test.</summary>
	void Hold();
	/// <summary>Releases the active test handler.</summary>
	void Release();
	/// <summary>Waits on the explicit test barrier.</summary>
	Task<CommandResult> WaitAsync();
	/// <summary>Increments the test counter on the engine main thread.</summary>
	CommandResult Increment();
}
