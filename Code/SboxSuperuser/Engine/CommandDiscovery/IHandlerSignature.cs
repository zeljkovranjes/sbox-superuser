using Sandbox;

namespace SboxSuperuser.Engine.CommandDiscovery;

internal interface IHandlerSignature
{
	void Validate( MethodDescription method );
}
