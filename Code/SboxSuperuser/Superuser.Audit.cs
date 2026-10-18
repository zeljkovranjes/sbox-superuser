using Sandbox;

namespace SboxSuperuser;

public static partial class Superuser
{
	/// <summary>Reads bounded audit pages; admin.audit.read allows all records and admin.results.own allows only the caller's.</summary>
	public static async Task<AuditPage> QueryAuditAsync( Connection caller, AuditQuery query, CancellationToken cancellation = default )
	{
		ArgumentNullException.ThrowIfNull( query );
		var runtime = FindAuthorityRuntime();
		if ( runtime is null )
		{
			return new AuditPage( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
		}

		await runtime.Policy.Initialization;
		return await runtime.Audit.QueryAsync( () => runtime.TryResolvePlayer( caller, out var actor ) ? actor : null,
			actor => runtime.Policy.Explain( actor, "admin.audit.read" ).IsAllowed,
			actor => runtime.Policy.Explain( actor, "admin.results.own" ).IsAllowed, query, cancellation );
	}

	/// <summary>Gets audit storage health from trusted host code on the main thread; returns null without an eligible runtime.</summary>
	public static AuditDiagnostics GetAuditDiagnostics() => FindAuthorityRuntime()?.Audit.Diagnostics();

	/// <summary>Changes durable audit count and age retention; only a currently authenticated owner may configure it.</summary>
	public static async Task<CommandResult> ConfigureAuditRetentionAsync( Connection caller, AuditRetentionOptions options,
		CancellationToken cancellation = default )
	{
		ArgumentNullException.ThrowIfNull( options );
		var runtime = FindAuthorityRuntime();
		if ( runtime is null )
		{
			return new CommandResult( CommandStatus.Unsupported, "An eligible authoritative runtime is required." );
		}

		await runtime.Policy.Initialization;
		return await runtime.Audit.ConfigureAsync( () => runtime.TryResolvePlayer( caller, out var actor )
			&& runtime.Policy.IsOwner( actor.SubjectId ) ? actor : null, options, cancellation );
	}
}
