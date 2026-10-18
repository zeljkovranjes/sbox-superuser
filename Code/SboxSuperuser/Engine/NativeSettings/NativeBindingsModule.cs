using Sandbox;
using SboxSuperuser.Engine.Bootstrap;

namespace SboxSuperuser.Engine.NativeSettings;

internal sealed partial class NativeBindingsModule : IAdminModule
{
	private readonly BootstrapRuntime _runtime;
	private readonly NativeSettingDescriptor[] _settings;
	private readonly NativeCommandBindingDescriptor[] _commands;
	private bool _restored;
	public ModuleDescriptor Descriptor { get; } = new( "superuser.settings", "Explicit host native bindings", 1 );

	internal NativeBindingsModule( BootstrapRuntime runtime, IEnumerable<NativeSettingDescriptor> settings,
		IEnumerable<NativeCommandBindingDescriptor> commands )
	{
		_runtime = runtime;
		ArgumentNullException.ThrowIfNull( settings );
		_settings = settings.Take( 65 ).ToArray();
		_commands = (commands ?? Array.Empty<NativeCommandBindingDescriptor>()).Take( 65 ).ToArray();
		if ( _settings.Length > 64 || _commands.Length > 64 || _settings.Any( value => value is null ) || _commands.Any( value => value is null )
			|| _settings.Select( value => value.Key ).Distinct( StringComparer.Ordinal ).Count() != _settings.Length
			|| _commands.Select( value => value.Command.Name ).Distinct( StringComparer.Ordinal ).Count() != _commands.Length
			|| _settings.Select( value => value.NativeName ).Concat( _commands.Select( value => value.NativeName ) ).Distinct( StringComparer.Ordinal ).Count() != _settings.Length + _commands.Length )
			throw new ArgumentException( "Provide at most 64 settings and 64 commands, with unique keys and native names." );
	}

	public async Task StartAsync( IModuleRegistration registration, CancellationToken cancellation )
	{
		cancellation.ThrowIfCancellationRequested();
		var permissions = _settings.Select( item => item.Permission ).Concat( _commands.Select( item => item.Command.Permission ) ).Distinct( StringComparer.Ordinal );
		foreach ( var permission in permissions )
		{
			if ( !_runtime.Policy.RegisteredPermissions.Contains( permission ) )
				registration.RegisterPermission( new PermissionDescriptor( permission, "Use explicitly approved native binding" ) );
		}
		foreach ( var binding in _settings ) RegisterSetting( registration, binding );
		foreach ( var binding in _commands )
			registration.RegisterCommand( binding.Command, (context, arguments) => Task.FromResult( NativeConsole.Run( binding, arguments ) ) );
		registration.RegisterCommand( new CommandDescriptor( "settings.list", "List permitted native bindings; values are read separately", "admin.help", Descriptor.Id,
			new[] { new CommandArgumentDescriptor( "page", "One-based page", CommandArgumentKind.Integer, isOptional: true, minimum: 1, maximum: 1000 ) },
			executionPolicy: CommandExecutionPolicy.ReadOnly ), (context, arguments) => Task.FromResult( List( context, arguments ) ) );
		if ( !_restored )
		{
			_restored = true;
			await RestoreAsync( cancellation );
		}
	}

	private void RegisterSetting( IModuleRegistration registration, NativeSettingDescriptor binding )
	{
		foreach ( var operation in new[] { "get", "set", "reset" } )
		{
			var arguments = operation == "set" ? new[] { binding.Value } : Array.Empty<CommandArgumentDescriptor>();
			var descriptor = new CommandDescriptor( $"settings.{operation}.{binding.Key}",
				$"{binding.Description}. Usage: admin settings {operation} {binding.Key}" + (operation == "set" ? " <value>" : ""),
				binding.Permission, Descriptor.Id, arguments, executionPolicy: operation == "get" ? CommandExecutionPolicy.ReadOnly : CommandExecutionPolicy.Mutation );
			registration.RegisterCommand( descriptor, (context, values) => ExecuteSettingAsync( context, binding, operation,
				operation == "set" ? NativeValue.Format( binding.Value, values ) : binding.DefaultValue ) );
		}
	}

	private CommandResult List( CommandContext context, CommandArguments arguments )
	{
		var page = arguments.Contains( "page" ) ? (int)arguments.Get<long>( "page" ) : 1;
		var rows = _settings.Where( item => _runtime.Dispatcher.CanUse( context, item.Permission ) )
			.OrderBy( item => item.Key, StringComparer.Ordinal ).Skip( (page - 1) * 8 ).Take( 9 ).ToArray();
		var data = rows.Take( 8 ).Select( (item, index) => new KeyValuePair<string, string>( "row" + (index + 1),
			$"{item.Key}: {item.Value.Kind}; scope={item.Scope}; persistence={item.Persistence}; restart={item.RequiresRestart}; sensitive={item.Sensitive}; accessible={NativeConsole.Accessible( item )}" ) )
			.ToDictionary( pair => pair.Key, pair => pair.Value );
		data[ "more" ] = (rows.Length > 8).ToString();
		data[ "page" ] = page.ToString( System.Globalization.CultureInfo.InvariantCulture );
		return new CommandResult( CommandStatus.Success, "Explicit native settings; get reads current engine state.", data );
	}

	private async Task RestoreAsync( CancellationToken cancellation )
	{
		foreach ( var binding in _settings.Where( item => item.Persistence == NativePersistence.Framework ) )
		{
			cancellation.ThrowIfCancellationRequested();
			var stored = await ReadAsync( binding, cancellation );
			if ( !_runtime.IsUsable ) throw new InvalidOperationException( "Native binding runtime retired." );
			if ( stored is null || stored.Desired is not null && NativeConsole.Write( binding, stored.Desired ) != CommandStatus.Success )
				Log.Warning( $"[Superuser] Setting '{binding.Key}' was not restored; stored evidence retained." );
		}
	}

	public Task StopAsync( CancellationToken cancellation ) => Task.CompletedTask;
}
