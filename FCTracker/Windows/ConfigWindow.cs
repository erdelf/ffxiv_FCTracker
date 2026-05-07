namespace FCTracker.Windows;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FCTracker.UI;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using System;
using System.Numerics;
using NightmareUI.Censoring;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow() : base("FC Tracker — Settings###FCTrackerConfigWindow",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Size = new Vector2(520, 420);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public override void Draw()
    {
        using (FCTrackerTheme.Push())
        {
            DrawHeader();

            using var body = ImRaii.Child("##ConfigBody", Vector2.Zero, false);
            if (!body.Success) return;

            
            ImGui.SetCursorPos(new Vector2(14, 12));
            /*
            FCTrackerWidgets.IconLabel(FCTrackerTheme.TextSecondary, FontAwesomeIcon.Cogs, "No settings yet — coming soon.");
            ImGui.SetCursorPosX(14);*/

            FCTrackerWidgets.Checkbox("Scramble Names", ref Censor.Config.Enabled);
        }
    }

    private static void DrawHeader()
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundHeader))
        {
            using var headerChild = ImRaii.Child("##ConfigHeader", new Vector2(0, 42), true, ImGuiWindowFlags.NoScrollbar);
            if (!headerChild.Success) return;

            ImGui.SetCursorPos(new Vector2(14, 11));

            FCTrackerWidgets.Icon(FCTrackerTheme.AccentBlue, FontAwesomeIcon.Cog);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, "Settings");

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, "  Configure FC Tracker");
        }

        ImGui.Spacing();
    }
}
