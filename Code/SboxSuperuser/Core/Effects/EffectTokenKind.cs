namespace SboxSuperuser.Core.Effects;

internal enum EffectTokenKind
{
	Number,
	Text,
	Color,
	Identifier,
	Keyword,
	Placeholder,
	Operator,
	Comment,
	Invalid,
	End
}
