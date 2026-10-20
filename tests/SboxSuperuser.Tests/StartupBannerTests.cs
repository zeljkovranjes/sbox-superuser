using SboxSuperuser.Core.Bootstrap;
using Xunit;

namespace SboxSuperuser.Tests;

public sealed class StartupBannerTests
{
	[Fact]
	public void TheHeaderShowsTheLogoVersionAndOwners()
	{
		var header = StartupBanner.Header( "1.2.3", "chomnr", 2, "superuser/owners.json" );

		Assert.Contains( "███████╗██╗   ██╗██████╗", header );
		Assert.Contains( "v1.2.3 by chomnr", header );
		Assert.Contains( "2 owners from superuser/owners.json", header );
		Assert.Contains( "No owners yet", StartupBanner.Header( "1.2.3", "chomnr", 0, "owners.json" ) );
	}

	[Fact]
	public void ModuleLinesSayWhatEachModuleAdded()
	{
		var loaded = new ModuleReport( "example.reports", "1.0.0", "Someone", true, "", 6, 4, 4, 2 );
		var plain = new ModuleReport( "example.greeter", "", "", true, "", 1, 1, 0, 0 );
		var broken = new ModuleReport( "example.broken", "", "", false, "requires missing module 'example.base'", 0, 0, 0, 0 );

		Assert.Equal( "  ✓ example.reports 1.0.0 by Someone · 6 commands · 4 permissions · 4 guide pages · 2 menu windows", StartupBanner.Module( loaded ) );
		Assert.Equal( "  ✓ example.greeter · 1 command · 1 permission", StartupBanner.Module( plain ) );
		Assert.Equal( "  ✗ example.broken: requires missing module 'example.base'", StartupBanner.Module( broken ) );
		Assert.Equal( "  Ready: 2 modules, 7 commands, 1 not loaded (see above). Type \"admin help\" in the console or \"/admin help\" in chat.",
			StartupBanner.Ready( new[] { loaded, plain, broken }, "admin" ) );
	}
}
