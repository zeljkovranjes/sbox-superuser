using System.Globalization;
using System.Text;
using System.Text.Json;
using SboxSuperuser.Core.Contracts;
using SboxSuperuser.Core.Identity;

namespace SboxSuperuser.Core.Storage;

internal static class TransactionFormat
{
	internal static void Validate( string payload )
	{
		if ( payload is null || Encoding.UTF8.GetByteCount( payload ) > StoreFormat.MaximumBytes / 2 )
		{
			throw new FormatException( "Transaction payload exceeds its limit." );
		}

		using var document = JsonDocument.Parse( payload, new JsonDocumentOptions { MaxDepth = 32 } );
		var root = document.RootElement;
		StoreFormat.RequireFields( root, "state", "auditIntents" );
		var state = root.GetProperty( "state" );
		if ( state.ValueKind != JsonValueKind.Object ) throw new FormatException( "State must be an object." );
		RejectDuplicateFields( state );
		var intents = root.GetProperty( "auditIntents" );
		if ( intents.ValueKind != JsonValueKind.Array || intents.GetArrayLength() is < 1 or > 256 )
		{
			throw new FormatException( "A transaction needs one to 256 audit intents." );
		}

		var ids = new HashSet<Guid>();
		foreach ( var intent in intents.EnumerateArray() )
		{
			StoreFormat.RequireFields( intent, "id", "requestId", "actorKind", "actorSubject", "origin",
				"command", "safeArguments", "targets", "createdUtc", "outcome" );
			var id = intent.GetProperty( "id" ).GetGuid();
			if ( id == Guid.Empty || !ids.Add( id ) || intent.GetProperty( "requestId" ).GetGuid() == Guid.Empty )
			{
				throw new FormatException( "Intent and request IDs must be explicit; intent IDs must be unique." );
			}

			var subject = intent.GetProperty( "actorSubject" ).GetString();
			var origin = intent.GetProperty( "origin" ).GetString();
			switch ( intent.GetProperty( "actorKind" ).GetString() )
			{
				case "Player" when origin is "Console" or "Chat":
					SteamIdentity.Validate( subject, "actorSubject" );
					break;
				case "DedicatedConsole" when origin == "Console" && subject == "dedicated-console":
					break;
				case "InternalService" when origin == "InternalService":
					ContractGuard.Key( subject, "actorSubject" );
					break;
				default:
					throw new FormatException( "Invalid recorded actor or origin." );
			}

			ContractGuard.Key( intent.GetProperty( "command" ).GetString(), "command" );
			if ( intent.GetProperty( "outcome" ).GetString() != "Unknown"
				|| !DateTimeOffset.TryParseExact( intent.GetProperty( "createdUtc" ).GetString(), "O",
					CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp ) || timestamp.Offset != TimeSpan.Zero )
			{
				throw new FormatException( "Intent must have a UTC timestamp and Unknown outcome." );
			}

			var arguments = intent.GetProperty( "safeArguments" );
			if ( arguments.ValueKind != JsonValueKind.Object ) throw new FormatException( "Expected safe argument fields." );
			RejectDuplicateFields( arguments );
			if ( arguments.EnumerateObject().Count() > 32 ) throw new FormatException( "Too many safe argument fields." );
			foreach ( var argument in arguments.EnumerateObject() )
			{
				ContractGuard.Key( argument.Name, "safeArguments" );
				if ( argument.Value.ValueKind != JsonValueKind.String || argument.Value.GetString().Length > 256 )
				{
					throw new FormatException( "Safe arguments must be bounded strings." );
				}
			}

			var targets = intent.GetProperty( "targets" );
			if ( targets.ValueKind != JsonValueKind.Array || targets.GetArrayLength() > 64 ) throw new FormatException( "Invalid targets." );
			foreach ( var target in targets.EnumerateArray() ) SteamIdentity.Validate( target.GetString(), "targets" );
		}
	}

	private static void RejectDuplicateFields( JsonElement element )
	{
		if ( element.ValueKind == JsonValueKind.Object )
		{
			var names = new HashSet<string>( StringComparer.Ordinal );
			foreach ( var property in element.EnumerateObject() )
			{
				if ( !names.Add( property.Name ) ) throw new FormatException( "Duplicate state field." );
				RejectDuplicateFields( property.Value );
			}
		}
		else if ( element.ValueKind == JsonValueKind.Array )
		{
			foreach ( var child in element.EnumerateArray() ) RejectDuplicateFields( child );
		}
	}
}
