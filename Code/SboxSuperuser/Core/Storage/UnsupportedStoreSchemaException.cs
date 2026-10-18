namespace SboxSuperuser.Core.Storage;

internal sealed class UnsupportedStoreSchemaException : Exception
{
	internal UnsupportedStoreSchemaException() : base( "Unsupported storage format or payload schema." )
	{
	}
}
