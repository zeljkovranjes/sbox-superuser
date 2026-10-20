using System.Globalization;

namespace SboxSuperuser.Core.Effects;

/// <summary>
/// Turns tokens into statements. Statements go one per line (or are separated by ;). On a mistake it records one
/// diagnostic, skips to the next line and keeps going, so the editor can show every error at once.
/// </summary>
internal sealed class EffectParser
{
	private const int MaximumDepth = 48;
	private const int MaximumDiagnostics = 16;

	private readonly string _source;
	private readonly IReadOnlyList<EffectToken> _tokens;
	private readonly List<EffectDiagnostic> _diagnostics;
	private int _position;
	private int _depth;
	private bool _failed;

	private EffectParser( string source, IReadOnlyList<EffectToken> tokens, List<EffectDiagnostic> diagnostics )
	{
		_source = source;
		_tokens = tokens.Where( token => token.Kind != EffectTokenKind.Comment ).ToArray();
		_diagnostics = diagnostics;
	}

	internal static EffectNode[] Parse( string source, IReadOnlyList<EffectToken> tokens, List<EffectDiagnostic> diagnostics )
		=> new EffectParser( source, tokens, diagnostics ).ParseStatements( insideBlock: false );

	private EffectToken Current => _tokens[ _position ];

	private EffectToken Previous => _tokens[ Math.Max( 0, _position - 1 ) ];

	private EffectNode[] ParseStatements( bool insideBlock )
	{
		var statements = new List<EffectNode>();
		while ( Current.Kind != EffectTokenKind.End && !(insideBlock && Current.Is( "}" )) )
		{
			if ( Current.Is( ";" ) )
			{
				_position++;
				continue;
			}

			var begin = _position;
			_failed = false;
			var statement = ParseStatement();
			if ( !_failed ) RequireStatementEnd();
			if ( _failed ) Synchronize( begin );
			else statements.Add( statement );
			if ( _diagnostics.Count >= MaximumDiagnostics ) break;
		}

		// Errors inside a block were reported and skipped there; the statement holding the block is still whole.
		_failed = false;
		return statements.ToArray();
	}

	private EffectNode ParseStatement()
	{
		var start = Current;
		if ( start.Is( "if" ) ) return ParseIf();
		if ( start.Is( "else" ) ) return Fail( start, "this else has no if before it" );
		if ( start.Kind == EffectTokenKind.Identifier && _tokens[ _position + 1 ].Is( "=" ) )
		{
			var equals = _tokens[ _position + 1 ];
			_position += 2;
			if ( Current.Kind == EffectTokenKind.End || !SameLine() ) return Fail( equals, $"a value is missing after {start.Text} =" );
			var value = ParseExpression();
			return value is null ? null : new EffectNode
			{
				Kind = EffectNodeKind.Assign, Name = start.Text, Children = new[] { value }, Start = start.Start, Length = start.Length
			};
		}

		var expression = ParseExpression();
		return expression is null ? null : new EffectNode
		{
			Kind = EffectNodeKind.ColorStatement, Children = new[] { expression }, Start = expression.Start, Length = expression.Length
		};
	}

	private EffectNode ParseIf()
	{
		var keyword = Current;
		_position++;
		var condition = ParseExpression();
		if ( condition is null ) return null;
		var body = ParseBlock( "if" );
		if ( body is null ) return null;

		var otherwise = Array.Empty<EffectNode>();
		if ( Current.Is( "else" ) )
		{
			_position++;
			if ( Current.Is( "if" ) )
			{
				var nested = ParseIf();
				if ( nested is null ) return null;
				otherwise = new[] { nested };
			}
			else
			{
				otherwise = ParseBlock( "else" );
				if ( otherwise is null ) return null;
			}
		}

		return new EffectNode
		{
			Kind = EffectNodeKind.If, Children = new[] { condition }, Body = body, Else = otherwise, Start = keyword.Start, Length = keyword.Length
		};
	}

	private EffectNode[] ParseBlock( string owner )
	{
		if ( !Current.Is( "{" ) )
		{
			Fail( Current.Kind == EffectTokenKind.End ? Previous : Current, $"put the {owner} lines inside {{ and }}" );
			return null;
		}
		var open = Current;
		_position++;
		if ( !Enter( open ) ) return null;
		var statements = ParseStatements( insideBlock: true );
		_depth--;
		if ( !Current.Is( "}" ) )
		{
			Fail( open, "this { is never closed; add a } after its lines" );
			return null;
		}
		_position++;
		return statements;
	}

	private EffectNode ParseExpression()
	{
		if ( !Enter( Current ) ) return null;
		var value = ParseConditional();
		_depth--;
		return value;
	}

	private EffectNode ParseConditional()
	{
		var condition = ParseOr();
		if ( condition is null || !Current.Is( "?" ) ) return condition;
		_position++;
		var whenTrue = ParseExpression();
		if ( whenTrue is null ) return null;
		if ( !Current.Is( ":" ) ) return Fail( Current, "a ? needs a : with the value to use otherwise, like  i == 0 ? red : blue" );
		_position++;
		var whenFalse = ParseExpression();
		return whenFalse is null ? null : Node( EffectNodeKind.Conditional, "?", condition, condition, whenTrue, whenFalse );
	}

	private EffectNode ParseOr()
	{
		var left = ParseAnd();
		while ( left is not null && (Current.Is( "or" ) || Current.Is( "||" )) )
		{
			_position++;
			var right = ParseAnd();
			left = right is null ? null : Node( EffectNodeKind.Or, "or", left, left, right );
		}
		return left;
	}

	private EffectNode ParseAnd()
	{
		var left = ParseNot();
		while ( left is not null && (Current.Is( "and" ) || Current.Is( "&&" )) )
		{
			_position++;
			var right = ParseNot();
			left = right is null ? null : Node( EffectNodeKind.And, "and", left, left, right );
		}
		return left;
	}

	private EffectNode ParseNot()
	{
		if ( !Current.Is( "not" ) && !Current.Is( "!" ) ) return ParseComparison();
		var keyword = Current;
		_position++;
		if ( !Enter( keyword ) ) return null;
		var operand = ParseNot();
		_depth--;
		return operand is null ? null : Node( EffectNodeKind.Unary, "not", keyword, operand );
	}

	private EffectNode ParseComparison()
	{
		var left = ParseAdditive();
		if ( left is null || Current.Kind != EffectTokenKind.Operator || Current.Text is not ("==" or "!=" or "<" or "<=" or ">" or ">=") ) return left;
		var op = Current;
		_position++;
		var right = ParseAdditive();
		return right is null ? null : Node( EffectNodeKind.Binary, op.Text, op, left, right );
	}

	private EffectNode ParseAdditive()
	{
		var left = ParseMultiplicative();
		while ( left is not null && (Current.Is( "+" ) || Current.Is( "-" )) )
		{
			var op = Current;
			_position++;
			var right = ParseMultiplicative();
			left = right is null ? null : Node( EffectNodeKind.Binary, op.Text, op, left, right );
		}
		return left;
	}

	private EffectNode ParseMultiplicative()
	{
		var left = ParseUnary();
		while ( left is not null && (Current.Is( "*" ) || Current.Is( "/" ) || Current.Is( "%" )) )
		{
			var op = Current;
			_position++;
			var right = ParseUnary();
			left = right is null ? null : Node( EffectNodeKind.Binary, op.Text, op, left, right );
		}
		return left;
	}

	private EffectNode ParseUnary()
	{
		if ( !Current.Is( "-" ) ) return ParsePostfix();
		var op = Current;
		_position++;
		if ( !Enter( op ) ) return null;
		var operand = ParseUnary();
		_depth--;
		return operand is null ? null : Node( EffectNodeKind.Unary, "-", op, operand );
	}

	private EffectNode ParsePostfix()
	{
		var value = ParsePrimary();
		while ( value is not null && Current.Is( "[" ) && SameLine() )
		{
			var open = Current;
			_position++;
			var index = ParseExpression();
			if ( index is null ) return null;
			if ( !Current.Is( "]" ) ) return Fail( open, "this [ is never closed; add a ]" );
			_position++;
			value = Node( EffectNodeKind.Index, "[]", open, value, index );
		}
		return value;
	}

	private EffectNode ParsePrimary()
	{
		var token = Current;
		switch ( token.Kind )
		{
			case EffectTokenKind.Number:
				_position++;
				if ( !double.TryParse( token.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number ) )
					return Fail( token, $"\"{token.Text}\" is not a number" );
				return Literal( token, EffectValue.Of( number ) );
			case EffectTokenKind.Text:
				_position++;
				return Literal( token, EffectValue.Of( EffectLexer.Unquote( token.Text ) ) );
			case EffectTokenKind.Color:
				_position++;
				EffectColor.TryParseHex( token.Text, out var color );
				return Literal( token, EffectValue.Of( color ) );
			case EffectTokenKind.Placeholder:
				_position++;
				if ( EffectSymbols.Find( token.Text ) is null )
					return Fail( token, $"{token.Text} is not a placeholder; use %name% or %prefix%" );
				return new EffectNode { Kind = EffectNodeKind.Variable, Name = token.Text, Start = token.Start, Length = token.Length };
			case EffectTokenKind.Keyword when token.Text is "true" or "false":
				_position++;
				return Literal( token, EffectValue.Of( token.Text == "true" ) );
			case EffectTokenKind.Identifier:
				_position++;
				if ( Current.Is( "(" ) && SameLine() ) return ParseCall( token );
				if ( EffectSymbols.Colors.TryGetValue( token.Text, out var named ) ) return Literal( token, EffectValue.Of( named ) );
				return new EffectNode { Kind = EffectNodeKind.Variable, Name = token.Text, Start = token.Start, Length = token.Length };
			case EffectTokenKind.Operator when token.Text == "(":
			{
				_position++;
				var inner = ParseExpression();
				if ( inner is null ) return null;
				if ( !Current.Is( ")" ) ) return Fail( token, "this ( is never closed; add a )" );
				_position++;
				return inner;
			}
			case EffectTokenKind.Operator when token.Text == "[":
				_position++;
				var items = ParseItems( "]", token, "this [ is never closed; add a ]" );
				return items is null ? null : new EffectNode { Kind = EffectNodeKind.List, Children = items, Start = token.Start, Length = token.Length };
			case EffectTokenKind.End:
				return Fail( Previous, "something is missing after this" );
			case EffectTokenKind.Invalid:
				// The lexer already explained this token.
				_position++;
				_failed = true;
				return null;
			default:
				return Fail( token, $"\"{token.Text}\" can't start a value here" );
		}
	}

	private EffectNode ParseCall( EffectToken name )
	{
		var open = Current;
		_position++;
		var arguments = ParseItems( ")", open, "this ( is never closed; add a )" );
		return arguments is null ? null : new EffectNode
		{
			Kind = EffectNodeKind.Call, Name = name.Text, Children = arguments, Start = name.Start, Length = name.Length
		};
	}

	private EffectNode[] ParseItems( string close, EffectToken open, string unclosed )
	{
		var items = new List<EffectNode>();
		if ( Current.Is( close ) )
		{
			_position++;
			return items.ToArray();
		}
		while ( true )
		{
			var item = ParseExpression();
			if ( item is null ) return null;
			items.Add( item );
			if ( Current.Is( "," ) )
			{
				_position++;
				continue;
			}
			if ( Current.Is( close ) )
			{
				_position++;
				return items.ToArray();
			}
			var ended = Current.Kind == EffectTokenKind.End;
			Fail( ended ? open : Current, ended ? unclosed : $"expected , or {close} here" );
			return null;
		}
	}

	private void RequireStatementEnd()
	{
		if ( Current.Kind == EffectTokenKind.End || Current.Is( ";" ) || Current.Is( "}" ) || !SameLine() ) return;
		Fail( Current, Current.Is( "=" )
			? "only a name can go before =, like  color = red"
			: "put each line on its own line, or separate them with ;" );
	}

	// Skips the rest of the broken line, stopping at the end of the block it is in.
	private void Synchronize( int begin )
	{
		if ( Current.Is( "}" ) && _depth == 0 )
		{
			_position++;
			return;
		}
		if ( _position == begin && Current.Kind != EffectTokenKind.End ) _position++;
		while ( Current.Kind != EffectTokenKind.End && !Current.Is( "}" ) && !Current.Is( ";" ) && SameLine() ) _position++;
	}

	private bool Enter( EffectToken at )
	{
		if ( ++_depth <= MaximumDepth ) return true;
		_depth--;
		Fail( at, "this is nested too deeply; split it into variables" );
		return false;
	}

	// True when nothing but spaces separates the previous token from the current one.
	private bool SameLine() => _position == 0 || _source.IndexOf( '\n', Previous.End, Math.Max( 0, Current.Start - Previous.End ) ) < 0;

	private EffectNode Fail( EffectToken at, string message )
	{
		_failed = true;
		_diagnostics.Add( new EffectDiagnostic( message, Math.Min( at.Start, _source.Length ), Math.Max( 1, at.Length ) ) );
		return null;
	}

	private static EffectNode Literal( EffectToken token, EffectValue value ) =>
		new() { Kind = EffectNodeKind.Literal, Value = value, Start = token.Start, Length = token.Length };

	private static EffectNode Node( EffectNodeKind kind, string name, EffectNode at, params EffectNode[] children ) =>
		new() { Kind = kind, Name = name, Children = children, Start = at.Start, Length = at.Length };

	private static EffectNode Node( EffectNodeKind kind, string name, EffectToken at, params EffectNode[] children ) =>
		new() { Kind = kind, Name = name, Children = children, Start = at.Start, Length = at.Length };
}
