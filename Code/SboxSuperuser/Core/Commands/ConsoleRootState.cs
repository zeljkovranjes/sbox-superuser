namespace SboxSuperuser.Core.Commands;

internal sealed class ConsoleRootState
{
	internal string Name { get; }
	internal bool IsAvailable { get; }
	internal string Message { get; }

	internal ConsoleRootState( string name, IEnumerable<string> registeredNativeNames )
	{
		Name = name ?? "";
		try
		{
			CommandRootName.Validate( name );
		}
		catch ( ArgumentException )
		{
			Message = "Invalid console root. Use 1–32 lowercase letters, digits or underscores, starting with a letter.";
			return;
		}

		var names = registeredNativeNames.ToArray();
		foreach ( var required in new[] { "superuser", "superuser_request", name } )
		{
			var expected = required == name && name != "admin" ? 0 : 1;
			if ( names.Count( value => string.Equals( value, required, StringComparison.OrdinalIgnoreCase ) ) != expected )
			{
				Message = $"Native root/transport collision or missing registration: {required}. Configure another logical root or remove the conflicting managed entry.";
				return;
			}
		}

		IsAvailable = true;
		Message = name == "admin" ? "Native admin entry ready; fallback: superuser admin <command>."
			: $"Configured logical root ready: superuser {name} <command>. Dynamic native aliases are unavailable in this engine.";
	}
}
