namespace SboxSuperuser;

/// <summary>Explicit-registration handler shape; registration and execution are not supplied by T02.</summary>
public delegate Task<CommandResult> CommandHandler( CommandContext context, CommandArguments arguments );
