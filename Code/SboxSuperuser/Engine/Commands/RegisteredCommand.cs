namespace SboxSuperuser.Engine.Commands;

internal sealed record RegisteredCommand( CommandDescriptor Descriptor, CommandHandler Handler, Guid RegistrationId )
{
	internal CommandCapabilities Capabilities { get; init; } = CommandCapabilities.Empty;
	internal bool RecordsAudit { get; init; } = true;
}
