namespace SboxSuperuser.Core.Commands;

internal sealed record QueuedResult<T>( QueueAdmission Admission, Task<T> Result );
