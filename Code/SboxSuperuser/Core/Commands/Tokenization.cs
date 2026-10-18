namespace SboxSuperuser.Core.Commands;

internal sealed record Tokenization( IReadOnlyList<string> Tokens, string Error );
