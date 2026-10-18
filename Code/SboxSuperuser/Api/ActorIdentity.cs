using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser;

/// <summary>A host-issued principal description, never a client-supplied authorization credential.</summary>
public sealed class ActorIdentity
{
	/// <summary>Gets the verified principal category.</summary>
	public ActorKind Kind { get; }

	/// <summary>Gets the decimal Steam ID, local console ID, or stable service key.</summary>
	public string SubjectId { get; }

	internal ActorIdentity( ActorKind kind, string subjectId )
	{
		Kind = ContractGuard.EnumValue( kind, nameof( kind ) );
		SubjectId = kind switch
		{
			ActorKind.Player => SteamIdentity.Validate( subjectId, nameof( subjectId ) ),
			ActorKind.DedicatedConsole when subjectId == "dedicated-console" => subjectId,
			ActorKind.InternalService => ContractGuard.Key( subjectId, nameof( subjectId ) ),
			_ => throw new ArgumentException( "An explicit authenticated principal is required.", nameof( kind ) )
		};
	}
}
