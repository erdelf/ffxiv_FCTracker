namespace FCTracker.UI.Views;

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

        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingFixedFit;
        using var table = ImRaii.Table("##ReadyTable", 3, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 75);
        ImGui.TableSetupColumn("FC", ImGuiTableColumnFlags.WidthFixed, 540);
        ImGui.TableSetupColumn("##Spacer", ImGuiTableColumnFlags.WidthStretch);

        foreach (FCData fc in readyFCs)
            DrawRow(fc);
    }

    private static void DrawBannerHeader(int count)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.AccentGreenDim))
        {
            using var banner = ImRaii.Child("##ReadyHeader", new Vector2(ImGui.GetContentRegionAvail().X - 28, 40), true);
            if (!banner.Success) return;

            ImGui.SetCursorPos(new Vector2(14, 10));
            FCTrackerWidgets.IconLabel(FCTrackerTheme.AccentGreen, FontAwesomeIcon.CheckCircle,
                $"{count} Free {(count == 1 ? "Company" : "Companies")} Ready for Housing");
        }
    }

    private static void DrawRow(FCData fc)
    {
        int daysEligible = fc.DaysSinceFounded - 30;

        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, fc.LoggedIn ? ImGui.GetColorU32(FCTrackerTheme.RowHighlightColor) : ImGui.GetColorU32(FCTrackerTheme.AccentGreenDim));

        ImGui.TableNextColumn();

		Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(new Vector2(screenPos.X + 4, screenPos.Y + 7), 4, ImGui.GetColorU32(FCTrackerTheme.AccentGreen));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentGreen, "READY");

        ImGui.TableNextColumn();

        bool selectable = fc.MemberCIDs.Count != 0;

        if (selectable)
        {
            ImGui.Selectable("##FCCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

		FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, fc.Tag);
        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, fc.FCName);
        ImGui.SameLine(0, 10);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"· {fc.WorldName} · {fc.MasterString}");
        if (daysEligible >= 0)
        {
            ImGui.SameLine(0, 10);
            FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentGreen, $"+{daysEligible}d");
        }

        if (selectable && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterAvailable ? fc.MasterString : Configuration.Instance.GatheredData.CharByCID[fc.MemberCIDs.First()].Name, fc.WorldName);

		ImGui.TableNextColumn();
    }
}
