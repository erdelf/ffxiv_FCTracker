namespace FCTracker.UI.Views;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.IPC;
using NightmareUI.Censoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class UpcomingView : IFCView
{
    public string Id => "upcoming";

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx) =>
        ("Upcoming Eligibility", $"{ctx.Data.GetUpcomingFCs().Count} FCs pending");

    public void Draw(FCViewContext ctx)
    {
        using var scrollArea = ImRaii.Child("##UpcomingScroll", Vector2.Zero, false);
        if (!scrollArea.Success) 
            return;

        ImGui.SetCursorPos(new Vector2(14, 12));

        DrawReadyBanner(ctx);
        DrawTimeline(ctx);
    }

    private static void DrawReadyBanner(FCViewContext ctx)
    {
        int readyCount = ctx.Data.GetReadyCount();
        if (readyCount == 0) return;

        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.AccentGreenDim))
        using (ImRaii.PushColor(ImGuiCol.Border, FCTrackerTheme.AccentGreen with { W = 0.3f }))
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
                    ctx.Sidebar.SetView("ready");
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
                "No upcoming FCs.");
            return;
        }

        DateTime now = DateTime.Now;
        DateTime thisWeekEnd = now.AddDays(7 - (int)now.DayOfWeek);
        DateTime nextWeekEnd = thisWeekEnd.AddDays(7);

		List<FCData> timeDone = upcomingFCs.Where(fc => fc.EligibilityDate <= DateTime.Now).ToList();
		List<FCData> thisWeek = upcomingFCs.Where(fc => fc.EligibilityDate > DateTime.Now && fc.EligibilityDate <= thisWeekEnd).ToList();
        List<FCData> nextWeek = upcomingFCs.Where(fc => fc.EligibilityDate > thisWeekEnd  && fc.EligibilityDate <= nextWeekEnd).ToList();

        List<IGrouping<string, FCData>> later = upcomingFCs.Where(fc => fc.EligibilityDate > nextWeekEnd)
                                                           .GroupBy(fc => fc.FoundingDate != default ? $"In {(fc.EligibilityDate - now).Days / 7 + 1} weeks" : "Unregistered")
                                                           .ToList();

        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY        |
                                      ImGuiTableFlags.PadOuterX      |
                                      ImGuiTableFlags.SizingFixedFit |
                                      ImGuiTableFlags.Resizable;

        using ImRaii.TableDisposable table = ImRaii.Table("##UpcomingTable", 5, flags);
        if (!table.Success) 
            return;
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableSetupColumn("Date",     ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableSetupColumn("Days",     ImGuiTableColumnFlags.WidthFixed, 45);
        ImGui.TableSetupColumn("GC",     ImGuiTableColumnFlags.WidthFixed, 45);
		ImGui.TableSetupColumn("FC",       ImGuiTableColumnFlags.WidthFixed, 500);
        ImGui.TableSetupColumn("##Spacer", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushColor(ImGuiCol.TableHeaderBg, FCTrackerTheme.BackgroundHeader))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.TextSecondary))
            ImGui.TableHeadersRow();
        if (timeDone.Count != 0)
            DrawSection("TIME DONE", timeDone);
		if (thisWeek.Count != 0)
            DrawSection("THIS WEEK", thisWeek);
        if (nextWeek.Count != 0)
            DrawSection("NEXT WEEK", nextWeek);

        foreach (IGrouping<string, FCData> monthGroup in later)
            DrawSection(monthGroup.Key.ToUpperInvariant(), monthGroup.ToList());
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
            DrawItem(fc);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(0, 4));
    }

    private static void DrawItem(FCData fc)
    {
        ImGui.TableNextRow();

        if (fc.LoggedIn)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, FCTrackerTheme.RowHighlightColor);

        bool hasDate = fc.FoundingDate != default;

        int daysLeft = fc.DaysUntilEligible;
        HousingStatusCategory category = daysLeft <= 3 ? HousingStatusCategory.Ready :
                                         daysLeft <= 7 ? HousingStatusCategory.Soon :
                                                         HousingStatusCategory.Waiting;

        Vector4 dotColor = FCTrackerTheme.GetStatusColor(category, false);

        if (daysLeft <= 3)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.AccentYellowDim));

        ImGui.TableNextColumn();
        if(hasDate)
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, fc.EligibilityDate.ToString("MMM dd"));

        ImGui.TableNextColumn();
        Vector2       screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList  = ImGui.GetWindowDrawList();

        if(hasDate)
            drawList.AddCircleFilled(new Vector2(screenPos.X + 4, screenPos.Y + 7), 4, ImGui.GetColorU32(dotColor));

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        if(hasDate)
            FCTrackerWidgets.ColoredText(dotColor, daysLeft <= 0 ? "DONE" : $"{daysLeft}d");

        ImGui.TableNextColumn();

		FCTrackerWidgets.ColoredText(FCTrackerTheme.GetRankColor(fc.Rank), $"{fc.Rank}");

		ImGui.TableNextColumn();

		bool selectable = fc.SourceData.ImportSourceConfig == null && fc.MemberCIDs.Count != 0;

        if (selectable)
        {
            selectable = ImGui.Selectable("##FCNameCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, Censor.Hide(fc.Tag, FCTrackerPlugin.ScrambleTag));

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, Censor.Character(fc.FCName));
        ImGui.SameLine(0, 10);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"· {Censor.World(fc.WorldName)} · {Censor.Character(fc.MasterString)}");

        ImGui.TableNextColumn();

        if (selectable)
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterAvailable ? fc.MasterString : Configuration.Instance.GatheredData.CharByCID[fc.MemberCIDs.First()].Name, fc.WorldName);
    }
}
