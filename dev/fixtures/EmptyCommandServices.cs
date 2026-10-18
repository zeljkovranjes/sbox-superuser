using SboxSuperuser;

namespace SuperuserCompatibilityLab.Engine.Commands;

internal sealed class EmptyCommandServices : ICommandServices
{
	/// <summary>Reports game capabilities unavailable in the compatibility fixture.</summary>
	public T GetService<T>() where T : class => null;
}
