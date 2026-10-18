using SboxSuperuser.Core.Modules;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ModuleGraphTests
{
	private static ModuleNode Node( string id, params ModuleRequirement[] dependencies ) => new( id, 1, dependencies );

	[Fact]
	public void DependenciesPrecedeConsumersAndMissingOptionalIsAllowed()
	{
		var nodes = new[] { Node( "consumer", new( "base", 1, false ), new( "absent", 1, true ) ), Node( "base" ) };
		Assert.Equal( new[] { "base", "consumer" }, ModuleGraph.Order( nodes ) );
	}

	[Fact]
	public void DuplicateIdsAndDuplicateRequirementsReject()
	{
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a" ), Node( "a" ) } ) );
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a", new( "b", 1, true ), new( "b", 1, true ) ) } ) );
	}

	[Fact]
	public void RequiredAbsenceAndInstalledOptionalVersionMismatchReject()
	{
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a", new ModuleRequirement( "b", 1, false ) ) } ) );
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a", new ModuleRequirement( "b", 2, true ) ), Node( "b" ) } ) );
	}

	[Fact]
	public void OptionalCyclesAndSelfReferencesReject()
	{
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a", new ModuleRequirement( "a", 1, true ) ) } ) );
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( new[] { Node( "a", new ModuleRequirement( "b", 1, true ) ), Node( "b", new ModuleRequirement( "a", 1, false ) ) } ) );
	}

	[Fact]
	public void CatalogHasAFixedCapacity()
	{
		Assert.Throws<ArgumentException>( () => ModuleGraph.Order( Enumerable.Range( 0, 129 ).Select( i => Node( "module" + i ) ).ToArray() ) );
	}
}
