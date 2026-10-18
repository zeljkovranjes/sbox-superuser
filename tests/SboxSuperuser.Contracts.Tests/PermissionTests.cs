using Xunit;

namespace SboxSuperuser.Contracts.Tests;

public sealed class PermissionTests
{
	private const string User = "76561198000000001";
	private const string Key = "admin.players.kick";
	private static PermissionDescriptor[] Registered => new[] { new PermissionDescriptor( Key, "Kick" ) };
	private static PermissionEntry Entry( PermissionEffect effect ) => new( Key, effect );

	private static PolicySnapshot Snapshot( long revision, PermissionEffect direct, bool allow, bool deny, bool reverse = false )
	{
		var parents = new[] { "allow", "deny" };
		if ( reverse ) Array.Reverse( parents );
		return new PolicySnapshot( revision, new[]
		{
			new GroupPolicy( "player" ),
			new GroupPolicy( "allow", permissions: allow ? new[] { Entry( PermissionEffect.Allow ) } : null ),
			new GroupPolicy( "deny", permissions: deny ? new[] { Entry( PermissionEffect.Deny ) } : null ),
			new GroupPolicy( "staff", parents )
		}, new[] { new UserPolicy( User, new[] { "staff" }, new[] { Entry( direct ) } ) } );
	}

	[Theory]
	[InlineData( PermissionEffect.Unset, false, false, false )]
	[InlineData( PermissionEffect.Unset, true, false, true )]
	[InlineData( PermissionEffect.Unset, false, true, false )]
	[InlineData( PermissionEffect.Unset, true, true, false )]
	[InlineData( PermissionEffect.Allow, false, false, true )]
	[InlineData( PermissionEffect.Allow, true, false, true )]
	[InlineData( PermissionEffect.Allow, false, true, true )]
	[InlineData( PermissionEffect.Allow, true, true, true )]
	[InlineData( PermissionEffect.Deny, false, false, false )]
	[InlineData( PermissionEffect.Deny, true, false, false )]
	[InlineData( PermissionEffect.Deny, false, true, false )]
	[InlineData( PermissionEffect.Deny, true, true, false )]
	public void ConflictRulesAreIndependentOfParentOrdering( PermissionEffect direct, bool allow, bool deny, bool expected )
	{
		foreach ( var reverse in new[] { false, true } )
		{
			var result = new PermissionResolver( Snapshot( 7, direct, allow, deny, reverse ), Registered ).Explain( User, Key );
			Assert.Equal( expected, result.IsAllowed );
			Assert.Equal( direct, result.DirectEntry );
			Assert.Equal( 7, result.PolicyRevision );
			Assert.True( result.IsRegistered );
			Assert.False( result.OwnerBypass );
			Assert.Equal( (allow ? 1 : 0) + (deny ? 1 : 0), result.Groups.Count );
			if ( deny ) Assert.Contains( result.Groups, group => group.GroupId == "deny" && group.Effect == PermissionEffect.Deny );
			Assert.Equal( result.Groups.OrderBy( group => group.GroupId, StringComparer.Ordinal ).Select( group => group.GroupId ),
				result.Groups.Select( group => group.GroupId ) );
		}
	}

	[Fact]
	public void UnknownPermissionDeniesEvenExplicitDirectAllowAndExplainsWhy()
	{
		var resolver = new PermissionResolver( Snapshot( 1, PermissionEffect.Allow, true, false ), Array.Empty<PermissionDescriptor>() );
		var result = resolver.Explain( User, Key );
		Assert.False( result.IsAllowed );
		Assert.False( result.IsRegistered );
		Assert.Contains( "not registered", result.Reason );
		Assert.Equal( PermissionEffect.Allow, result.DirectEntry );
	}

	[Fact]
	public void EveryUserImplicitlyReceivesPlayerIncludingUnknownOfflineUsers()
	{
		var policy = new PolicySnapshot( 0, new[] { new GroupPolicy( "player", permissions: new[] { Entry( PermissionEffect.Allow ) } ) } );
		var result = new PermissionResolver( policy, Registered ).Explain( User, Key );
		Assert.True( result.IsAllowed );
		Assert.Equal( "player", Assert.Single( result.Groups ).GroupId );
	}

	[Fact]
	public void EveryPolicyRevisionInvalidatesDirectMembershipAndInheritedDecisions()
	{
		var resolver = new PermissionResolver( Snapshot( 1, PermissionEffect.Allow, false, true ), Registered );
		var cached = resolver.Resolver.Explain( User, Key );
		Assert.Same( cached, resolver.Resolver.Explain( User, Key ) );
		Assert.True( cached.IsAllowed );
		resolver.ReplacePolicy( Snapshot( 2, PermissionEffect.Unset, false, true ) );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
		Assert.Equal( 2, resolver.Explain( User, Key ).PolicyRevision );
		Assert.NotSame( cached, resolver.Resolver.Explain( User, Key ) );
		resolver.ReplacePolicy( Snapshot( 3, PermissionEffect.Unset, true, false ) );
		Assert.True( resolver.Explain( User, Key ).IsAllowed );
		resolver.ReplacePolicy( new PolicySnapshot( 4, new[] { new GroupPolicy( "player" ) } ) );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
		Assert.Throws<ArgumentException>( () => resolver.ReplacePolicy( Snapshot( 4, PermissionEffect.Allow, true, false ) ) );
		Assert.Throws<ArgumentException>( () => resolver.ReplacePolicy( Snapshot( 1, PermissionEffect.Allow, true, false ) ) );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
	}

	[Fact]
	public void RegistryReplacementInvalidatesBothUnknownAndPreviouslyKnownDecisionsAtomically()
	{
		var resolver = new PermissionResolver( Snapshot( 1, PermissionEffect.Allow, false, false ), Array.Empty<PermissionDescriptor>() );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
		var oldRegistry = resolver.RegistryRevision;
		resolver.ReplaceRegisteredPermissions( Registered );
		Assert.True( resolver.Explain( User, Key ).IsAllowed );
		Assert.True( resolver.RegistryRevision > oldRegistry );
		Assert.Throws<ArgumentException>( () => resolver.ReplaceRegisteredPermissions( Registered.Concat( Registered ) ) );
		Assert.True( resolver.Explain( User, Key ).IsAllowed );
		resolver.ReplaceRegisteredPermissions( Array.Empty<PermissionDescriptor>() );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
	}

	[Theory]
	[InlineData( 1 )]
	[InlineData( 2 )]
	[InlineData( 3 )]
	public void InheritanceCyclesAreRejectedBeforePolicyActivation( int count )
	{
		var groups = new List<GroupPolicy> { new( "player" ) };
		for ( var i = 0; i < count; i++ )
		{
			groups.Add( new GroupPolicy( "group" + i, new[] { "group" + ((i + 1) % count) } ) );
		}
		var error = Assert.Throws<ArgumentException>( () => new PolicySnapshot( 1, groups ) );
		Assert.Contains( "cycle", error.Message );
	}

	[Fact]
	public void MissingGroupsAndDuplicateUsersOrGroupsAreRejected()
	{
		Assert.Throws<ArgumentException>( () => new PolicySnapshot( 0, new[] { new GroupPolicy( "staff" ) } ) );
		Assert.Throws<ArgumentException>( () => new PolicySnapshot( 0, new[] { new GroupPolicy( "player", new[] { "missing" } ) } ) );
		Assert.Throws<ArgumentException>( () => new PolicySnapshot( 0, new[] { new GroupPolicy( "player" ) }, new[] { new UserPolicy( User, new[] { "missing" } ) } ) );
		Assert.Throws<ArgumentException>( () => new PolicySnapshot( 0, new[] { new GroupPolicy( "player" ), new GroupPolicy( "player" ) } ) );
		Assert.Throws<ArgumentException>( () => new PolicySnapshot( 0, new[] { new GroupPolicy( "player" ) }, new[] { new UserPolicy( User ), new UserPolicy( User ) } ) );
	}

	[Fact]
	public void DiamondInheritanceContributesEachGroupOnlyOnce()
	{
		var policy = new PolicySnapshot( 1, new[]
		{
			new GroupPolicy( "player", permissions: new[] { Entry( PermissionEffect.Deny ) } ),
			new GroupPolicy( "left", new[] { "player" } ), new GroupPolicy( "right", new[] { "player" } ),
			new GroupPolicy( "staff", new[] { "left", "right" } )
		}, new[] { new UserPolicy( User, new[] { "staff", "left" } ) } );
		var result = new PermissionResolver( policy, Registered ).Explain( User, Key );
		Assert.False( result.IsAllowed );
		Assert.Single( result.Groups );
	}

	[Fact]
	public void CallerMutationsCannotChangePolicyAndUnsetRemovesTheDirectEntry()
	{
		var parents = new[] { "player" };
		var entries = new[] { Entry( PermissionEffect.Allow ) };
		var assignments = new[] { "staff" };
		var group = new GroupPolicy( "staff", parents, entries );
		var user = new UserPolicy( User, assignments, new[] { Entry( PermissionEffect.Unset ) } );
		parents[ 0 ] = "missing";
		entries[ 0 ] = Entry( PermissionEffect.Deny );
		assignments[ 0 ] = "missing";
		var policy = new PolicySnapshot( 1, new[] { new GroupPolicy( "player" ), group }, new[] { user } );
		Assert.True( new PermissionResolver( policy, Registered ).Explain( User, Key ).IsAllowed );
		Assert.Empty( user.Permissions );
		Assert.Throws<NotSupportedException>( () => ((IList<string>)group.Parents)[ 0 ] = "other" );
	}

	[Fact]
	public void ReservedOwnerCannotBeCreatedInheritedOrAssignedAsAnOrdinaryGroup()
	{
		Assert.Throws<ArgumentException>( () => new GroupPolicy( "owner" ) );
		Assert.Throws<ArgumentException>( () => new GroupPolicy( "staff", new[] { "owner" } ) );
		Assert.Throws<ArgumentException>( () => new UserPolicy( User, new[] { "owner" } ) );
		Assert.Throws<ArgumentException>( () => new GroupPolicy( "Owner" ) );
	}

	[Fact]
	public void DefaultPolicyGrantsPublicHelpAndKeepsStaffAuthorityExplicit()
	{
		var policy = PolicySnapshot.CreateDefault();
		Assert.Empty( policy.Users );
		Assert.Equal( 0, policy.Groups.Single( group => group.Id == "player" ).ProtectionLevel );
		Assert.Equal( 10, policy.Groups.Single( group => group.Id == "moderator" ).ProtectionLevel );
		Assert.Equal( 50, policy.Groups.Single( group => group.Id == "admin" ).ProtectionLevel );
		var resolver = new PermissionResolver( policy, Registered.Concat( new[] { new PermissionDescriptor( "admin.help", "Help" ) } ) );
		Assert.True( resolver.Explain( User, "admin.help" ).IsAllowed );
		Assert.False( resolver.Explain( User, Key ).IsAllowed );
		Assert.DoesNotContain( policy.Groups.SelectMany( group => group.Permissions ), entry => entry.Key.StartsWith( "admin.security" ) );
	}
}
