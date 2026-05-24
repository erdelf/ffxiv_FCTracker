namespace FCTracker.UI;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FCTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class FCTrackerSidebar(IFCDataProvider dataProvider)
{
    private readonly Dictionary<string, bool> regionExpandedState = new();
    private readonly Dictionary<string, bool> dcExpandedState     = new();

    public string ActiveViewId { get; private set; } = "all";
    public string? SelectedRegion { get; private set; }
    public string? SelectedDatacenter { get; private set; }
    public string? SelectedWorld { get; private set; }

    private const float SidebarWidth = 200f;

    public void Draw()
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundSidebar))
        {
            using var sidebar = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth, 0), true);
            if (!sidebar.Success) return;

            this.DrawSectionLabel("VIEWS");
            this.DrawViewItem("All FCs", FontAwesomeIcon.List, "all", dataProvider.GetTotalCount());
            this.DrawViewItem("Upcoming", FontAwesomeIcon.Clock, "upcoming", dataProvider.GetUpcomingCount(),
                dataProvider.GetUpcomingCount() > 0 ? FCTrackerTheme.AccentYellow : null);
            this.DrawViewItem("Ready Now", FontAwesomeIcon.Check, "ready", dataProvider.GetReadyCount(),
                dataProvider.GetReadyCount() > 0 ? FCTrackerTheme.AccentGreen : null);
            this.DrawViewItem("Characters", FontAwesomeIcon.User, "chars", dataProvider.CharData().GetAllCharsWithoutFC().Count,
                dataProvider.CharData().GetAllCharsWithoutFC().Count > 0 ? FCTrackerTheme.AccentOrange : null);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            this.DrawSectionLabel("REGIONS");
            this.DrawRegionsTree();
        }
    }

    private void DrawSectionLabel(string label)
    {
        ImGui.SetCursorPos(new Vector2(12, ImGui.GetCursorPosY() + 4));
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, label);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
    }

    private void DrawViewItem(string label, FontAwesomeIcon icon, string viewId, int count, Vector4? countColor = null)
    {
        bool isActive = this.ActiveViewId == viewId && this.SelectedWorld == null && this.SelectedDatacenter == null && this.SelectedRegion == null;

        ImGui.SetCursorPosX(0);

        using (ImRaii.PushColor(ImGuiCol.Header, isActive ? FCTrackerTheme.BackgroundSelected : new Vector4(0, 0, 0, 0)))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
        {
            if (ImGui.Selectable($"##{viewId}", isActive, ImGuiSelectableFlags.None, new Vector2(SidebarWidth, 22)))
            {
                this.ActiveViewId = viewId;
                this.SelectedRegion = null;
                this.SelectedDatacenter = null;
                this.SelectedWorld = null;
            }
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 22);

        if (isActive) 
            DrawActiveIndicator(22);

        ImGui.SetCursorPosX(12);
        FCTrackerWidgets.IconLabel(
            isActive ? FCTrackerTheme.AccentBlue : FCTrackerTheme.TextSecondary,
            icon, label,
            isActive ? FCTrackerTheme.TextBright : FCTrackerTheme.TextPrimary);

        ImGui.SameLine(SidebarWidth - 36);
        DrawCountBadge(count, countColor ?? FCTrackerTheme.TextMuted);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
    }

    private void DrawRegionsTree()
    {
        Dictionary<string, List<string>> dcsByRegion = dataProvider.GetDatacentersByRegion();
        Dictionary<string, List<string>> worldsByDc = dataProvider.GetWorldsByDatacenter();

        foreach (string region in dcsByRegion.Keys.OrderBy(r => r == "NA" ? 0 : r == "EU" ? 1 : r == "JP" ? 2 : 3))
        {
            (int done, int ready, int upcoming, int total) = dataProvider.GetStatusCountsForRegion(region);
            if (total == 0) 
                continue;
            this.DrawRegion(region, dcsByRegion[region], worldsByDc, done, ready, upcoming, total);
        }
    }

    private void DrawRegion(string region, List<string> dcs, Dictionary<string, List<string>> worldsByDc, int done, int ready, int upcoming, int total)
    {
        bool            isExpanded = this.regionExpandedState.GetValueOrDefault(region, true);
        bool            isActive   = this.SelectedRegion == region && this.SelectedDatacenter == null && this.SelectedWorld == null;
        string          regionName = FCTrackerTheme.GetRegionDisplayName(region);
        FontAwesomeIcon regionIcon = FCTrackerTheme.GetRegionIcon(region);

        ImGui.SetCursorPosX(0);

        Vector4 bgColor = isActive ? FCTrackerTheme.BackgroundSelected : GetRegionBackgroundColor(region);
        using (ImRaii.PushColor(ImGuiCol.Header, bgColor))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
            if (ImGui.Selectable($"##Region{region}", isActive, ImGuiSelectableFlags.None, new Vector2(SidebarWidth, 22)))
            {
                if(isActive)
                {
                    this.regionExpandedState[region] = !isExpanded;
                }
                else
                {
                    this.ActiveViewId                = "all";
                    this.SelectedRegion              = region;
                    this.SelectedDatacenter          = null;
                    this.SelectedWorld               = null;
                    this.regionExpandedState[region] = true;
                }
            }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 22);

        if (isActive)
            DrawActiveIndicator(22);

        ImGui.SetCursorPosX(8);
        ImGui.SetItemAllowOverlap();
        using (ImRaii.PushColor(ImGuiCol.Header, bgColor))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
            if (ImGui.Selectable($"##RegionChevron{region}", false, ImGuiSelectableFlags.None, new Vector2(14, 14)))
                this.regionExpandedState[region] = !isExpanded;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 22);
        ImGui.SetCursorPosX(8);
        FCTrackerWidgets.Icon(FCTrackerTheme.TextMuted, isExpanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight);

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.Icon(isActive ? FCTrackerTheme.AccentBlue : FCTrackerTheme.TextSecondary, regionIcon);

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(isActive ? FCTrackerTheme.TextBright : FCTrackerTheme.TextPrimary, regionName);

        ImGui.SameLine(SidebarWidth - 50);
        DrawStatusDot(ready, upcoming);

        ImGui.SameLine(SidebarWidth - 36);
        DrawCountBadge(total, FCTrackerTheme.TextMuted);

        ImGui.SetCursorPosX(20);
        DrawProgressBar(done, ready, upcoming, total);


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);

        if (isExpanded)
        {
            foreach (string dc in dcs)
            {
                (int Done, int Ready, int Upcoming, int Total) dcStats = dataProvider.GetStatusCountsForDatacenter(dc);
                if (dcStats.Total == 0) 
                    continue;
                this.DrawDatacenter(dc, worldsByDc, dcStats.Done, dcStats.Ready, dcStats.Upcoming, dcStats.Total);
            }
        }
    }

    private void DrawDatacenter(string dc, Dictionary<string, List<string>> worldsByDc, int done, int ready, int upcoming, int total)
    {
        bool isExpanded = this.dcExpandedState.GetValueOrDefault(dc, false);
        bool isActive = this.SelectedDatacenter == dc && this.SelectedWorld == null;

        ImGui.SetCursorPosX(0);

        Vector4 bgColor = isActive ? FCTrackerTheme.BackgroundSelected : new Vector4(0, 0, 0, 0);
        using (ImRaii.PushColor(ImGuiCol.Header, bgColor))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
        {
            if (ImGui.Selectable($"##DC{dc}", isActive, ImGuiSelectableFlags.None, new Vector2(SidebarWidth, 20)))
            {
                if(isActive)
                {
                    this.dcExpandedState[dc] = !isExpanded;
                }
                else
                {
                    this.ActiveViewId        = "all";
                    this.SelectedRegion      = null;
                    this.SelectedDatacenter  = dc;
                    this.SelectedWorld       = null;
                    this.dcExpandedState[dc] = true;
                }
            }
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);

        if (isActive) 
            DrawActiveIndicator(20);

        ImGui.SetCursorPosX(20);
        ImGui.SetItemAllowOverlap();
        using (ImRaii.PushColor(ImGuiCol.Header, bgColor))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
            if (ImGui.Selectable($"##DatacenterChevron{dc}", false, ImGuiSelectableFlags.None, new Vector2(14, 14)))
                this.dcExpandedState[dc] = !isExpanded;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 22);
        ImGui.SetCursorPosX(20);
        FCTrackerWidgets.Icon(FCTrackerTheme.TextMuted, isExpanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight);

        ImGui.SameLine(0, 6);
        FCTrackerWidgets.ColoredText(isActive ? FCTrackerTheme.TextBright : FCTrackerTheme.TextSecondary, dc);

        ImGui.SameLine(SidebarWidth - 50);
        DrawStatusDot(ready, upcoming);

        ImGui.SameLine(SidebarWidth - 36);
        DrawCountBadge(total, FCTrackerTheme.TextMuted);

        ImGui.SetCursorPosX(20);
        DrawProgressBar(done, ready, upcoming, total);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);

        if (isExpanded && worldsByDc.TryGetValue(dc, out List<string>? worlds))
        {
            foreach (string world in worlds)
            {
                (int Done, int Ready, int Upcoming, int Total) ws = dataProvider.GetStatusCountsForWorld(world);
                if (ws.Total == 0) continue;
                this.DrawWorld(world, ws.Done, ws.Ready, ws.Upcoming, ws.Total);
            }
        }
    }

    private void DrawWorld(string world, int done, int ready, int upcoming, int total)
    {
        bool isActive = this.SelectedWorld == world;

        ImGui.SetCursorPosX(0);

        using (ImRaii.PushColor(ImGuiCol.Header, isActive ? FCTrackerTheme.BackgroundSelected : new Vector4(0, 0, 0, 0)))
        using (ImRaii.PushColor(ImGuiCol.HeaderHovered, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.HeaderActive, FCTrackerTheme.BackgroundSelected))
        {
            if (ImGui.Selectable($"##{world}World", isActive, ImGuiSelectableFlags.None, new Vector2(SidebarWidth, 18)))
            {
                this.ActiveViewId = "all";
                this.SelectedWorld = world;
            }
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 18);

        if (isActive) 
            DrawActiveIndicator(18);

        ImGui.SetCursorPosX(34);
        DrawStatusDot(ready, upcoming, 3);

        ImGui.SetCursorPosX(44);
        FCTrackerWidgets.ColoredText(isActive ? FCTrackerTheme.TextBright : FCTrackerTheme.TextPrimary, world);

        ImGui.SameLine(SidebarWidth - 36);
        DrawCountBadge(total, FCTrackerTheme.TextMuted);

        if (total > 1)
        {
            ImGui.SetCursorPosX(20);
            DrawProgressBar(done, ready, upcoming, total);
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);
    }

    private static void DrawActiveIndicator(float height)
    {
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 windowPos = ImGui.GetWindowPos();
        float cursorY = ImGui.GetCursorPosY();
        drawList.AddRectFilled(
            windowPos with { Y = windowPos.Y + cursorY },
            new Vector2(windowPos.X + 3, windowPos.Y + cursorY + height),
            ImGui.GetColorU32(FCTrackerTheme.AccentBlue)
        );
    }

    private static void DrawStatusDot(int ready, int upcoming, float radius = 4)
    {
        Vector4 color;
        if (ready > 0) 
            color = FCTrackerTheme.AccentGreen;
        else if (upcoming > 0) 
            color = FCTrackerTheme.AccentYellow;
        else 
            return;

        Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(new Vector2(screenPos.X + radius, screenPos.Y + 7), radius, ImGui.GetColorU32(color));
    }

    private static void DrawCountBadge(int count, Vector4 color)
    {
        string text = count.ToString();
        Vector2 textSize = ImGui.CalcTextSize(text);
        const float padding = 4f;
        float badgeWidth = Math.Max(textSize.X + padding * 2, 20);

        Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();

        drawList.AddRectFilled(
            screenPos,
            new Vector2(screenPos.X + badgeWidth, screenPos.Y + 16),
            ImGui.GetColorU32(color with { W = 0.15f }),
            8f
        );

        float textOffset = (badgeWidth - textSize.X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textOffset);
        FCTrackerWidgets.ColoredText(color, text);
    }

    private static void DrawProgressBar(int done, int ready, int upcoming, int total)
    {
        if (total == 0 || done == total)
            return;

        int pending = total - done - ready - upcoming;
        const float barWidth = SidebarWidth - 56;
        const float barHeight = 3f;
        Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();

        drawList.AddRectFilled(
            screenPos,
            new Vector2(screenPos.X + barWidth, screenPos.Y + barHeight),
            ImGui.GetColorU32(FCTrackerTheme.BackgroundLight),
            2f
        );

        float xOffset = 0f;

        total -= done;

        if (ready > 0)
        {
            float w = (float) ready / total * barWidth;
            drawList.AddRectFilled(
                screenPos with { X = screenPos.X + xOffset },
                new Vector2(screenPos.X + xOffset + w, screenPos.Y + barHeight),
                ImGui.GetColorU32(FCTrackerTheme.AccentGreen), 2f);
            xOffset += w;
        }

        if (upcoming > 0)
        {
            float w = (float) upcoming / total * barWidth;
            drawList.AddRectFilled(
                screenPos with { X = screenPos.X + xOffset },
                new Vector2(screenPos.X + xOffset + w, screenPos.Y + barHeight),
                ImGui.GetColorU32(FCTrackerTheme.AccentYellow), 2f);
            xOffset += w;
        }

        if (pending > 0)
        {
            float w = (float) pending / total * barWidth;
            drawList.AddRectFilled(
                screenPos with { X = screenPos.X + xOffset },
                new Vector2(screenPos.X + xOffset + w, screenPos.Y + barHeight),
                ImGui.GetColorU32(FCTrackerTheme.AccentOrange), 2f);
        }

        ImGui.Dummy(new Vector2(barWidth, barHeight));
    }

    private static Vector4 GetRegionBackgroundColor(string region) => region switch
    {
        "NA" => new Vector4(0.2f, 0.4f, 0.8f, 0.08f),
        "EU" => new Vector4(0.8f, 0.6f, 0.2f, 0.08f),
        "JP" => new Vector4(0.8f, 0.3f, 0.3f, 0.08f),
        "OCE" => new Vector4(0.3f, 0.7f, 0.5f, 0.08f),
        _ => new Vector4(0, 0, 0, 0)
    };

    public bool SelectionActive => this.SelectedRegion != null || this.SelectedDatacenter != null || this.SelectedWorld != null;

    public void ClearSelection()
    {
        this.SelectedRegion = null;
        this.SelectedDatacenter = null;
        this.SelectedWorld = null;
    }

    public void SetView(string viewId)
    {
        this.ActiveViewId = viewId;
        this.SelectedRegion = null;
        this.SelectedDatacenter = null;
        this.SelectedWorld = null;
    }
}
