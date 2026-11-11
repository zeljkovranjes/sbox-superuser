using Sandbox;
using SboxSuperuser.Engine.Vendor.ImGui;

namespace SboxSuperuser.UI;

internal sealed partial class AdminMenuRenderer
{
	private const string PropsCleanupCommand = "props.cleanup";
	private const string PropsDelayCommand = "props.delay";
	private const string PropsCleanupAllCommand = "props.cleanupall";
	private const int DelayLength = 12;

	// Filled from the host once, then left to the user so a refresh never overwrites what they are typing.
	private string _propDelay;
	private bool _keepProps;

	/// <summary>The cleanup delay on top, then who owns what with a Clean up action each.</summary>
	private void DrawPropsWindow()
	{
		if ( !BeginWindow( PropsWindow, "Props", new Vector2( 790, 80 ), new Vector2( 520, 420 ), new Vector2( 420, 260 ), out var expanded ) ) return;
		if ( expanded )
		{
			var reply = Server.Props;
			if ( reply is null ) Caption( "Loading..." );
			else if ( reply.Status != CommandStatus.Success ) ColoredWrapped( reply.Message, AdminMenuTheme.Tone( reply.Status ) );
			else
			{
				DrawPropCleanupDelay( reply );
				ImGui.Separator();
				DrawPropOwners( reply );
			}
		}
		ImGui.End();
	}

	private void DrawPropCleanupDelay( AdminReply reply )
	{
		Caption( reply.Message );
		if ( !Server.Can( PropsDelayCommand ) ) return;
		if ( _propDelay is null )
		{
			_keepProps = reply.PropCleanupSeconds is null;
			_propDelay = DelayText( reply.PropCleanupSeconds ?? 120 );
		}
		ImGui.AlignTextToFramePadding();
		ImGui.TextUnformatted( "Remove after" );
		ImGui.SameLine();
		ImGui.BeginDisabled( _keepProps );
		Field( "##delay", "2m", ref _propDelay, DelayLength, 80 * Scale );
		ImGui.EndDisabled();
		ImGui.SameLine();
		ImGui.Checkbox( "Keep until cleaned up", ref _keepProps );
		ImGui.SameLine();
		ImGui.BeginDisabled( Server.Running || !_keepProps && _propDelay.Trim().Length == 0 );
		if ( ImGui.Button( "Apply" ) ) Server.Run( PropsDelayCommand, new[] { _keepProps ? "permanent" : _propDelay.Trim() } );
		ImGui.EndDisabled();
		ImGui.SetItemTooltip( "{0}", "Between 10s and 7d, such as 90s, 5m or 1h. Players who come back in time keep their props." );
	}

	private void DrawPropOwners( AdminReply reply )
	{
		if ( Server.Can( PropsCleanupAllCommand ) )
		{
			ImGui.BeginDisabled( Server.Running || reply.PropOwners.Length == 0 );
			if ( ImGui.Button( "Clean up all" ) )
				RunCommand( "Clean up every player's props", PropsCleanupAllCommand, Array.Empty<string>(), true, false, true,
					("Owners", reply.PropOwners.Length.ToString()), ("Props", reply.PropOwners.Sum( owner => owner.Props ).ToString()) );
			ImGui.EndDisabled();
			ImGui.SameLine();
			Caption( "The map's own objects are never removed." );
		}
		if ( reply.PropOwners.Length == 0 ) { Caption( "Nobody has spawned anything." ); return; }
		var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
		if ( !ImGui.BeginTable( "prop-owners", 3, flags ) ) return;
		ImGui.TableSetupColumn( "Player", ImGuiTableColumnFlags.WidthStretch );
		ImGui.TableSetupColumn( "Props", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "Props" ).x + 16 );
		ImGui.TableSetupColumn( "", ImGuiTableColumnFlags.WidthFixed, ButtonWidth( "Clean up" ) );
		ImGui.TableHeadersRow();
		foreach ( var owner in reply.PropOwners )
		{
			ImGui.PushID( owner.SteamId );
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( owner.Name );
			if ( !owner.Online ) { ImGui.SameLine(); ImGui.TextDisabled( "{0}", "left" ); }
			ImGui.TableNextColumn();
			ImGui.TextUnformatted( owner.Props.ToString() );
			ImGui.TableNextColumn();
			ImGui.BeginDisabled( Server.Running );
			if ( ImGui.SmallButton( "Clean up" ) )
				RunCommand( $"Clean up {owner.Name}'s props", PropsCleanupCommand, new[] { owner.SteamId }, true, false, true, ("Props", owner.Props.ToString()) );
			ImGui.EndDisabled();
			ImGui.PopID();
		}
		ImGui.EndTable();
	}

	// The shortest exact form: 120 seconds reads as 2m, 86400 as 1d.
	private static string DelayText( long seconds ) =>
		seconds % 86400 == 0 ? $"{seconds / 86400}d" : seconds % 3600 == 0 ? $"{seconds / 3600}h" : seconds % 60 == 0 ? $"{seconds / 60}m" : $"{seconds}s";
}
