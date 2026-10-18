namespace SboxSuperuser.Engine.Commands;

internal sealed class CommandCapabilities : ICommandServices
{
	private readonly IReadOnlyDictionary<Type, object> _values;
	internal static CommandCapabilities Empty { get; } = new( new Dictionary<Type, object>() );

	internal CommandCapabilities( IReadOnlyDictionary<Type, object> values )
	{
		_values = new Dictionary<Type, object>( values );
	}

	public T GetService<T>() where T : class => _values.TryGetValue( typeof( T ), out var value ) ? (T)value : null;
}
