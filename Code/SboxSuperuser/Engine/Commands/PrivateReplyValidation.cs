namespace SboxSuperuser.Engine.Commands;

internal static class PrivateReplyValidation
{
	internal static bool IsValid( Guid requestId, CommandStatus status, string message, string[] keys, string[] values )
	{
		return requestId != Guid.Empty && Enum.IsDefined( status ) && message is not null && message.Length <= 1024
			&& keys is not null && values is not null && keys.Length == values.Length && keys.Length <= 16
			&& keys.All( key => key is not null && key.Length <= 64 ) && values.All( value => value is not null && value.Length <= 1024 )
			&& keys.Distinct( StringComparer.Ordinal ).Count() == keys.Length
			&& keys.Sum( key => key.Length ) + values.Sum( value => value.Length ) <= 8192;
	}
}
