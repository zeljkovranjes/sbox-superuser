namespace SboxSuperuser.Engine.Bootstrap;

/// <summary>Which shipped modules the bootstrap installs, and the facade's guide page for the Prefix module.</summary>
internal sealed record BootstrapModules( bool Prefixes, bool PrefixedChat, string PrefixGuide );
