namespace SboxSuperuser.Core.Commands;

internal sealed class RequestWork<T>
{
	internal RequestKey Key { get; }
	internal string Fingerprint { get; }
	internal Func<Task<T>> Execute { get; set; }
	internal TaskCompletionSource<T> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
	internal CancellationTokenRegistration Cancellation { get; set; }
	internal bool Started { get; set; }
	internal DateTimeOffset? CompletedUtc { get; set; }

	internal RequestWork( RequestKey key, string fingerprint, Func<Task<T>> execute )
	{
		Key = key;
		Fingerprint = fingerprint;
		Execute = execute;
	}
}
