using Sandbox;

namespace SboxSuperuser.Engine.NativeSettings;

internal static class NativeConsole
{
	private static ConVarAttribute Find( string name, bool command )
	{
		var matches = TypeLibrary.GetMemberAttributes<ConVarAttribute>()
			.Where( entry => entry.Name == name && (entry is ConCmdAttribute) == command ).Take( 2 ).ToArray();
		if ( matches.Length != 1 ) return null;
		var attribute = matches[ 0 ];
		// Running as host must never promote a framework player to native Admin or Cheat authority.
		return (attribute.Flags & (ConVarFlags.Protected | ConVarFlags.Admin | ConVarFlags.Cheat)) == 0 ? attribute : null;
	}

	internal static bool Accessible( NativeSettingDescriptor binding )
	{
		var attribute = Find( binding.NativeName, false );
		if ( attribute is null ) return false;
		var saved = attribute.Saved || (attribute.Flags & ConVarFlags.Saved) != 0;
		if ( saved != (binding.Persistence == NativePersistence.Engine) ) return false;
		return !binding.Sensitive || (attribute.Flags & (ConVarFlags.Replicated | ConVarFlags.UserInfo | ConVarFlags.ChangeNotice)) == 0;
	}

	internal static bool TryRead( NativeSettingDescriptor binding, out string value )
	{
		value = null;
		if ( !Accessible( binding ) ) return false;
		try
		{
			value = NativeValue.Normalize( binding, ConsoleSystem.GetValue( binding.NativeName, null ), validateConstraints: false );
			return value is not null;
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Native setting read rejected; type={error.GetType().Name}." );
			return false;
		}
	}

	internal static CommandStatus Write( NativeSettingDescriptor binding, string value )
	{
		if ( !TryRead( binding, out var previous ) ) return CommandStatus.Unsupported;
		try
		{
			ConsoleSystem.SetValue( binding.NativeName, value );
			if ( TryRead( binding, out var current ) && current == value ) return CommandStatus.Success;
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Native setting write rejected; type={error.GetType().Name}." );
		}
		return Restore( binding, previous ) ? CommandStatus.Failed : CommandStatus.Unknown;
	}

	internal static bool Restore( NativeSettingDescriptor binding, string previous )
	{
		if ( !Accessible( binding ) ) return false;
		try
		{
			ConsoleSystem.SetValue( binding.NativeName, previous );
			return TryRead( binding, out var restored ) && restored == previous;
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Native setting rollback failed; type={error.GetType().Name}." );
			return false;
		}
	}

	internal static CommandResult Run( NativeCommandBindingDescriptor binding, CommandArguments arguments )
	{
		if ( Find( binding.NativeName, true ) is null )
			return new CommandResult( CommandStatus.Unsupported, "Native command is inaccessible or its flags cannot be verified." );
		if ( binding.Command.Arguments.Any( argument => argument.Audit == CommandArgumentAudit.Sensitive || Audit.AuditRedaction.SensitiveName( argument.Name ) ) )
			return new CommandResult( CommandStatus.Unsupported, "Sensitive native command arguments require an independently verified game adapter." );
		var values = binding.Command.Arguments.Select( argument => (object)NativeValue.Format( argument, arguments ) ).ToArray();
		try
		{
			ConsoleSystem.Run( binding.NativeName, values );
		}
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Native command submission failed; type={error.GetType().Name}." );
		}
		return new CommandResult( CommandStatus.Unknown, "Native command submitted; the engine provides no execution acknowledgment. Verify the effect before retrying." );
	}
}
