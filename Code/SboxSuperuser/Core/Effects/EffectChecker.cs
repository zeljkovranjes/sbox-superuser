namespace SboxSuperuser.Core.Effects;

/// <summary>
/// Checks names, argument counts and value kinds before a script runs, and gives every variable its slot. Kinds it
/// can't know yet (a variable set to different kinds in different branches) are checked again while running.
/// </summary>
internal sealed class EffectChecker
{
	private const int MaximumListItems = 64;

	private readonly List<EffectDiagnostic> _diagnostics;
	private readonly Dictionary<string, int> _slots = new( StringComparer.Ordinal );
	private readonly List<EffectType> _types = new();

	private EffectChecker( List<EffectDiagnostic> diagnostics )
	{
		_diagnostics = diagnostics;
		for ( var slot = 0; slot < EffectSlots.Fixed; slot++ )
		{
			_slots.Add( EffectSlots.Names[ slot ], slot );
			_types.Add( EffectSlots.TypeOf( slot ) );
		}
	}

	internal bool UsesTime { get; private set; }

	internal int SlotCount => _types.Count;

	internal static EffectChecker Check( EffectNode[] statements, List<EffectDiagnostic> diagnostics )
	{
		var checker = new EffectChecker( diagnostics );
		checker.Statements( statements );
		return checker;
	}

	private void Statements( EffectNode[] statements )
	{
		foreach ( var statement in statements ) Statement( statement );
	}

	private void Statement( EffectNode node )
	{
		switch ( node.Kind )
		{
			case EffectNodeKind.Assign:
				Assign( node );
				break;
			case EffectNodeKind.ColorStatement:
				var type = Expression( node.Children[ 0 ] );
				if ( type.Kind is { } kind && kind != EffectValueKind.Color )
					Error( node, $"a line on its own sets the color, but this is {EffectValue.Describe( kind )}; write  name = ...  to keep it in a variable" );
				node.Slot = EffectSlots.Color;
				break;
			case EffectNodeKind.If:
				Expression( node.Children[ 0 ] );
				Statements( node.Body );
				Statements( node.Else );
				break;
		}
	}

	private void Assign( EffectNode node )
	{
		var type = Expression( node.Children[ 0 ] );
		var name = node.Name;
		if ( EffectFunctions.All.ContainsKey( name ) ) { Error( node, $"{name} is a built-in function; pick another name" ); return; }
		if ( EffectSymbols.IsColor( name ) ) { Error( node, $"{name} is a built-in color; pick another name" ); return; }

		if ( !_slots.TryGetValue( name, out var slot ) )
		{
			slot = _types.Count;
			_slots.Add( name, slot );
			_types.Add( type );
		}
		else if ( EffectSlots.IsReadOnly( slot ) )
		{
			Error( node, $"{name} is an input and can't be changed; store the value under a new name instead" );
			return;
		}
		else if ( EffectSlots.IsOutput( slot ) )
		{
			if ( OutputProblem( slot, type ) is { } problem ) Error( node.Children[ 0 ], problem );
		}
		else
		{
			_types[ slot ] = _types[ slot ].Merge( type );
		}
		node.Slot = slot;
	}

	/// <summary>Why a value can't go into an output, or null when it can (or can't be known yet).</summary>
	internal static string OutputProblem( int slot, EffectType type )
	{
		if ( type.Kind is not { } kind ) return null;
		var accepted = slot switch
		{
			EffectSlots.Color => kind == EffectValueKind.Color,
			EffectSlots.Alpha or EffectSlots.Offset => type.IsNumeric,
			EffectSlots.Char => kind is EffectValueKind.Text or EffectValueKind.Number,
			_ => true
		};
		if ( accepted ) return null;
		var wanted = slot switch
		{
			EffectSlots.Color => "a color, like red, #ff8800 or rgb(255, 136, 0)",
			EffectSlots.Char => "text, like \"o\"",
			_ => "a number"
		};
		return $"{EffectSlots.Names[ slot ]} needs {wanted}, but this is {EffectValue.Describe( kind )}";
	}

	private EffectType Expression( EffectNode node )
	{
		switch ( node.Kind )
		{
			case EffectNodeKind.Literal:
				return EffectType.Of( node.Value.Kind );
			case EffectNodeKind.Variable:
				return Variable( node );
			case EffectNodeKind.List:
				return List( node );
			case EffectNodeKind.Index:
				return Index( node );
			case EffectNodeKind.Call:
				return Call( node );
			case EffectNodeKind.Unary:
				var operand = Expression( node.Children[ 0 ] );
				if ( node.Name == "not" ) return EffectType.Of( EffectValueKind.Bool );
				RequireNumber( node, operand, "-" );
				return EffectType.Of( EffectValueKind.Number );
			case EffectNodeKind.Binary:
				return Binary( node, Expression( node.Children[ 0 ] ), Expression( node.Children[ 1 ] ) );
			case EffectNodeKind.And:
			case EffectNodeKind.Or:
				Expression( node.Children[ 0 ] );
				Expression( node.Children[ 1 ] );
				return EffectType.Of( EffectValueKind.Bool );
			case EffectNodeKind.Conditional:
				Expression( node.Children[ 0 ] );
				return Expression( node.Children[ 1 ] ).Merge( Expression( node.Children[ 2 ] ) );
			default:
				return EffectType.Unknown;
		}
	}

	private EffectType Variable( EffectNode node )
	{
		if ( _slots.TryGetValue( node.Name, out var slot ) )
		{
			node.Slot = slot;
			if ( slot == EffectSlots.Time ) UsesTime = true;
			return _types[ slot ];
		}
		var suggestion = EffectSymbols.Suggest( node.Name, _slots.Keys.Concat( EffectSymbols.Colors.Keys ) );
		Error( node, suggestion is null
			? $"\"{node.Name}\" isn't defined yet; set it first, like  {node.Name} = 0"
			: $"\"{node.Name}\" isn't defined; did you mean {suggestion}?" );
		return EffectType.Unknown;
	}

	private EffectType List( EffectNode node )
	{
		if ( node.Children.Length > MaximumListItems ) Error( node, $"a list can hold at most {MaximumListItems} values" );
		var item = node.Children.Length == 0 ? EffectType.Unknown : Expression( node.Children[ 0 ] );
		foreach ( var child in node.Children.Skip( 1 ) ) item = item.Merge( Expression( child ) );
		node.Arguments = new EffectValue[ node.Children.Length ];
		return new EffectType( EffectValueKind.List, item.Kind );
	}

	private EffectType Index( EffectNode node )
	{
		var target = Expression( node.Children[ 0 ] );
		RequireNumber( node.Children[ 1 ], Expression( node.Children[ 1 ] ), "[ ]" );
		switch ( target.Kind )
		{
			case EffectValueKind.List:
				return new EffectType( target.Item );
			case EffectValueKind.Text:
				return EffectType.Of( EffectValueKind.Text );
			case null:
				return EffectType.Unknown;
			default:
				Error( node, $"[ ] picks from a list or text, but this is {EffectValue.Describe( target.Kind.Value )}" );
				return EffectType.Unknown;
		}
	}

	private EffectType Call( EffectNode node )
	{
		var arguments = node.Children.Select( Expression ).ToArray();
		if ( !EffectFunctions.All.TryGetValue( node.Name, out var function ) )
		{
			var suggestion = EffectSymbols.Suggest( node.Name, EffectSymbols.FunctionNames );
			Error( node, $"there is no function called {node.Name}" + (suggestion is null ? "" : $"; did you mean {suggestion}?") );
			return EffectType.Unknown;
		}

		var symbol = EffectSymbols.Find( node.Name );
		if ( arguments.Length < symbol.MinArguments || arguments.Length > symbol.MaxArguments )
		{
			var expected = symbol.MinArguments == symbol.MaxArguments ? $"{symbol.MinArguments}" : $"{symbol.MinArguments} or {symbol.MaxArguments}";
			Error( node, $"{node.Name} takes {expected} value{(symbol.MaxArguments == 1 ? "" : "s")}, but got {arguments.Length}; for example  {symbol.Example}" );
			return EffectType.Of( function.Returns );
		}

		for ( var index = 0; index < arguments.Length; index++ )
		{
			var parameter = function.Parameters[ index ];
			if ( arguments[ index ].Kind is { } kind && !Accepts( parameter, kind ) )
				Error( node.Children[ index ], $"{node.Name} needs {Describe( parameter )} here, but this is {EffectValue.Describe( kind )}" );
		}
		node.Arguments = new EffectValue[ arguments.Length ];
		return EffectType.Of( function.Returns );
	}

	private EffectType Binary( EffectNode node, EffectType left, EffectType right )
	{
		switch ( node.Name )
		{
			case "==":
			case "!=":
				return EffectType.Of( EffectValueKind.Bool );
			case "+" when left.Kind == EffectValueKind.Text || right.Kind == EffectValueKind.Text:
				return EffectType.Of( EffectValueKind.Text );
			case "+" when left.Kind == EffectValueKind.Color || right.Kind == EffectValueKind.Color:
				Error( node, "colors can't be added; use mix(first, second, amount) to blend them" );
				return EffectType.Unknown;
			case "+" when left.Kind is null || right.Kind is null:
				return EffectType.Unknown;
		}

		RequireNumber( node.Children[ 0 ], left, node.Name );
		RequireNumber( node.Children[ 1 ], right, node.Name );
		return EffectType.Of( node.Name is "<" or "<=" or ">" or ">=" ? EffectValueKind.Bool : EffectValueKind.Number );
	}

	private void RequireNumber( EffectNode node, EffectType type, string operation )
	{
		if ( type.Kind is { } kind && !type.IsNumeric ) Error( node, $"{operation} works on numbers, but this is {EffectValue.Describe( kind )}" );
	}

	internal static bool Accepts( char parameter, EffectValueKind kind ) => parameter switch
	{
		'n' => kind is EffectValueKind.Number or EffectValueKind.Bool,
		'c' => kind == EffectValueKind.Color,
		't' => kind == EffectValueKind.Text,
		's' => kind is EffectValueKind.Text or EffectValueKind.List,
		_ => true
	};

	internal static string Describe( char parameter ) => parameter switch
	{
		'n' => "a number",
		'c' => "a color",
		't' => "text",
		_ => "text or a list"
	};

	private void Error( EffectNode node, string message ) => _diagnostics.Add( new EffectDiagnostic( message, node.Start, Math.Max( 1, node.Length ) ) );
}
