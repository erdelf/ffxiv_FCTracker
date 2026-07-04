namespace FCTracker.UI.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.IPC;

public class ReadyNowView : IFCView
{
    public string Id => "ready";

    private static readonly Dictionary<string, string> HeaderTooltips = new()
    {
        ["FC Points"] = "FC points are required for fuel and the submarine licenses\n99.900 are required for one stack of fuel\n160.000 are required for 16 Dive credits for all 4 submarines.",
    };

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx) =>
        ("Ready for Housing", $"{ctx.Data.GetReadyCount()} FCs eligible");

    public void Draw(FCViewContext ctx)
    {
        IReadOnlyList<FCData> readyFCs = ctx.Data.GetEligibleFCs();

        using ImRaii.ChildDisposable scrollArea = ImRaii.Child("##ReadyScroll", Vector2.Zero, false);
        if (!scrollArea.Success) 
            return;

        if (readyFCs.Count == 0)
        {
            ImGui.SetCursorPos(new Vector2(14, 20));
            FCTrackerWidgets.IconLabel(FCTrackerTheme.TextSecondary, FontAwesomeIcon.Hourglass,
                "No FCs are currently eligible for housing.");
            return;
        }

        ImGui.SetCursorPos(new Vector2(14, 12));
        DrawBannerHeader(readyFCs.Count);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8);

        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY        |
                                      ImGuiTableFlags.PadOuterX      |
                                      ImGuiTableFlags.SizingFixedFit |
                                      ImGuiTableFlags.Resizable;

        using ImRaii.TableDisposable table = ImRaii.Table("##ReadyTable", 4, flags);
        if (!table.Success) 
            return;

        ImGui.TableSetupScrollFreeze(4, 1);

        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 75);
        ImGui.TableSetupColumn("FC", ImGuiTableColumnFlags.WidthFixed, 540);
        ImGui.TableSetupColumn("FC Points", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("##Spacer",  ImGuiTableColumnFlags.WidthStretch);


        using (ImRaii.PushColor(ImGuiCol.TableHeaderBg, FCTrackerTheme.BackgroundHeader))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.TextSecondary))
            FCTrackerWidgets.TableHeadersRowWithTooltips(HeaderTooltips);

        foreach (FCData fc in readyFCs)
            DrawRow(fc);
    }

    private static void DrawBannerHeader(int count)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.AccentGreenDim))
        {
            using ImRaii.ChildDisposable banner = ImRaii.Child("##ReadyHeader", new Vector2(ImGui.GetContentRegionAvail().X - 28, 40), true);
            if (!banner.Success)
                return;

            ImGui.SetCursorPos(new Vector2(14, 10));
            FCTrackerWidgets.IconLabel(FCTrackerTheme.AccentGreen, FontAwesomeIcon.CheckCircle,
                $"{count} Free {(count == 1 ? "Company" : "Companies")} Ready for Housing");

            ImGui.SameLine(0, 20f);

            if (FCTrackerPlugin.Plugin.IsEntryPeriod)
            {
                FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentGreen, 
                                             $"Entry period active till {FCTrackerPlugin.Plugin.EntryPeriodCurrentStartDate.AddDays(FCTrackerPlugin.ENTRY_PERIOD_DURATION_DAYS):d}");
            }
            else
            {
                TimeSpan timeUntilNextEntry = FCTrackerPlugin.Plugin.EntryPeriodNextStartDate - DateTime.Now;
                if(timeUntilNextEntry.Days <= 0)
                    FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentGreen,
                                                 @$"Next Entry period starting in {@timeUntilNextEntry:h\h} to {FCTrackerPlugin.Plugin.EntryPeriodNextStartDate.AddDays(FCTrackerPlugin.ENTRY_PERIOD_DURATION_DAYS):d}");
                else
                    FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, 
                                             $"Next Entry period active from {FCTrackerPlugin.Plugin.EntryPeriodNextStartDate:d} to {FCTrackerPlugin.Plugin.EntryPeriodNextStartDate.AddDays(FCTrackerPlugin.ENTRY_PERIOD_DURATION_DAYS):d}");
            }
        }
    }

    private static void DrawRow(FCData fc)
    {
        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, fc.LoggedIn ? ImGui.GetColorU32(FCTrackerTheme.RowHighlightColor) : ImGui.GetColorU32(FCTrackerTheme.AccentGreenDim));

        ImGui.TableNextColumn();

		Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(new Vector2(screenPos.X + 4, screenPos.Y + 7), 4, ImGui.GetColorU32(FCTrackerTheme.AccentGreen));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentGreen, "READY");

        ImGui.TableNextColumn();

        bool selectable = fc.SourceData.ImportSourceConfig == null && fc.MemberCIDs.Count != 0;

        if (selectable)
        {
            selectable = ImGui.Selectable("##FCCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

		FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, fc.Tag);
        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, fc.FCName);
        ImGui.SameLine(0, 10);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"· {fc.WorldName} · {fc.MasterString}");
        if (selectable)
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterAvailable ? fc.MasterString : Configuration.Instance.GatheredData.CharByCID[fc.MemberCIDs.First()].Name, fc.WorldName);

		ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetFCPointColor(fc.FCPoints), fc.FCPoints.ToString("N0"));

        ImGui.TableNextColumn();
    }
}
