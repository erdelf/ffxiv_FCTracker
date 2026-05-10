namespace FCTracker.UI;

using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FCTracker.Services;

public class FCTrackerLayout
{
    public FCTrackerSidebar Sidebar { get; }

    public FCTrackerLayout(IFCDataProvider dataProvider)
    {
        this.Sidebar = new FCTrackerSidebar(dataProvider);
    }

    public void DrawWithHeader(string title, string subtitle, Action renderContent, Action? renderHeaderActions = null)
    {
        this.Sidebar.Draw();
        ImGui.SameLine(0, 0);

        using var contentArea = ImRaii.Child("##FCTrackerContent", Vector2.Zero, false);
        if (!contentArea.Success) return;

        DrawContentHeader(title, subtitle, renderHeaderActions);

        using var contentBody = ImRaii.Child("##ContentBody", Vector2.Zero, false);
        if (!contentBody.Success) return;

        renderContent();
    }

    private static void DrawContentHeader(string title, string subtitle, Action? renderActions)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundHeader))
        {
            using var headerChild = ImRaii.Child("##ContentHeader", new Vector2(0, 42), true, ImGuiWindowFlags.NoScrollbar);
            if (!headerChild.Success) return;

            ImGui.SetCursorPos(new Vector2(14, 11));

            FCTrackerWidgets.Icon(FCTrackerTheme.AccentBlue, FontAwesomeIcon.Building);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, title);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, $"  {subtitle}");

            if (renderActions != null)
            {
                ImGui.SameLine(ImGui.GetContentRegionMax().X - 360);
                renderActions();
            }
        }

        ImGui.Spacing();
    }

    public static void DrawSummaryStrip(params (string Label, int Value, Vector4 DotColor)[] stats)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundCard))
        {
            using var strip = ImRaii.Child("##SummaryStrip", new Vector2(0, 32), true, ImGuiWindowFlags.NoScrollbar);
            if (!strip.Success) 
                return;

            ImGui.SetCursorPos(new Vector2(14, 8));

            bool isFirst = true;
            foreach ((string label, int value, Vector4 dotColor) in stats)
            {
                if (!isFirst)
                    ImGui.SameLine(0, 24);
                isFirst = false;

                DrawStatBadge(label, value, dotColor);
            }
        }
        ImGui.Spacing();
    }

    private static void DrawStatBadge(string label, int value, Vector4 dotColor)
    {
        Vector2 cursorPos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(
            new Vector2(cursorPos.X + 4, cursorPos.Y + 7),
            4,
            ImGui.GetColorU32(dotColor)
        );

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14);

        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, label);
        ImGui.SameLine(0, 4);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, value.ToString());
    }
}
