namespace FCTracker.UI;

using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using FCTracker.Services;

public class FCTrackerLayout(IFCDataProvider dataProvider)
{
    public FCTrackerSidebar Sidebar { get; } = new(dataProvider);

    public void DrawWithHeader(string title, string subtitle, Action renderContent, Action? renderHeaderActions = null)
    {
        this.Sidebar.Draw();
        ImGui.SameLine(0, 0);

        using ImRaii.ChildDisposable contentArea = ImRaii.Child("##FCTrackerContent", Vector2.Zero, false);
        if (!contentArea.Success)
            return;

        DrawContentHeader(title, subtitle, renderHeaderActions);

        using ImRaii.ChildDisposable contentBody = ImRaii.Child("##ContentBody", Vector2.Zero, false);
        if (!contentBody.Success)
            return;

        renderContent();
    }

    private static void DrawContentHeader(string title, string subtitle, Action? renderActions)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundHeader))
        {
            using ImRaii.ChildDisposable headerChild = ImRaii.Child("##ContentHeader", new Vector2(0, 42f), true, ImGuiWindowFlags.NoScrollbar);
            if (!headerChild.Success) 
                return;

            ImGui.SetCursorPos(new Vector2(14f.Scale(), 11));

            FCTrackerWidgets.Icon(FCTrackerTheme.AccentBlue, FontAwesomeIcon.Building);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, title);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, $"  {subtitle}");

            if (renderActions != null)
            {
                ImGui.SameLine(ImGui.GetContentRegionMax().X - 350f.Scale());
                renderActions();
            }
        }

        ImGui.Spacing();
    }

    public static void DrawSummaryStrip(params (string Label, int Value, Vector4 DotColor)[] stats)
        => DrawSummaryStrip(stats, null);

    public static void DrawSummaryStrip(
        (string Label, int Value, Vector4 DotColor)[]                         left,
        (string Label, int Value, Vector4 DotColor, Func<string>? tooltip)[]? right)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundCard))
        {
            using ImRaii.ChildDisposable strip = ImRaii.Child("##SummaryStrip", new Vector2(0, 32), true, ImGuiWindowFlags.NoScrollbar);
            if (!strip.Success)
                return;

            ImGui.SetCursorPos(new Vector2(14, 8));

            bool isFirst = true;
            foreach ((string label, int value, Vector4 dotColor) in left)
            {
                if (!isFirst)
                    ImGui.SameLine(0, 24);
                isFirst = false;

                DrawStatBadge(label, value, dotColor, null);
            }

            if (right is { Length: > 0 })
            {
                float rightWidth = MeasureBadgeGroupWidth(right);
                float posX = ImGui.GetContentRegionMax().X - rightWidth - 14;
                ImGui.SameLine();
                ImGui.SetCursorPosX(posX);

                isFirst = true;
                foreach ((string label, int value, Vector4 dotColor, Func<string> tooltip) in right)
                {
                    if (!isFirst)
                        ImGui.SameLine(0, 24);
                    isFirst = false;

                    DrawStatBadge(label, value, dotColor, tooltip);
                }
            }
        }
        ImGui.Spacing();
    }

    private static float MeasureBadgeGroupWidth((string Label, int Value, Vector4 DotColor, Func<string> tooltip)[] stats)
    {
        float total = 0;
        for (int i = 0; i < stats.Length; i++)
        {
            (string label, int value, _, _) = stats[i];
            float labelW = ImGui.CalcTextSize(label).X;
            float valueW = ImGui.CalcTextSize(value.ToString()).X;
            total += 14 + labelW + 4 + valueW;
            if (i > 0) total += 24;
        }
        return total;
    }

    private static void DrawStatBadge(string label, int value, Vector4 dotColor, Func<string>? tooltip)
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
        bool hovered = ImGui.IsItemHovered();

        ImGui.SameLine(0, 4);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, value.ToString("##,#"));

        hovered |= ImGui.IsItemHovered();
        if(hovered && tooltip != null)
            FCTrackerWidgets.Tooltip(tooltip());
    }
}
