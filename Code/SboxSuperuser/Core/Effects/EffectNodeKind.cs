namespace SboxSuperuser.Core.Effects;

internal enum EffectNodeKind
{
	Literal,
	Variable,
	List,
	Index,
	Call,
	Unary,
	Binary,
	And,
	Or,
	Conditional,
	Assign,
	ColorStatement,
	If
}
