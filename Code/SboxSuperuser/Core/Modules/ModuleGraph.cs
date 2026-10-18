namespace SboxSuperuser.Core.Modules;

internal static class ModuleGraph
{
	internal static IReadOnlyList<string> Order( IReadOnlyList<ModuleNode> nodes )
	{
		if ( nodes.Count > 128 )
		{
			throw new ArgumentException( "At most 128 modules are supported." );
		}

		var catalog = new Dictionary<string, ModuleNode>( StringComparer.Ordinal );
		foreach ( var node in nodes )
		{
			if ( !catalog.TryAdd( node.Id, node ) )
			{
				throw new ArgumentException( $"Duplicate module '{node.Id}'." );
			}
		}

		var visiting = new HashSet<string>( StringComparer.Ordinal );
		var visited = new HashSet<string>( StringComparer.Ordinal );
		var order = new List<string>();
		foreach ( var node in nodes )
		{
			Visit( node, catalog, visiting, visited, order );
		}
		return order.AsReadOnly();
	}

	private static void Visit( ModuleNode node, Dictionary<string, ModuleNode> catalog,
		HashSet<string> visiting, HashSet<string> visited, List<string> order )
	{
		if ( visited.Contains( node.Id ) )
		{
			return;
		}
		if ( !visiting.Add( node.Id ) )
		{
			throw new ArgumentException( $"Module dependency cycle at '{node.Id}'." );
		}

		var seen = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var dependency in node.Dependencies )
		{
			if ( !seen.Add( dependency.Id ) )
			{
				throw new ArgumentException( $"Module '{node.Id}' repeats dependency '{dependency.Id}'." );
			}
			if ( !catalog.TryGetValue( dependency.Id, out var target ) )
			{
				if ( dependency.Optional )
				{
					continue;
				}
				throw new ArgumentException( $"Module '{node.Id}' requires missing module '{dependency.Id}'." );
			}
			if ( target.Version != dependency.Version )
			{
				throw new ArgumentException( $"Module '{node.Id}' requires API {dependency.Version} of '{dependency.Id}', found {target.Version}." );
			}
			Visit( target, catalog, visiting, visited, order );
		}

		visiting.Remove( node.Id );
		visited.Add( node.Id );
		order.Add( node.Id );
	}
}
