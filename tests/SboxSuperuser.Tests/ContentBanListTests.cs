using SboxSuperuser.Core.Content;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class ContentBanListTests
{
	[Fact]
	public void MatchesExactlyOrWithWildcardsIgnoringCase()
	{
		var list = new ContentBanList();
		list.Load( new[]
		{
			new ContentBan( ContentBanList.Map, "facepunch.construct", "Too big" ),
			new ContentBan( ContentBanList.Addon, "badauthor.*", "Malware" ),
			new ContentBan( ContentBanList.Entity, "models/*/nuke.vmdl", "" )
		} );
		Assert.NotNull( list.Find( ContentBanList.Map, "Facepunch.Construct" ) );
		Assert.Null( list.Find( ContentBanList.Map, "facepunch.flatgrass" ) );
		Assert.Equal( "Malware", list.Find( ContentBanList.Addon, "badauthor.guns" ).Reason );
		Assert.Null( list.Find( ContentBanList.Map, "badauthor.guns" ) );
		Assert.NotNull( list.Find( ContentBanList.Entity, "models/weapons/nuke.vmdl" ) );
		Assert.Null( list.Find( ContentBanList.Entity, "models/weapons/nuke.vmdl_c" ) );
	}

	[Fact]
	public void RefusesBadKindsAndPatterns()
	{
		Assert.NotNull( ContentBanList.Problem( "vehicle", "car" ) );
		Assert.NotNull( ContentBanList.Problem( ContentBanList.Map, "*" ) );
		Assert.NotNull( ContentBanList.Problem( ContentBanList.Map, "two words" ) );
		Assert.Null( ContentBanList.Problem( ContentBanList.Entity, "Sandbox.Prop" ) );
		Assert.Throws<FormatException>( () => new ContentBanList().Load( new[] { new ContentBan( "vehicle", "car", "" ) } ) );
	}
}
