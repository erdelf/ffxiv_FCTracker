namespace FCTracker.UI.Views;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.IPC;
using NightmareUI.Censoring;

public class AllFCsView : IFCView
{
    public string Id => "all";

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx)
    {
        IReadOnlyList<FCData> fcs = ctx.GetFilteredFCs();
        int worldCount = fcs.Select(fc => fc.WorldName).Distinct().Count();
        return ("All Free Companies", $"{fcs.Count} FCs across {worldCount} worlds");
    }

    public void Draw(FCViewContext ctx)
    {
        FCTrackerLayout.DrawSummaryStrip(
            ("Total:", ctx.Data.GetTotalCount(), FCTrackerTheme.AccentBlue),
            ("Ready:", ctx.Data.GetReadyCount(), FCTrackerTheme.AccentGreen),
            ("Soon:", ctx.Data.GetPending7DayCount(), FCTrackerTheme.AccentYellow),
            ("Pending:", ctx.Data.GetPending30DayCount(), FCTrackerTheme.AccentOrange)
        );

        DrawFCTable(ctx);
    }

    private static void DrawFCTable(FCViewContext ctx)
    {
        List<FCData> fcs = ctx.GetFilteredFCs().OrderBy(fc => fc.WorldName).ThenBy(fc => fc.FCName).ToList();

        using var tableChild = ImRaii.Child("##FCTableArea", Vector2.Zero, false);
        if (!tableChild.Success) return;

        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY |
                                      ImGuiTableFlags.PadOuterX |
                                      ImGuiTableFlags.SizingFixedFit |
                                      ImGuiTableFlags.Resizable;

        using var table = ImRaii.Table("##FCTable", 6, flags);
        if (!table.Success) 
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Free Company", ImGuiTableColumnFlags.WidthFixed, 260);
        ImGui.TableSetupColumn("Master", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Members", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Founded", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("##Spacer", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushColor(ImGuiCol.TableHeaderBg, FCTrackerTheme.BackgroundHeader))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.TextSecondary))
        {
            ImGui.TableHeadersRow();
        }

        string? currentWorld = null;

        foreach (FCData fc in fcs)
        {
            if (fc.WorldName != currentWorld)
            {
                currentWorld = fc.WorldName;
                DrawWorldGroupHeader(ctx, fc);
            }

            DrawFCRow(fc);
        }
    }

    private static void DrawWorldGroupHeader(FCViewContext ctx, FCData fc)
    {
        int worldCount = ctx.Data.GetFCCountForWorld(fc.WorldName);

        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.BackgroundSidebar));

        ImGui.TableNextColumn();
        FCTrackerWidgets.IconLabel(FCTrackerTheme.TextPrimary, FontAwesomeIcon.Globe, Censor.World(fc.WorldName));

        if (!string.IsNullOrEmpty(fc.Datacenter))
        {
            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"·  {fc.Datacenter}");
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"{worldCount}");

        ImGui.TableNextColumn();
    }

    private static void DrawFCRow(FCData fc)
    {
        ImGui.TableNextRow();

        if (fc.GetStatusCategory() == HousingStatusCategory.Ready)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.AccentGreenDim));

        ImGui.TableNextColumn();
        DrawFCNameCell(fc);

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary,  Censor.Character(fc.MasterString));

        ImGui.TableNextColumn();
        Vector4 memberColor = fc.TotalMembers <= 1 ? FCTrackerTheme.TextMuted :
                              fc.TotalMembers > 10 ? FCTrackerTheme.AccentPurple : FCTrackerTheme.TextSecondary;
        FCTrackerWidgets.ColoredText(memberColor, fc.TotalMembers.ToString());

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, fc.FoundingDate == default ? "—" : $"{fc.DaysSinceFounded}d ago");

        ImGui.TableNextColumn();
        DrawStatusCell(fc);

        ImGui.TableNextColumn();
    }

    private const string ScrambleTag = "« »";

    private static void DrawFCNameCell(FCData fc)
    {
        ImGui.Selectable("##FCNameCell" + fc.Id);
        ImGui.SetItemAllowOverlap();
        ImGui.SameLine(0, 0);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetRankColor(fc.Rank), $"{fc.Rank}");

        ImGui.SameLine(0, 8);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, Censor.Hide(fc.Tag, ScrambleTag));

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, Censor.Character(fc.FCName));

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterString, fc.WorldName);
    }

    private static void DrawStatusCell(FCData fc)
    {
        bool clickable = Svc.ClientState.IsLoggedIn && fc.HasHouse;

        if (clickable)
        {
            ImGui.Selectable("##FCStatusCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

        Vector4 color = FCTrackerTheme.GetStatusColor(fc.GetStatusCategory());

        Vector2 cursorPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(
            new Vector2(cursorPos.X + 4, cursorPos.Y + 8),
            4,
            ImGui.GetColorU32(color)
        );

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(color, fc.HasHouse ? Censor.Hide(fc.GetHousingStatusText(), "House owned") : fc.GetHousingStatusText());

        if (clickable && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ECommonsIPC.Lifestream.GoToHousingAddress(($"{fc.WorldName}-{fc.Id}", (int) fc.HomeWorldId, (int)fc.House.Value.City, fc.House.Value.Ward+1, 0, fc.House.Value.Plot+1, -1, false, false, string.Empty));
    }
}