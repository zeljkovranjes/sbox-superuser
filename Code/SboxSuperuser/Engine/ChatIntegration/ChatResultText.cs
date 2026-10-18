namespace SboxSuperuser.Engine.ChatIntegration;

internal static class ChatResultText
{
	internal static IEnumerable<string> Lines( CommandResult result )
	{
		yield return $"[Superuser] {result.Status}: {result.Message}";
		foreach ( var field in result.Data )
		{
			if ( field.Key is "row1" or "row2" or "row3" or "row4" or "row5" or "row6" or "row7" or "row8" )
			{
				yield return field.Value;
			}
			else if ( field.Key is not ("page" or "more") )
			{
				yield return $"{field.Key}: {field.Value}";
			}
		}
		if ( result.Data.TryGetValue( "page", out var page ) )
		{
			var more = result.Data.TryGetValue( "more", out var value ) && value == "True";
			yield return $"Page {page}" + (more ? " — more results available." : ".");
		}
	}
}
