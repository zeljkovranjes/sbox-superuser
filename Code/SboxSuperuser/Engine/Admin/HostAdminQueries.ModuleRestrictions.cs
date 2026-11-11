namespace SboxSuperuser.Engine.Admin;

internal sealed partial class HostAdminQueries
{
	(AdminPlayerActionRow[] Actions, AdminModerationRow[] Rows, bool CanRead) ReadModuleRestrictions( Func<string, bool> can, bool canTarget, string target = null )
	{
		var actions = new List<AdminPlayerActionRow>();
		var rows = new List<AdminModerationRow>();
		var canRead = false;
		if ( _registry is null ) return (actions.ToArray(), rows.ToArray(), canRead);
		foreach ( var (module, provider) in _registry.RestrictionProviders )
		{
			if ( provider.Types is null || provider.Types.Count > 16 ) throw new InvalidOperationException( "Invalid restriction catalog: " + module );
			foreach ( var type in provider.Types )
			{
				var apply = _registry.Find( type.ApplyCommand )?.Descriptor;
				var restore = _registry.Find( type.RestoreCommand )?.Descriptor;
				if ( apply?.ModuleId != module || restore?.ModuleId != module || string.IsNullOrWhiteSpace( type.Label ) || type.Label.Length > 64 )
					throw new InvalidOperationException( "Restriction commands must belong to their module: " + module );
				var canRestore = can( restore.Permission );
				if ( canTarget && can( apply.Permission ) ) actions.Add( new() { Label = type.Label, Command = apply.Name } );
				if ( canRestore ) actions.Add( new() { Label = "Restore " + type.Label, Command = restore.Name, Restore = true } );
				if ( !can( type.ReadPermission ) ) continue;
				canRead = true;
				var records = provider.ReadActive( type.Kind );
				if ( records is null || records.Count > 512 ) throw new InvalidOperationException( "Invalid restriction records: " + module );
				foreach ( var row in records )
				{
					if ( !row.Active || row.ExpiresUtc <= DateTimeOffset.UtcNow || target is not null && row.SteamId != target ) continue;
					if ( !ulong.TryParse( row.SteamId, out var id ) || id == 0 || row.Reason is null || row.Reason.Length > 256 )
						throw new InvalidOperationException( "Invalid restriction evidence: " + module );
					rows.Add( new() { SteamId = row.SteamId, Kind = type.Label, RequestId = row.RequestId, CreatedUtc = row.CreatedUtc,
						ExpiresUtc = row.ExpiresUtc, Reason = row.Reason, Active = true, RestoreCommand = canRestore ? restore.Name : "" } );
				}
			}
		}
		return (actions.ToArray(), rows.ToArray(), canRead);
	}
}
