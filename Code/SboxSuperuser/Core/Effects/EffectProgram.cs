namespace SboxSuperuser.Core.Effects;

/// <summary>
/// A checked script, ready to run once per letter. It never throws: a problem while running stops this frame,
/// sets <see cref="LastError"/> and the caller draws the text plain. Not thread-safe; render from one thread.
/// </summary>
internal sealed class EffectProgram : IGlyphEffect
{
	internal const int MaximumSourceLength = 2048;
	internal const int MaximumTextLength = 256;
	internal const int MaximumCharLength = 8;
	private const int StepBudget = 4096;

	private static readonly string[] AsciiLetters = Enumerable.Range( 0, 128 ).Select( code => ((char)code).ToString() ).ToArray();

	private readonly EffectNode[] _statements;
	private readonly EffectValue[] _slots;
	private int _steps;
	private EffectDiagnostic _error;

	internal EffectProgram( string source, EffectNode[] statements, int slotCount, bool usesTime )
	{
		Source = source;
		_statements = statements;
		_slots = new EffectValue[ slotCount ];
		IsAnimated = usesTime;
	}

	internal string Source { get; }

	public bool IsAnimated { get; }

	/// <summary>Why the most recent render stopped, or null when it finished.</summary>
	internal EffectDiagnostic LastError => _error;

	public bool Render( in EffectInput input, List<EffectGlyph> output )
	{
		var text = input.Text ?? "";
		var first = output.Count;
		_error = null;
		_slots[ EffectSlots.Time ] = EffectValue.Of( input.Time );
		_slots[ EffectSlots.Part ] = EffectValue.Of( input.Part ?? "" );
		_slots[ EffectSlots.Text ] = EffectValue.Of( text );
		_slots[ EffectSlots.Palette ] = input.Palette;
		_slots[ EffectSlots.Seed ] = EffectValue.Of( input.Seed );
		_slots[ EffectSlots.Name ] = EffectValue.Of( input.Name ?? "" );
		_slots[ EffectSlots.Prefix ] = EffectValue.Of( input.Prefix ?? "" );

		var letters = CountLetters( text );
		_slots[ EffectSlots.Count ] = EffectValue.Of( letters );
		var position = 0;
		for ( var index = 0; index < letters; index++ )
		{
			var letter = NextLetter( text, ref position );
			StartLetter( index, letter );
			Run( _statements );
			if ( _error is not null )
			{
				output.RemoveRange( first, output.Count - first );
				return false;
			}
			if ( _slots[ EffectSlots.Hidden ].IsTrue ) continue;

			var color = _slots[ EffectSlots.Color ].Color;
			var alpha = Math.Clamp( _slots[ EffectSlots.Alpha ].Number, 0, 1 );
			output.Add( new EffectGlyph( _slots[ EffectSlots.Char ].Text, color.WithAlpha( color.A * alpha ),
				(float)Math.Clamp( _slots[ EffectSlots.Offset ].Number, -64, 64 ), _slots[ EffectSlots.Bold ].IsTrue, _slots[ EffectSlots.Italic ].IsTrue ) );
		}
		return true;
	}

	private void StartLetter( int index, string letter )
	{
		_steps = 0;
		_slots[ EffectSlots.Index ] = EffectValue.Of( index );
		_slots[ EffectSlots.Letter ] = EffectValue.Of( letter );
		_slots[ EffectSlots.Color ] = EffectValue.Of( EffectColor.White );
		_slots[ EffectSlots.Alpha ] = EffectValue.Of( 1 );
		_slots[ EffectSlots.Char ] = EffectValue.Of( letter );
		_slots[ EffectSlots.Offset ] = EffectValue.Zero;
		_slots[ EffectSlots.Bold ] = EffectValue.Of( false );
		_slots[ EffectSlots.Italic ] = EffectValue.Of( false );
		_slots[ EffectSlots.Hidden ] = EffectValue.Of( false );
		for ( var slot = EffectSlots.Fixed; slot < _slots.Length; slot++ ) _slots[ slot ] = EffectValue.Zero;
	}

	// A letter is one character, or a surrogate pair kept together so emoji survive.
	private static int CountLetters( string text )
	{
		var count = 0;
		for ( var position = 0; position < text.Length; count++ )
			position += char.IsHighSurrogate( text[ position ] ) && position + 1 < text.Length && char.IsLowSurrogate( text[ position + 1 ] ) ? 2 : 1;
		return count;
	}

	private static string NextLetter( string text, ref int position )
	{
		var c = text[ position ];
		if ( char.IsHighSurrogate( c ) && position + 1 < text.Length && char.IsLowSurrogate( text[ position + 1 ] ) )
		{
			position += 2;
			return text.Substring( position - 2, 2 );
		}
		position++;
		return c < 128 ? AsciiLetters[ c ] : c.ToString();
	}

	private void Run( EffectNode[] statements )
	{
		foreach ( var statement in statements )
		{
			if ( _error is not null ) return;
			switch ( statement.Kind )
			{
				case EffectNodeKind.Assign:
				case EffectNodeKind.ColorStatement:
					Store( statement, Evaluate( statement.Children[ 0 ] ) );
					break;
				case EffectNodeKind.If:
					Run( Evaluate( statement.Children[ 0 ] ).IsTrue ? statement.Body : statement.Else );
					break;
			}
		}
	}

	private void Store( EffectNode statement, EffectValue value )
	{
		if ( _error is not null ) return;
		var slot = statement.Slot;
		if ( !EffectSlots.IsOutput( slot ) )
		{
			_slots[ slot ] = value;
			return;
		}
		if ( EffectChecker.OutputProblem( slot, EffectType.Of( value.Kind ) ) is { } problem )
		{
			Fail( statement.Children[ 0 ], problem );
			return;
		}
		_slots[ slot ] = slot switch
		{
			EffectSlots.Char => EffectValue.Of( Truncate( value.ToString(), MaximumCharLength ) ),
			EffectSlots.Bold or EffectSlots.Italic or EffectSlots.Hidden => EffectValue.Of( value.IsTrue ),
			EffectSlots.Alpha or EffectSlots.Offset => EffectValue.Of( value.Number ),
			_ => value
		};
	}

	private EffectValue Evaluate( EffectNode node )
	{
		if ( _error is not null ) return EffectValue.Zero;
		if ( ++_steps > StepBudget ) return Fail( node, "this effect does too much work for one letter; simplify it" );
		switch ( node.Kind )
		{
			case EffectNodeKind.Literal:
				return node.Value;
			case EffectNodeKind.Variable:
				return _slots[ node.Slot ];
			case EffectNodeKind.List:
				for ( var index = 0; index < node.Children.Length; index++ ) node.Arguments[ index ] = Evaluate( node.Children[ index ] );
				return EffectValue.Of( node.Arguments );
			case EffectNodeKind.Index:
				return Index( node, Evaluate( node.Children[ 0 ] ), Evaluate( node.Children[ 1 ] ) );
			case EffectNodeKind.Call:
				return Call( node );
			case EffectNodeKind.Unary:
				var operand = Evaluate( node.Children[ 0 ] );
				if ( node.Name == "not" ) return EffectValue.Of( !operand.IsTrue );
				return Numeric( node.Children[ 0 ], operand, "-" ) ? EffectValue.Of( -operand.Number ) : EffectValue.Zero;
			case EffectNodeKind.Binary:
				return Binary( node, Evaluate( node.Children[ 0 ] ), Evaluate( node.Children[ 1 ] ) );
			case EffectNodeKind.And:
				return EffectValue.Of( Evaluate( node.Children[ 0 ] ).IsTrue && Evaluate( node.Children[ 1 ] ).IsTrue );
			case EffectNodeKind.Or:
				return EffectValue.Of( Evaluate( node.Children[ 0 ] ).IsTrue || Evaluate( node.Children[ 1 ] ).IsTrue );
			case EffectNodeKind.Conditional:
				return Evaluate( node.Children[ Evaluate( node.Children[ 0 ] ).IsTrue ? 1 : 2 ] );
			default:
				return Fail( node, "this can't be used as a value" );
		}
	}

	private EffectValue Index( EffectNode node, EffectValue target, EffectValue index )
	{
		if ( !Numeric( node.Children[ 1 ], index, "[ ]" ) ) return EffectValue.Zero;
		switch ( target.Kind )
		{
			case EffectValueKind.List when target.Items.Length == 0:
				return Fail( node, "this list is empty, so nothing can be picked from it" );
			case EffectValueKind.List:
				return target.Items[ EffectFunctions.Wrap( index.Number, target.Items.Length ) ];
			case EffectValueKind.Text:
				return EffectValue.Of( EffectFunctions.Letter( target.Text, index.Number ) );
			default:
				return Fail( node, $"[ ] picks from a list or text, but this is {EffectValue.Describe( target.Kind )}" );
		}
	}

	private EffectValue Call( EffectNode node )
	{
		var function = EffectFunctions.All[ node.Name ];
		for ( var index = 0; index < node.Children.Length; index++ )
		{
			var value = Evaluate( node.Children[ index ] );
			if ( _error is not null ) return EffectValue.Zero;
			var parameter = function.Parameters[ index ];
			if ( !EffectChecker.Accepts( parameter, value.Kind ) )
				return Fail( node.Children[ index ], $"{node.Name} needs {EffectChecker.Describe( parameter )} here, but got {EffectValue.Describe( value.Kind )}" );
			node.Arguments[ index ] = value;
		}
		return function.Body( node.Arguments );
	}

	private EffectValue Binary( EffectNode node, EffectValue left, EffectValue right )
	{
		if ( _error is not null ) return EffectValue.Zero;
		switch ( node.Name )
		{
			case "==":
				return EffectValue.Of( Equal( left, right ) );
			case "!=":
				return EffectValue.Of( !Equal( left, right ) );
			case "+" when left.Kind == EffectValueKind.Text || right.Kind == EffectValueKind.Text:
				var joined = left.ToString() + right.ToString();
				return joined.Length > MaximumTextLength
					? Fail( node, $"text can be at most {MaximumTextLength} letters long" )
					: EffectValue.Of( joined );
		}

		if ( !Numeric( node.Children[ 0 ], left, node.Name ) || !Numeric( node.Children[ 1 ], right, node.Name ) ) return EffectValue.Zero;
		var a = left.Number;
		var b = right.Number;
		return node.Name switch
		{
			"+" => EffectValue.Of( EffectFunctions.Finite( a + b ) ),
			"-" => EffectValue.Of( EffectFunctions.Finite( a - b ) ),
			"*" => EffectValue.Of( EffectFunctions.Finite( a * b ) ),
			"/" => EffectValue.Of( b == 0 ? 0 : EffectFunctions.Finite( a / b ) ),
			"%" => EffectValue.Of( EffectFunctions.Mod( a, b ) ),
			"<" => EffectValue.Of( a < b ),
			"<=" => EffectValue.Of( a <= b ),
			">" => EffectValue.Of( a > b ),
			_ => EffectValue.Of( a >= b )
		};
	}

	private static bool Equal( EffectValue left, EffectValue right )
	{
		var numeric = left.Kind is EffectValueKind.Number or EffectValueKind.Bool && right.Kind is EffectValueKind.Number or EffectValueKind.Bool;
		if ( numeric ) return left.Number == right.Number;
		if ( left.Kind != right.Kind ) return false;
		return left.Kind switch
		{
			EffectValueKind.Text => left.Text == right.Text,
			EffectValueKind.Color => left.Color == right.Color,
			_ => ReferenceEquals( left.Items, right.Items )
		};
	}

	private bool Numeric( EffectNode node, EffectValue value, string operation )
	{
		if ( value.Kind is EffectValueKind.Number or EffectValueKind.Bool ) return true;
		Fail( node, $"{operation} works on numbers, but this is {EffectValue.Describe( value.Kind )}" );
		return false;
	}

	private EffectValue Fail( EffectNode node, string message )
	{
		_error ??= new EffectDiagnostic( message, node.Start, Math.Max( 1, node.Length ) );
		return EffectValue.Zero;
	}

	private static string Truncate( string text, int length ) => text.Length <= length ? text : text.Substring( 0, length );
}
