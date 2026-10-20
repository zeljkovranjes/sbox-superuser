namespace SboxSuperuser.Engine.Commands;

internal sealed partial class AdministrationCommands
{
	// Superuser documents itself through the same API any module uses.
	private void RegisterDocumentation()
	{
		_scope.AddDocumentation( "Getting started", GettingStarted );
		_scope.AddDocumentation( "Roles and permissions", RolesAndPermissions );
	}

	private const string GettingStarted = """
		# Getting started

		Superuser runs every command on the server: it checks who you are, what you may do,
		records the attempt, runs it and replies only to you.

		## Three ways in

		- **Console:** `admin help`, `admin ban <user> 2h Griefing`
		- **Chat:** the same commands with a slash, `/admin help`
		- **This menu:** every window here runs the same commands for you

		`<user>` is a Steam ID, or a name when exactly one online player matches.

		## Reading a result

		| Result | Meaning |
		|---|---|
		| Success | Done |
		| NoChange | Nothing needed changing |
		| Denied | You lack the permission, or the target outranks you |
		| Unknown | The outcome could not be confirmed; check Activity before retrying |

		Unknown is never success. Look before you repeat anything with an effect.
		""";

	private const string RolesAndPermissions = """
		# Roles and permissions

		Every command needs one permission key, such as `admin.players.ban`. Nothing is
		allowed until it is granted.

		## Built-in groups

		| Group | Grants |
		|---|---|
		| `player` | Help and your own results; everyone has it |
		| `moderator` | Kick, warn, text mute, notes, recent chat |
		| `admin` | Ban, history and settings, plus everything a moderator has |

		Owners are set by the host and outrank every group.

		## Giving access

		```text
		admin user <user> group add moderator
		admin user <user> permission allow admin.players.kick
		admin permissions explain <user> admin.players.ban
		```

		Owners can do the same in **Server › Permissions**.

		## Protection

		Groups have a protection level from 0 to 99. You can only punish someone whose level is
		lower than yours, so equals cannot punish each other and no owner can punish another.
		""";
}
