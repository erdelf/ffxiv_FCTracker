namespace FCTracker.UI.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

public class UpcomingView : IFCView
{
    public string Id => "upcoming";

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx) =>
        ("Upcoming Eligibility", $"{ctx.Data.GetUpcomingFCs().Count} FCs pending");

    public void Draw(FCViewContext ctx)
    {
        using var scrollArea = ImRaii.Child("##UpcomingScroll", Vector2.Zero, false);
        if (!scrollArea.Success) return;

        ImGui.SetCursorPos(new Vector2(14, 12));

        DrawReadyBanner(ctx);
        DrawTimeline(ctx);
    }

    private static void DrawReadyBanner(FCViewContext ctx)
    {
        int readyCount = ctx.Data.GetReadyCount();
        if (readyCount == 0) return;

        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.AccentGreenDim))
        using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(FCTrackerTheme.AccentGreen.X, FCTrackerTheme.AccentGreen.Y, FCTrackerTheme.AccentGreen.Z, 0.3f)))
        {
            using var banner = ImRaii.Child("##ReadyBanner", new Vector2(ImGui.GetContentRegionAvail().X - 28, 36), true);
            if (!banner.Success) return;

            ImGui.SetCursorPos(new Vector2(12, 8));

            FCTrackerWidgets.IconLabel(FCTrackerTheme.AccentGreen, FontAwesomeIcon.CheckCircle,
                $"{readyCount} FC{(readyCount != 1 ? "s" : "")} ready for housing");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 60);

            using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.AccentGreen))
            using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0)))
            using (ImRaii.PushColor(ImGuiCol.ButtonHovered, FCTrackerTheme.AccentGreenDim))
            {
                if (ImGui.SmallButton("View"))
                {
                    ctx.Sidebar.SetView("ready");
                }
            }
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12);
    }

    private static void DrawTimeline(FCViewContext ctx)
    {
        IReadOnlyList<FCData> upcomingFCs = ctx.Data.GetUpcomingFCs();
        if (upcomingFCs.Count == 0)
        {
            ImGui.SetCursorPos(new Vector2(14, ImGui.GetCursorPosY() + 20));
            FCTrackerWidgets.IconLabel(FCTrackerTheme.TextSecondary, FontAwesomeIcon.CalendarAlt,
                "No upcoming FCs in the next 30 days.");
            return;
        }

        DateTime now = DateTime.Now;
        DateTime thisWeekEnd = now.AddDays(7 - (int)now.DayOfWeek);
        DateTime nextWeekEnd = thisWeekEnd.AddDays(7);

        List<FCData> thisWeek = upcomingFCs.Where(fc => fc.EligibilityDate <= thisWeekEnd).ToList();
        List<FCData> nextWeek = upcomingFCs.Where(fc => fc.EligibilityDate > thisWeekEnd && fc.EligibilityDate <= nextWeekEnd).ToList();
        List<IGrouping<string, FCData>> later = upcomingFCs.Where(fc => fc.EligibilityDate > nextWeekEnd)
            .GroupBy(fc => fc.EligibilityDate.ToString("MMMM yyyy"))
            .ToList();

        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingFixedFit;
        using var table = ImRaii.Table("##UpcomingTable", 4, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 65);
        ImGui.TableSetupColumn("Days", ImGuiTableColumnFlags.WidthFixed, 45);
        ImGui.TableSetupColumn("FC", ImGuiTableColumnFlags.WidthFixed, 500);
        ImGui.TableSetupColumn("##Spacer", ImGuiTableColumnFlags.WidthStretch);

        if (thisWeek.Any()) DrawSection("THIS WEEK", thisWeek);
        if (nextWeek.Any()) DrawSection("NEXT WEEK", nextWeek);
        foreach (IGrouping<string, FCData> monthGroup in later)
        {
            DrawSection(monthGroup.Key.ToUpperInvariant(), monthGroup.ToList());
        }
    }

    private static void DrawSection(string title, List<FCData> fcs)
    {
        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.BackgroundSidebar));

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, title);

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"{fcs.Count}");
        ImGui.TableNextColumn();

        foreach (FCData fc in fcs.OrderBy(f => f.EligibilityDate))
        {
            DrawItem(fc);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(0, 4));
    }

    private static void DrawItem(FCData fc)
    {
        int daysLeft = fc.DaysUntilEligible;
        HousingStatusCategory category = daysLeft <= 3 ? HousingStatusCategory.Ready : 
                                         daysLeft <= 7 ? HousingStatusCategory.Soon : 
                                                         HousingStatusCategory.Waiting;
        Vector4 dotColor = FCTrackerTheme.GetStatusColor(category);

        ImGui.TableNextRow();

        if (daysLeft <= 3)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.AccentYellowDim));

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, fc.EligibilityDate.ToString("MMM dd"));

        ImGui.TableNextColumn();
        Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(new Vector2(screenPos.X + 4, screenPos.Y + 7), 4, ImGui.GetColorU32(dotColor));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(dotColor, daysLeft <= 1 ? "1d" : $"{daysLeft}d");

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, fc.Tag);
        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, fc.FCName);
        ImGui.SameLine(0, 10);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"· {fc.WorldName} · {fc.MasterString}");

        ImGui.TableNextColumn();
    }
}
