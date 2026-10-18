namespace SboxSuperuser.Core.Commands;

internal sealed record PlayerResolution( string SteamId, IReadOnlyList<PlayerCandidate> Choices, string Error );
