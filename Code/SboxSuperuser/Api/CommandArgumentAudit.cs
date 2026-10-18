namespace SboxSuperuser;

/// <summary>Controls audit value recording; free text is always redacted and sensitive arguments are refused through chat.</summary>
public enum CommandArgumentAudit
{
	/// <summary>Records only a redaction marker, the default for every argument.</summary>
	Redacted = 0,

	/// <summary>Records a validated non-text scalar value explicitly classified as safe by the module.</summary>
	SafeValue = 1,

	/// <summary>Always redacts the value and disallows the command through chat.</summary>
	Sensitive = 2
}
