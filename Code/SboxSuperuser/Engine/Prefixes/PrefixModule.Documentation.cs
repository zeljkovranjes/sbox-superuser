using SboxSuperuser.Core.Effects;

namespace SboxSuperuser.Engine.Prefixes;

internal sealed partial class PrefixModule
{
	private void RegisterDocumentation( IModuleRegistration registration )
	{
		registration.AddDocumentation( "Prefixes", """
			# Prefixes

			A prefix is a styled tag in front of a player's name, like **[ADMIN]**. Every prefix has:

			- a **tag**, the text in front of the name;
			- a **template** that places it, `%prefix% %name%` unless you change it;
			- a **tag effect** and a **name effect**, which color and animate each part;
			- an optional **palette** of colors the effects can use.

			## Who wears which

			1. A player's own prefix, set with `prefix user set`, always wins.
			2. Otherwise the prefix of their group with the highest protection level.
			3. The group `owner` stands for every owner, above every other group.

			A new server starts with `[OWNER]`, `[ADMIN]` and `[MOD]` for the owner, admin and moderator groups.
			They are ordinary prefixes: change or delete them.

			## In the menu

			Open **Server › Prefixes**. Pick a prefix on the left to edit it, use **Edit code** to open the effect
			editor, and the **Groups** and **Players** tabs to hand prefixes out. Changes need the
			`admin.prefixes.manage` permission, which admins have by default.

			## From the console

			```text
			admin prefix create vip [VIP]
			admin prefix update vip tag-effect preset:rain
			admin prefix update vip name-effect hsv(t / 5 + i * 30, 1, 1)
			admin prefix update vip palette #ff3333,#ffff00
			admin prefix group set admin vip
			admin prefix user set 76561198000000000 vip
			```

			Console effects are one line. Multi-line effects are written in the menu's editor.

			## Chat

			While prefixes are on, Superuser draws the chat box itself so tags can be colored and animated, because
			the built-in s&box chat only shows plain text. Turn **Prefixed Chat** off on the Superuser Bootstrap to keep
			the built-in chat and show prefixes in your own UI instead.
			""" );
		registration.AddDocumentation( "Writing effects", """
			# Writing effects

			An effect is a few lines that run for every letter. Start simple and add one idea at a time.

			## One color

			A line that is just a color colors every letter:

			```effect
			cyan
			```

			## A color per letter

			`i` is the letter's position, starting at 0, and `palette` is the prefix's colors:

			```effect
			color = palette[i]
			```

			## Animation

			`t` is the time in milliseconds, the same for every player. Dividing it slows things down:

			```effect
			// every letter steps to the next palette color 10 times a second
			color = palette[floor(t / 100) + i]
			```

			## Decisions

			```effect
			if i == 0 {
			    color = red
			    bold = true
			} else {
			    color = mix(white, red, i / L)
			}
			```

			## Changing letters

			`char` replaces the letter, `hidden` removes it and `offset` pushes it (and everything after it) right:

			```effect
			char = i == pingpong(t / 75, L) ? "o" : "."
			```

			## When something is wrong

			The editor underlines the problem and says what to do, for example
			`"rde" isn't defined; did you mean red?`. Save is blocked until the effect is clean. The full list of
			names is in **Effect reference**.
			""" );
		registration.AddDocumentation( "Effect reference", EffectReference.Markdown() );
		registration.AddDocumentation( "Presets", EffectReference.PresetsMarkdown() );
		if ( _host.ApiGuide.Length > 0 ) registration.AddDocumentation( "For game code", _host.ApiGuide );
	}
}
