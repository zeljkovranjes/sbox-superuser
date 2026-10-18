namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	private void RegisterModules()
	{
		if ( _modules is null )
		{
			return;
		}
		foreach ( var enabled in new[] { true, false } )
		{
			var operation = enabled ? "enable" : "disable";
			Register( "modules." + operation, $"{operation} an installed module. Example: admin modules {operation} sample.counter",
				"admin.security.manage", new[] { Text( "module", "Exact installed module ID" ) },
				( context, arguments ) => _modules.SetEnabledAsync( () => Authenticate( context ), arguments.Get<string>( "module" ),
					enabled, context.Cancellation ), mutation: true, lifecycleControl: true );
		}
	}

	private IEnumerable<string> ModuleRows()
	{
		var statuses = _modules?.Statuses ?? Array.Empty<ModuleStatus>();
		return statuses.Select( status => $"{status.Descriptor.Id} API {status.Descriptor.ApiVersion}: {status.State}; required={status.Descriptor.IsRequiredSecurityModule}; {status.Message}" )
			.Concat( _registry.Descriptors.Select( item => item.ModuleId ).Distinct().Where( id => !statuses.Any( status => status.Descriptor.Id == id ) )
				.Select( id => id + ": direct registration; lifetime owned by adopting code" ) ).Order( StringComparer.Ordinal );
	}
}
