namespace FCTracker.UI.Views;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.IPC;
using NightmareUI.Censoring;

public class AllFCsView : IFCView
{
    public string Id => "all";

    private static readonly Dictionary<string, string> HeaderTooltips = new()
    {
        ["Free Company"] = "House bidding requires your FC to have GC rank 6\n\nClick to log into a char belonging to the FC",
        ["Members"] = "House bidding requires 4 members",
        ["Founded"] = "House bidding requires 30 days to have passed since joining the FC\nFor simplicity it is assumed you joined the FC by creating it",
        ["Status"] = "The state of the house of the FC.\nClick to walk to the house if available",
        ["Demolition"] = "Houses are demolished if not visited in 45 days"
    };

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx)
    {
        IReadOnlyList<FCData> fcs = ctx.GetFilteredFCs();
        int worldCount = fcs.Select(fc => fc.WorldName).Distinct().Count();
        return ("All Free Companies", $"{fcs.Count} FCs across {worldCount} worlds");
    }

    public void Draw(FCViewContext ctx)
    {
        bool all = !ctx.Sidebar.SelectionActive && ctx.SearchText.IsNullOrEmpty();

        IReadOnlyList<FCData> fcs = all ? ctx.Data.GetAllFCs() : ctx.GetFilteredFCs();

        FCTrackerLayout.DrawSummaryStrip(
            left: [
                ("Total:", ctx.Data.GetTotalCount(), FCTrackerTheme.AccentBlue),
                ("Ready:", ctx.Data.GetReadyCount(), FCTrackerTheme.AccentGreen),
                ("Soon:", ctx.Data.GetPending7DayCount(), FCTrackerTheme.AccentYellow),
                ("Pending:", ctx.Data.GetPending30DayCount(), FCTrackerTheme.AccentOrange)
            ],
            right: [
                ("Repair:", all ? Configuration.ARData.RepairCount : fcs.Sum(fcd => fcd.AutoRetainerData?.RepairCount ?? 0), FCTrackerTheme.AccentPurple),
                ("Fuel:",   all ? Configuration.ARData.FuelCount   : fcs.Sum(fcd => fcd.AutoRetainerData?.FuelCount   ?? 0), FCTrackerTheme.AccentRed)
            ]);

        DrawFCTable(ctx);
    }

    private static void DrawFCTable(FCViewContext ctx)
    {
        List<FCData> fcs = ctx.GetFilteredFCs().OrderBy(fc => fc.WorldName).ThenBy(fc => fc.FCName).ToList();

        using var tableChild = ImRaii.Child("##FCTableArea", Vector2.Zero, false);
        if (!tableChild.Success) return;

        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY        |
                                      ImGuiTableFlags.PadOuterX      |
                                      ImGuiTableFlags.SizingFixedFit |
                                      ImGuiTableFlags.Resizable;

        using var table = ImRaii.Table("##FCTable", 7, flags);
        if (!table.Success)
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Free Company", ImGuiTableColumnFlags.WidthFixed, 260);
        ImGui.TableSetupColumn("Master",       ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Members",      ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Founded",      ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Status",       ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Demolition",   ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("##Spacer",     ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushColor(ImGuiCol.TableHeaderBg, FCTrackerTheme.BackgroundHeader))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.TextSecondary))
            FCTrackerWidgets.TableHeadersRowWithTooltips(HeaderTooltips);

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
        FCTrackerWidgets.IconLabel(FCTrackerTheme.TextPrimary, FCTrackerTheme.GetRegionIcon(FCTrackerTheme.RegionString(fc.World)), Censor.World(fc.WorldName));

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

        if(fc.LoggedIn)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, FCTrackerTheme.RowHighlightColor);

        if (fc.GetStatusCategory() == HousingStatusCategory.Ready)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.AccentGreenDim));

        ImGui.TableNextColumn();
        DrawFCNameCell(fc);

        ImGui.TableNextColumn();

        bool masterSelectable = fc.SourceData.ImportSourceConfig == null && fc.MasterAvailable;

        if (masterSelectable)
        {
            ImGui.Selectable("##FCMasterCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary,  Censor.Character(fc.MasterString));

        if (masterSelectable && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterString, fc.WorldName);
        
        ImGui.TableNextColumn();
        Vector4 memberColor = fc.TotalMembers <= 1 ? FCTrackerTheme.TextMuted :
                              fc.TotalMembers > 10 ? FCTrackerTheme.AccentPurple : FCTrackerTheme.TextSecondary;
        FCTrackerWidgets.ColoredText(memberColor, fc.TotalMembers.ToString());

        ImGui.TableNextColumn();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, fc.FoundingDate == default ? "—" : $"{fc.DaysSinceFounded}d ago");

        ImGui.TableNextColumn();
        DrawStatusCell(fc);

        ImGui.TableNextColumn();

        DrawDemolitionCell(fc);
        ImGui.TableNextColumn();
    }

    private static void DrawFCNameCell(FCData fc)
    {
        bool selectable = fc.SourceData.ImportSourceConfig == null && fc.MemberCIDs.Count != 0;

        if (selectable)
        {
            ImGui.Selectable("##FCNameCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetRankColor(fc.Rank), $"{fc.Rank}");

        ImGui.SameLine(0, 8);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.AccentBlue, Censor.Hide(fc.Tag, FCTrackerPlugin.ScrambleTag));

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, Censor.Character(fc.FCName));

        if (selectable && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterAvailable ? fc.MasterString : Configuration.Instance.GatheredData.CharByCID[fc.MemberCIDs.First()].Name, fc.WorldName);
    }

    private static void DrawStatusCell(FCData fc)
    {
        bool selectable = fc.SourceData.ImportSourceConfig == null && fc.MemberCIDs.Count != 0 && fc.HasHouse;

        if (selectable)
        {
            ImGui.Selectable("##FCStatusCell" + fc.Id);
            ImGui.SetItemAllowOverlap();
            ImGui.SameLine(0, 0);
        }

        Vector4 color = FCTrackerTheme.GetStatusColor(fc.GetStatusCategory(), false);

        Vector2 cursorPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(
            new Vector2(cursorPos.X + 4, cursorPos.Y + 8),
            4,
            ImGui.GetColorU32(color)
        );

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(color, (fc.HasHouse ? Censor.Hide(fc.GetHousingStatusText(), "House owned") : fc.GetHousingStatusText()));

        void TeleportToFCHouse()
        {
            if(fc.MemberCIDs.Contains(Player.CID))
                ECommonsIPC.Lifestream.TeleportToFC();
            else
                ECommonsIPC.Lifestream.GoToHousingAddress(($"{fc.WorldName}-{fc.Id}", (int) fc.HomeWorldId, (int)fc.House.City, fc.House.Ward+1, 0, fc.House.Plot+1, -1, false, false, string.Empty));
        }

        if (selectable && ImGui.IsItemClicked(ImGuiMouseButton.Left))
            if(Svc.ClientState.IsLoggedIn)
            {
                TeleportToFCHouse();
            }
            else
            {
                TaskManager taskManager = FCTrackerPlugin.Plugin.TaskManager;

                taskManager.Enqueue(() => ECommonsIPC.Lifestream.ChangeCharacter(fc.MasterAvailable ? fc.MasterString : Configuration.Instance.GatheredData.CharByCID[fc.MemberCIDs.First()].Name, fc.WorldName));
                taskManager.EnqueueDelay(100);
                taskManager.Enqueue(() => !ECommonsIPC.Lifestream.IsBusy());
                taskManager.EnqueueDelay(100);
                taskManager.Enqueue(() => Svc.ClientState.IsLoggedIn);
                taskManager.Enqueue(() => PlayerHelper.IsReady);
                taskManager.Enqueue(TeleportToFCHouse);
            }
    }

    private static void DrawDemolitionCell(FCData fc)
    {
        if (!fc.HasHouse)
        {
            ImGui.Text("—");
            return;
        }

        string  text  = fc.GetHousingDemolitionText();
        Vector4 color = FCTrackerTheme.GetStatusColor(fc.House!.GetVisitationStatus(), true);

        FCTrackerWidgets.ColoredText(color, text);
    }
}