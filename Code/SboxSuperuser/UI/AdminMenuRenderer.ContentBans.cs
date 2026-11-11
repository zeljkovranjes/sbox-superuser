using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string ContentListCommand = "content.list";
	private const string ContentSearchCommand = "content.search";
	private static readonly (string Kind, string Tab, string Search, string Key)[] ContentKinds =
	{
		("map", "Maps", "Search maps", "content-search-map"),
		("addon", "Addons", "Search addons", "content-search-addon"),
		("entity", "Entities", "Search prefabs and models", "content-search-entity")
	};
	private const int ContentQueryLength = 64;
	private const int ContentPatternLength = 128;
	private const int ContentReasonLength = 256;
	private const float ContentIconSize = 36;
	private const float SearchDelayMilliseconds = 300;

	private readonly CatalogSearch[] _contentSearches = { new(), new(), new() };
	private readonly string[] _bannedFilters = { "", "", "" };
	private readonly Dictionary<string, Texture> _contentIcons = new( StringComparer.Ordinal );
	private string _contentPattern = "";
	private string _contentReason = "";

	/// <summary>One tab per kind: what isn't banned on the left, searchable, and what is on the right, both with icons.</summary>
	private void DrawContentBansWindow()
	{
		if ( !BeginWindow( ContentBansWindow, "Content bans", new Vector2( 110, 80 ), new Vector2( 820, 520 ), new Vector2( 600, 320 ), out var expanded ) ) return;
		if ( expanded )
		{
			Field( "##reason", "Reason for new bans, shown when it's refused (optional)", ref _contentReason, ContentReasonLength, ImGui.GetContentRegionAvail().x );
			if ( ImGui.BeginTabBar( "content-kinds" ) )
			{
				for ( var kind = 0; kind < ContentKinds.Length; kind++ )
				{
					if ( !ImGui.BeginTabItem( ContentKinds[ kind ].Tab ) ) continue;
					DrawContentKind( kind );
					ImGui.EndTabItem();
				}
				ImGui.EndTabBar();
			}
		}
		ImGui.End();
	}

	private void DrawContentKind( int kind )
	{
		var height = ImGui.GetContentRegionAvail().y;
		var half = (ImGui.GetContentRegionAvail().x - ImGui.GetStyle().ItemSpacing.x) / 2;
		ImGui.BeginChild( "allowed", new Vector2( half, height ), ImGuiChildFlags.Borders );
		DrawAllowedContent( kind );
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild( "banned", new Vector2( 0, height ), ImGuiChildFlags.Borders );
		DrawBannedContent( kind );
		ImGui.EndChild();
	}

	private void DrawAllowedContent( int kind )
	{
		var (name, _, hint, key) = ContentKinds[ kind ];
		var search = _contentSearches[ kind ];
		ImGui.SeparatorText( "Not banned" );
		var query = search.Query;
		Field( "##search", hint, ref query, ContentQueryLength, ImGui.GetContentRegionAvail().x );
		if ( query != search.Query || search.Arguments.Length == 0 )
		{
			search.Query = query;
			search.Arguments = new[] { name, query };
			search.After = DateTimeOffset.UtcNow.AddMilliseconds( SearchDelayMilliseconds );
			Server.ResetLive( key );
		}
		DrawPatternBan( name );
		var reply = DateTimeOffset.UtcNow < search.After ? null : Server.Live( key, ContentSearchCommand, search.Arguments, DateTimeOffset.UtcNow );
		if ( reply is null ) { Caption( "Searching..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		search.Refresh( reply );
		if ( search.Rows.Length == 0 ) { Caption( reply.Message ); return; }
		if ( !ImGui.BeginTable( "allowed-items", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY ) ) return;
		SetupContentColumns( "Ban" );
		foreach ( var row in search.Rows )
		{
			ImGui.PushID( row.Item.Id );
			if ( ContentRow( row.Icon, row.Item.Title, row.Item.Id, "", "Ban" ) ) Ban( name, row.Item.Id );
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	// Wildcards such as author.* can't come from a search, so they are typed.
	private void DrawPatternBan( string kind )
	{
		var room = ImGui.GetContentRegionAvail().x - ButtonWidth( "Ban pattern" ) - ImGui.GetStyle().ItemSpacing.x;
		Field( "##pattern", "Or a pattern, such as author.*", ref _contentPattern, ContentPatternLength, room );
		ImGui.SameLine();
		var pattern = _contentPattern.Trim();
		ImGui.BeginDisabled( pattern.Length == 0 || Server.Running );
		if ( ImGui.Button( "Ban pattern" ) )
		{
			Ban( kind, pattern );
			_contentPattern = "";
		}
		ImGui.EndDisabled();
	}

	private void DrawBannedContent( int kind )
	{
		var name = ContentKinds[ kind ].Kind;
		ImGui.SeparatorText( "Banned" );
		Field( "##filter", "Filter", ref _bannedFilters[ kind ], ContentQueryLength, ImGui.GetContentRegionAvail().x );
		var reply = Server.ContentBans;
		if ( reply is null ) { Caption( "Loading..." ); return; }
		if ( reply.Status != CommandStatus.Success ) { ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) ); return; }
		var filter = _bannedFilters[ kind ].Trim();
		var shown = 0;
		if ( !ImGui.BeginTable( "banned-items", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY ) ) return;
		SetupContentColumns( "Unban" );
		foreach ( var ban in reply.ContentBans )
		{
			if ( ban.Kind != name ) continue;
			if ( filter.Length > 0 && !ban.Title.Contains( filter, StringComparison.OrdinalIgnoreCase )
				&& !ban.Pattern.Contains( filter, StringComparison.OrdinalIgnoreCase ) ) continue;
			shown++;
			ImGui.PushID( ban.Pattern );
			if ( ContentRow( ContentIcon( ban.Icon ), ban.Title, ban.Pattern, ban.Reason, "Unban" ) )
				Server.Run( "content.unban", new[] { ban.Kind, ban.Pattern } );
			ImGui.PopID();
		}
		ImGui.EndTable();
		if ( shown == 0 ) Caption( filter.Length > 0 ? "Nothing banned matches." : "Nothing banned here yet." );
	}

	private void SetupContentColumns( string action )
	{
		ImGui.TableSetupColumn( "Icon", ImGuiTableColumnFlags.WidthFixed, ContentIconSize * Scale );
		ImGui.TableSetupColumn( "Item", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Action", ImGuiTableColumnFlags.WidthFixed, ButtonWidth( action ) );
	}

	/// <summary>One item with its icon, title and ID (and reason when banned); true when its button was pressed.</summary>
	private bool ContentRow( Texture icon, string title, string id, string reason, string action )
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		var size = new Vector2( ContentIconSize, ContentIconSize ) * Scale;
		if ( icon is not null ) ImGui.Image( icon, size );
		else ImGui.Dummy( size );
		ImGui.TableNextColumn();
		ImGui.TextUnformatted( title );
		ImGui.TextDisabled( "{0}", reason.Length > 0 ? $"{id} - {reason}" : id );
		ImGui.TableNextColumn();
		ImGui.BeginDisabled( Server.Running );
		var pressed = ImGui.SmallButton( action );
		ImGui.EndDisabled();
		return pressed;
	}

	private void Ban( string kind, string id )
	{
		var reason = _contentReason.Trim();
		Server.Run( "content.ban", reason.Length > 0 ? new[] { kind, id, reason } : new[] { kind, id } );
	}

	// Each icon loads once; the engine fills it in when the image arrives.
	private Texture ContentIcon( string icon )
	{
		if ( icon.Length == 0 ) return null;
		if ( !_contentIcons.TryGetValue( icon, out var texture ) ) _contentIcons[ icon ] = texture = Texture.Load( icon, false );
		return texture;
	}
}
