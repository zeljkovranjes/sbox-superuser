namespace SboxSuperuser.Engine.Modules;

// T12 boundary: metadata view, not a claim that modules have started or validated dependencies.
internal interface IModuleCatalog
{
	IReadOnlyList<ModuleDescriptor> Descriptors { get; }
}
