using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Engine.NativeSettings;

namespace SboxSuperuser;

/// <summary>Explicit host-process ConVar binding; declaration never bypasses native access checks.</summary>
public sealed class NativeSettingDescriptor
{
	/// <summary>Gets the stable settings key.</summary>
	public string Key { get; }
	/// <summary>Gets the exact native name; no console expression is accepted.</summary>
	public string NativeName { get; }
	/// <summary>Gets the help shown to authorized operators.</summary>
	public string Description { get; }
	/// <summary>Gets the exact permission for reading or changing this binding.</summary>
	public string Permission { get; }
	/// <summary>Gets the scalar type and constraints used for writes and readback.</summary>
	public CommandArgumentDescriptor Value { get; }
	/// <summary>Gets the explicitly registered canonical reset value.</summary>
	public string DefaultValue { get; }
	/// <summary>Gets the sole persistence owner.</summary>
	public NativePersistence Persistence { get; }
	/// <summary>Gets whether changes wait for a new runtime instead of changing the active session.</summary>
	public bool RequiresRestart { get; }
	/// <summary>Gets whether values must be hidden and chat access refused.</summary>
	public bool Sensitive { get; }
	/// <summary>Gets the binding scope; client configuration is never changed by these bindings.</summary>
	public string Scope => "Host";

	/// <summary>Declares a supported scalar ConVar and a required explicit default; no native entry is discovered automatically.</summary>
	public NativeSettingDescriptor( string key, string nativeName, string description, string permission,
		CommandArgumentKind kind, string defaultValue, NativePersistence persistence = NativePersistence.Session,
		double? minimum = null, double? maximum = null, IEnumerable<string> choices = null,
		bool requiresRestart = false, bool sensitive = false )
	{
		Key = NativeValue.Key( key );
		NativeName = NativeValue.Name( nativeName );
		Description = ContractGuard.Text( description, nameof( description ) );
		Permission = ContractGuard.Key( permission, nameof( permission ) );
		Persistence = ContractGuard.EnumValue( persistence, nameof( persistence ) );
		if ( kind is not (CommandArgumentKind.String or CommandArgumentKind.Integer or CommandArgumentKind.Number
			or CommandArgumentKind.Boolean or CommandArgumentKind.Enum) )
			throw new ArgumentException( "Native settings require a scalar type.", nameof( kind ) );
		if ( requiresRestart && persistence != NativePersistence.Framework )
			throw new ArgumentException( "Pending settings require framework persistence.", nameof( persistence ) );
		Sensitive = sensitive;
		RequiresRestart = requiresRestart;
		Value = new CommandArgumentDescriptor( "value", description, kind, minimum: minimum, maximum: maximum,
			choices: choices, audit: sensitive ? CommandArgumentAudit.Sensitive : CommandArgumentAudit.Redacted );
		DefaultValue = NativeValue.Normalize( this, defaultValue )
			?? throw new ArgumentException( "Default must satisfy the binding's type and constraints.", nameof( defaultValue ) );
	}
}
