namespace SboxSuperuser.Engine.Commands;

internal static class PrivateReplyValidation
{
	internal static bool IsValid( Guid requestId, CommandStatus status, string message, string[] keys, string[] values )
	{
		return requestId != Guid.Empty && Enum.IsDefined( status ) && !string.IsNullOrWhiteSpace( message ) && message.Length <= 1024
			&& !message.Any( char.IsControl )
			&& keys is not null && values is not null && keys.Length == values.Length && keys.Length <= 16
			&& keys.All( key => !string.IsNullOrWhiteSpace( key ) && key.Length <= 64 && !key.Any( char.IsControl ) )
			&& values.All( value => value is not null && value.Length <= 1024 && !value.Any( char.IsControl ) )
			&& keys.Distinct( StringComparer.Ordinal ).Count() == keys.Length
			&& keys.Sum( key => key.Length ) + values.Sum( value => value.Length ) <= 8192;
	}
}
