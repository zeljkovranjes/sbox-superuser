namespace SboxSuperuser;

/// <summary>Host-approved game capabilities; implementations must expose only explicitly approved services.</summary>
public interface ICommandServices
{
	/// <summary>Returns an approved service, or null when unavailable; never constructs a fallback implementation.</summary>
	T GetService<T>() where T : class;
}
