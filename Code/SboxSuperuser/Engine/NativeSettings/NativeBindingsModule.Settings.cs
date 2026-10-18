using Sandbox;

namespace SboxSuperuser.Engine.NativeSettings;

internal sealed partial class NativeBindingsModule
{
	private async Task<NativeSettingState> ReadAsync( NativeSettingDescriptor binding, CancellationToken cancellation )
	{
		if ( binding.Persistence != NativePersistence.Framework ) return new NativeSettingState( 0, null );
		try
		{
			return NativeSettingState.Read( binding, await _runtime.Store.ReadAsync( Key( binding ), 1, cancellation ) );
		}
		catch ( OperationCanceledException ) { throw; }
		catch ( Exception error )
		{
			Log.Warning( $"[Superuser] Setting state unavailable; type={error.GetType().Name}; files preserved." );
			return null;
		}
	}

	private async Task<CommandResult> ExecuteSettingAsync( CommandContext context, NativeSettingDescriptor binding, string operation, string desired )
	{
		if ( binding.Sensitive && context.Origin == CommandOrigin.Chat )
			return new CommandResult( CommandStatus.Denied, "Sensitive bindings require a private non-chat transport." );
		var stored = await ReadAsync( binding, context.Cancellation );
		context.Cancellation.ThrowIfCancellationRequested();
		if ( !_runtime.Dispatcher.CanUse( context, binding.Permission ) )
			return new CommandResult( CommandStatus.Denied, "Binding permission or runtime authority ended." );
		if ( stored is null ) return new CommandResult( CommandStatus.StorageUnavailable, "Setting state is unavailable; explicit recovery is required." );
		if ( !NativeConsole.TryRead( binding, out var previous ) )
			return new CommandResult( CommandStatus.Unsupported, "Native access, readback, flags or persistence ownership could not be verified." );
		if ( operation == "get" ) return Describe( binding, stored.Desired, previous );
		if ( !binding.RequiresRestart )
		{
			var status = NativeConsole.Write( binding, desired );
			if ( status != CommandStatus.Success )
				return new CommandResult( status, "Native write was not confirmed; stored desired state was not changed." );
		}
		if ( binding.Persistence == NativePersistence.Framework )
		{
			var intent = new AuditIntent( Guid.NewGuid(), context.RequestId, context.Actor.Kind, context.Actor.SubjectId,
				context.Origin, $"settings.{operation}.{binding.Key}", DateTimeOffset.UtcNow );
			var transaction = new PolicyTransaction( NativeSettingState.Serialize( binding, desired ), new[] { intent } );
			var result = await _runtime.Store.WriteAsync( transaction.CreateWriteRequest( Key( binding ), stored.Revision ), context.Cancellation );
			if ( result.Status != StoreWriteStatus.Committed )
			{
				if ( result.Status == StoreWriteStatus.Unknown || !_runtime.IsUsable )
					return new CommandResult( CommandStatus.Unknown, "Setting persistence was not confirmed; inspect active and stored state before retrying." );
				var restored = binding.RequiresRestart || NativeConsole.Restore( binding, previous );
				return new CommandResult( restored ? CommandStatus.StorageUnavailable : CommandStatus.Unknown,
					"Setting was not persisted; active rollback " + (restored ? "confirmed." : "could not be confirmed.") );
			}
		}
		return Describe( binding, binding.Persistence == NativePersistence.Framework ? desired : null,
			binding.RequiresRestart ? previous : desired, binding.RequiresRestart ? "Desired setting saved for a new runtime." : "Native write confirmed by readback." );
	}

	private static StoreKey Key( NativeSettingDescriptor binding ) => new( "superuser.settings", binding.Key );

	private static CommandResult Describe( NativeSettingDescriptor binding, string desired, string current,
		string message = "Actual native value and separately stored desired state." ) => new( CommandStatus.Success, message, new Dictionary<string, string>
	{
		[ "key" ] = binding.Key,
		[ "active" ] = binding.Sensitive ? "[redacted]" : current,
		[ "desired" ] = desired is null ? "[not stored by framework]" : binding.Sensitive ? "[redacted]" : desired,
		[ "pending" ] = (binding.RequiresRestart && desired is not null && desired != current).ToString(),
		[ "persistence" ] = binding.Persistence.ToString(),
		[ "restart" ] = binding.RequiresRestart.ToString()
	} );
}
