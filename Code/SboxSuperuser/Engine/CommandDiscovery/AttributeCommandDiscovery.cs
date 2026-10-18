using Sandbox;
using SboxSuperuser.Engine.Commands;

namespace SboxSuperuser.Engine.CommandDiscovery;

internal static class AttributeCommandDiscovery
{
	internal static void Discover( CommandRegistrationScope scope )
	{
		foreach ( var entry in TypeLibrary.GetMethodsWithAttribute<AdminCommandAttribute>( onlyStatic: false ) )
		{
			if ( entry.Attribute.ModuleId != scope.ModuleId )
			{
				continue;
			}

			try
			{
				Register( scope, entry.Method, entry.Attribute );
			}
			catch ( Exception error )
			{
				throw new ArgumentException( $"Module '{scope.ModuleId}', method '{entry.Method.DeclaringType.FullName}.{entry.Method.Name}': {error.Message}", error );
			}
		}
	}

	private static void Register( CommandRegistrationScope scope, MethodDescription method, AdminCommandAttribute attribute )
	{
		var parameters = method.Parameters;
		if ( !method.IsPublic || !method.IsStatic || method.IsSpecialName
			|| method.ReturnType != typeof( CommandResult ) && method.ReturnType != typeof( Task<CommandResult> )
			|| parameters.Length is < 1 or > 9 || parameters[ 0 ].ParameterType != typeof( CommandContext ) )
		{
			throw new ArgumentException( "Expected public static CommandResult or Task<CommandResult>, with CommandContext first and at most eight typed arguments." );
		}

		var metadata = method.Attributes.OfType<AdminArgumentAttribute>().ToDictionary( value => value.Parameter, StringComparer.Ordinal );
		var bindings = new List<HandlerParameter>();
		foreach ( var parameter in parameters.Skip( 1 ) )
		{
			metadata.Remove( parameter.Name, out var details );
			bindings.Add( new HandlerParameter( parameter.ParameterType, parameter.Name, parameter.IsOptional,
				parameter.IsOptional ? parameter.DefaultValue : null, details ) );
		}

		if ( metadata.Count > 0 )
		{
			throw new ArgumentException( "Argument metadata names an unknown parameter or the context parameter." );
		}

		// Delegate binding validates a closed, invocable signature without invoking the handler or inspecting MethodInfo.
		var signature = Signature( parameters.Length );
		var validator = TypeLibrary.GetType( signature )?.CreateGeneric<IHandlerSignature>( parameters.Select( value => value.ParameterType )
			.ToArray() ) ?? throw new NotSupportedException( "TypeLibrary signature binding unavailable; use explicit registration." );
		validator.Validate( method );

		var descriptor = new CommandDescriptor( attribute.Name, attribute.Description, attribute.Permission, attribute.ModuleId,
			bindings.Select( value => value.Descriptor ), attribute.Aliases, attribute.ExecutionPolicy );
		CommandMetadata.Validate( descriptor );
		scope.RegisterCommand( descriptor, async ( context, arguments ) =>
		{
			var values = new object[ bindings.Count + 1 ];
			values[ 0 ] = context;
			for ( var index = 0; index < bindings.Count; index++ )
			{
				values[ index + 1 ] = bindings[ index ].Bind( arguments );
			}

			return method.ReturnType == typeof( CommandResult )
				? method.InvokeWithReturn<CommandResult>( null, values )
				: await method.InvokeWithReturn<Task<CommandResult>>( null, values );
		} );
	}

	private static Type Signature( int count ) => count switch
	{
		1 => typeof( HandlerSignature1<> ),
		2 => typeof( HandlerSignature2<,> ),
		3 => typeof( HandlerSignature3<,,> ),
		4 => typeof( HandlerSignature4<,,,> ),
		5 => typeof( HandlerSignature5<,,,,> ),
		6 => typeof( HandlerSignature6<,,,,,> ),
		7 => typeof( HandlerSignature7<,,,,,,> ),
		8 => typeof( HandlerSignature8<,,,,,,,> ),
		9 => typeof( HandlerSignature9<,,,,,,,,> ),
		_ => throw new ArgumentOutOfRangeException( nameof( count ) )
	};
}
