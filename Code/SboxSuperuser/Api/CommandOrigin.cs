namespace SboxSuperuser;

/// <summary>Transport that submitted the request, independently of actor authority.</summary>
public enum CommandOrigin
{
	/// <summary>Unclassified transport.</summary>
	Unknown = 0,

	/// <summary>Native console transport.</summary>
	Console = 1,

	/// <summary>Authenticated chat transport.</summary>
	Chat = 2,

	/// <summary>Host service invocation.</summary>
	InternalService = 3,

	/// <summary>Authenticated administration interface.</summary>
	Interface = 4,
}
