namespace SboxSuperuser;

/// <summary>Private client notification from a verified host reply; it grants no command authority.</summary>
public sealed class AdminChatResultEvent
{
	/// <summary>Gets the request ID associated with this reply.</summary>
	public Guid RequestId { get; }

	/// <summary>Gets immutable caller-safe status, message and structured fields.</summary>
	public CommandResult Result { get; }

	/// <summary>Set true when a custom view rendered this result to avoid duplicate default chatbox display.</summary>
	public bool SuppressDefaultDisplay { get; set; }

	internal AdminChatResultEvent( Guid requestId, CommandResult result )
	{
		RequestId = requestId;
		Result = result;
	}
}
