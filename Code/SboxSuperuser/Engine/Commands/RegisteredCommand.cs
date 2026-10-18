namespace SboxSuperuser.Engine.Commands;

internal sealed record RegisteredCommand( CommandDescriptor Descriptor, CommandHandler Handler, Guid RegistrationId );
