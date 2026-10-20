using SboxSuperuser.Core.Effects;
using SboxSuperuser.Core.Prefixes;

namespace SboxSuperuser.Engine.Prefixes;

/// <summary>
/// Turns prefix styles into runnable effects: compiles scripts once per distinct source and keeps the C# effects
/// game code registered. One per machine, owned by the prefix system.
/// </summary>
internal sealed class EffectLibrary
{
	// ponytail: drops the whole cache past this many scripts; an LRU only matters if servers churn effects.
	private const int MaximumCompiled = 128;

	private readonly Dictionary<string, EffectCompilation> _compiled = new( StringComparer.Ordinal );
	private readonly Dictionary<string, TagEffectAdapter> _code = new( StringComparer.Ordinal );
	private readonly Dictionary<string, ITagEffect> _registered = new( StringComparer.Ordinal );

	internal IReadOnlyCollection<ITagEffect> CodeEffects => _registered.Values;

	internal void Register( ITagEffect effect )
	{
		ArgumentNullException.ThrowIfNull( effect );
		Core.Contracts.ContractGuard.Key( effect.Id, nameof( effect ) );
		if ( effect.Id.Length > PrefixStyle.MaximumIdLength ) throw new ArgumentException( "Tag effect IDs can be at most 64 characters.", nameof( effect ) );
		_registered[ effect.Id ] = effect;
		_code[ effect.Id ] = new TagEffectAdapter( effect );
	}

	internal EffectCompilation Compile( string source )
	{
		source ??= "";
		if ( _compiled.TryGetValue( source, out var compiled ) ) return compiled;
		if ( _compiled.Count >= MaximumCompiled ) _compiled.Clear();
		compiled = EffectCompiler.Compile( source );
		_compiled[ source ] = compiled;
		return compiled;
	}

	/// <summary>The effect for a style, or null when it can't run here (a broken script, an unregistered C# effect).</summary>
	internal IGlyphEffect Resolve( PrefixStyle style ) => style.Kind == PrefixStyleKind.Code
		? _code.GetValueOrDefault( style.Value )
		: Compile( style.Source ).Program;
}
