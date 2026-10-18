namespace SboxSuperuser.Engine.Modules;

internal interface IModuleCatalog
{
	IReadOnlyList<ModuleStatus> Statuses { get; }
}
