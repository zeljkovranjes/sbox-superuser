using SboxSuperuser.Engine.Commands;
using SboxSuperuser.Engine.NativeSettings;

namespace SboxSuperuser;

/// <summary>An allowlisted native command with typed dispatcher metadata; native submission has no success acknowledgment.</summary>
public sealed class NativeCommandBindingDescriptor
{
	/// <summary>Gets the validated framework descriptor, owned by superuser.settings.</summary>
	public CommandDescriptor Command { get; }
	/// <summary>Gets the exact native name passed separately from typed arguments.</summary>
	public string NativeName { get; }
	/// <summary>Gets the explicit execution scope.</summary>
	public string Scope => "Host";

	/// <summary>Declares a native command; player targets, durations and optional native parameters are deliberately unsupported.</summary>
	public NativeCommandBindingDescriptor( string key, string nativeName, string description, string permission,
		IEnumerable<CommandArgumentDescriptor> arguments = null )
	{
		NativeName = NativeValue.Name( nativeName );
		Command = new CommandDescriptor( "native." + NativeValue.Key( key ), description, permission, "superuser.settings", arguments );
		CommandMetadata.Validate( Command );
		if ( Command.Arguments.Any( argument => argument.IsOptional || argument.Kind is CommandArgumentKind.Player or CommandArgumentKind.Duration ) )
			throw new ArgumentException( "Native bindings require explicit scalar arguments.", nameof( arguments ) );
	}
}
