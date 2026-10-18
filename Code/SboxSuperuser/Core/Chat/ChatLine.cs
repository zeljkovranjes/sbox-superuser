namespace SboxSuperuser.Core.Chat;

/// <summary>One delivered chat message as moderators see it.</summary>
internal sealed record ChatLine( string SteamId, string Name, string Message, DateTimeOffset CreatedUtc );
