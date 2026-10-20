namespace SboxSuperuser;

/// <summary>
/// Loads this module automatically: when the bootstrap starts, Superuser creates it with its public parameterless
/// constructor, installs it and starts it, so adding the module's code to a game is all it takes.
/// </summary>
[AttributeUsage( AttributeTargets.Class, Inherited = false )]
public sealed class AdminModuleAttribute : Attribute
{
}
