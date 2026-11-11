using System;
using System.Collections.Generic;

namespace SboxSuperuser;

/// <summary>A persisted timed player ability restriction, independent of game and storage adapters.</summary>
public sealed record PlayerRestriction(ulong SteamId,DateTimeOffset? Until,string Reason)
{
	public Guid RequestId { get; init; }
	public DateTimeOffset CreatedUtc { get; init; }
	public bool Active(DateTimeOffset now) => Until is null || Until>now;
	public static void Validate(PlayerRestriction[] records)
	{
		if(records is null || records.Length>512)throw new ArgumentException("At most 512 ability restrictions are supported.");
		var ids=new HashSet<ulong>();
		foreach(var row in records)
		{
			if(row is null || row.SteamId==0 || !ids.Add(row.SteamId))throw new ArgumentException("Invalid or duplicate player identity.");
			ValidateReason(row.Reason);
		}
	}
	public static void ValidateReason(string reason)
	{
		if(string.IsNullOrWhiteSpace(reason) || reason.Length>256)throw new ArgumentException("Supply a reason of 1–256 printable characters.");
		foreach(var c in reason)if(char.IsControl(c))throw new ArgumentException("The reason must be a single printable line.");
	}
}
