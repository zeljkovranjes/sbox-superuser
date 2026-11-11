using System.Text.Json;
using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	readonly Dictionary<string, CatalogSearch> _catalogSearches = new();

	void DrawModuleCatalog( AdminMenuElementRow element, string key )
	{
		if ( element.Arguments.Length != 2 ) return;
		if ( !_catalogSearches.TryGetValue( key, out var search ) ) _catalogSearches[key] = search = new();
		ImGui.SeparatorText( Label( element.Text ) );
		var changed = false;
		if ( ImGui.Button( search.Blocked ? "Find items" : "• Find items" ) && search.Blocked ) { search.Blocked = false; changed = true; }
		ImGui.SameLine();
		if ( ImGui.Button( search.Blocked ? "• Blocked items" : "Blocked items" ) && !search.Blocked ) { search.Blocked = true; changed = true; }
		ImGui.Spacing();
		var query = search.Query;
		Field( "##search", "Search props, items, entities or tools…", ref query, 80, ImGui.GetContentRegionAvail().x );
		if ( query != search.Query || changed )
		{
			search.Query = query;
			search.Page = 1;
			search.Arguments = search.Blocked ? new[] { query, "1" } : new[] { query };
			search.After = DateTimeOffset.UtcNow.AddMilliseconds( 300 );
			search.Rows = Array.Empty<CatalogRow>();
			Server.ResetLive( key );
		}
		var reply = DateTimeOffset.UtcNow < search.After ? null : Server.Live( key, search.Blocked ? element.StatusCommand : element.Command, search.Arguments, DateTimeOffset.UtcNow );
		if ( reply is null ) { Caption( "Searching…" ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		search.Refresh( reply );
		Caption( reply.Message );
		if ( search.Blocked )
		{
			ImGui.BeginDisabled( search.Page == 1 );
			var previous = ImGui.SmallButton( "Previous" );
			ImGui.EndDisabled(); ImGui.SameLine();
			ImGui.BeginDisabled( !reply.Data.TryGetValue( "more", out var more ) || more != "True" );
			var next = ImGui.SmallButton( "Next" );
			ImGui.EndDisabled();
			if ( previous || next )
			{
				search.Page += previous ? -1 : 1;
				search.Arguments = new[] { search.Query, search.Page.ToString() };
				Server.ResetLive( key );
			}
		}
		ImGui.Spacing();
		if ( ImGui.BeginTable( "results", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg, new Vector2( 0, 340 * Scale ) ) )
		{
			ImGui.TableSetupColumn( "Icon", ImGuiTableColumnFlags.WidthFixed, 46 * Scale );
			ImGui.TableSetupColumn( "Resource", ImGuiTableColumnFlags.WidthStretch );
			ImGui.TableSetupColumn( "Action", ImGuiTableColumnFlags.WidthFixed, 80 * Scale );
			foreach ( var row in search.Rows ) DrawCatalogRow( element, row );
			ImGui.EndTable();
		}
		if ( Server.LastCommand == element.Arguments[0] || Server.LastCommand == element.Arguments[1] )
			if ( Server.LastResult is {} result ) ColoredWrapped( result.Message, AdminMenuTheme.Tone( result.Status ) );
	}

	void DrawCatalogRow( AdminMenuElementRow element, CatalogRow row )
	{
		ImGui.PushID( row.Item.Id );
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		if ( row.Icon is not null ) ImGui.Image( row.Icon, new Vector2( 40, 40 ) * Scale );
		else ImGui.TextUnformatted( "◇" );
		ImGui.TableNextColumn();
		ImGui.TextUnformatted( row.Item.Title );
		Caption( row.Item.Id );
		ImGui.TableNextColumn();
		var command = row.Item.Blocked ? element.Arguments[1] : element.Arguments[0];
		ImGui.BeginDisabled( Server.Running || !Server.Can( command ) );
		if ( ImGui.Button( row.Item.Blocked ? "Unblock" : "Block", new Vector2( 76 * Scale, 0 ) ) )
			RunCommand( (row.Item.Blocked ? "Allow " : "Block ") + row.Item.Title + "?", command, new[] { row.Item.Id }, confirm: true );
		ImGui.EndDisabled();
		ImGui.PopID();
	}

	sealed record CatalogRow( AdminCatalogItem Item, Texture Icon );
	sealed class CatalogSearch
	{
		internal bool Blocked;
		internal int Page = 1;
		internal string Query = "";
		internal string[] Arguments = Array.Empty<string>();
		internal DateTimeOffset After;
		internal CatalogRow[] Rows = Array.Empty<CatalogRow>();
		AdminReply _reply;
		internal void Refresh( AdminReply reply )
		{
			if ( ReferenceEquals( _reply, reply ) ) return;
			_reply = reply;
			var rows = new List<CatalogRow>();
			foreach ( var field in reply.Data )
			{
				if ( !field.Key.StartsWith( "item", StringComparison.Ordinal ) ) continue;
				try
				{
					var item = JsonSerializer.Deserialize<AdminCatalogItem>( field.Value );
					if ( item is null || string.IsNullOrWhiteSpace( item.Id ) ) continue;
					var icon = string.IsNullOrWhiteSpace( item.Icon ) ? null : Texture.Load( item.Icon, false );
					rows.Add( new( item, icon ) );
				}
				catch ( Exception error ) { Log.Warning( $"[Superuser] Invalid catalog item {field.Key}: {error.Message}" ); }
			}
			Rows = rows.ToArray();
		}
	}
}
