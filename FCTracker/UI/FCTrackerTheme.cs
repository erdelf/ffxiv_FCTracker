namespace FCTracker.UI;

using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

public static class FCTrackerTheme
{
    public static Vector4 BackgroundDark { get; } = new(0.06f, 0.06f, 0.08f, 1f);
    public static Vector4 BackgroundMid { get; } = new(0.10f, 0.10f, 0.14f, 1f);
    public static Vector4 BackgroundLight { get; } = new(0.14f, 0.14f, 0.18f, 1f);
    public static Vector4 BackgroundCard { get; } = new(0.12f, 0.12f, 0.16f, 1f);
    public static Vector4 BackgroundSidebar { get; } = new(0.08f, 0.08f, 0.10f, 1f);
    public static Vector4 BackgroundHeader { get; } = new(0.13f, 0.13f, 0.17f, 1f);
    public static Vector4 BackgroundHover { get; } = new(0.18f, 0.18f, 0.24f, 1f);
    public static Vector4 BackgroundSelected { get; } = new(0.16f, 0.16f, 0.22f, 1f);

    public static Vector4 Border { get; } = new(0.22f, 0.22f, 0.28f, 1f);
    public static Vector4 BorderDark { get; } = new(0.16f, 0.16f, 0.20f, 1f);

    public static Vector4 TextPrimary { get; } = new(0.88f, 0.88f, 0.90f, 1f);
    public static Vector4 TextSecondary { get; } = new(0.55f, 0.55f, 0.62f, 1f);
    public static Vector4 TextMuted { get; } = new(0.40f, 0.40f, 0.45f, 1f);
    public static Vector4 TextBright { get; } = new(0.95f, 0.95f, 0.97f, 1f);

    public static Vector4 AccentBlue { get; } = new(0.35f, 0.60f, 0.85f, 1f);
    public static Vector4 AccentBlueDim { get; } = new(0.35f, 0.60f, 0.85f, 0.15f);
    public static Vector4 AccentGreen { get; } = new(0.35f, 0.75f, 0.45f, 1f);
    public static Vector4 AccentGreenDim { get; } = new(0.35f, 0.75f, 0.45f, 0.15f);
    public static Vector4 AccentYellow { get; } = new(0.85f, 0.70f, 0.30f, 1f);
    public static Vector4 AccentYellowDim { get; } = new(0.85f, 0.70f, 0.30f, 0.12f);
    public static Vector4 AccentOrange { get; } = new(0.85f, 0.55f, 0.30f, 1f);
    public static Vector4 AccentOrangeDim { get; } = new(0.85f, 0.55f, 0.30f, 0.12f);
    public static Vector4 AccentPurple { get; } = new(0.60f, 0.45f, 0.80f, 1f);
    public static Vector4 AccentPurpleDim { get; } = new(0.60f, 0.45f, 0.80f, 0.12f);
    public static Vector4 AccentRed { get; } = new(0.85f, 0.35f, 0.35f, 1f);

    public static Vector4 ButtonDefault { get; } = new(0.18f, 0.18f, 0.22f, 1f);
    public static Vector4 ButtonHovered { get; } = new(0.24f, 0.24f, 0.30f, 1f);
    public static Vector4 ButtonActive { get; } = new(0.30f, 0.30f, 0.38f, 1f);

    public static IDisposable Push()
    {
        int colorCount = 0;
        int styleCount = 0;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, BackgroundMid); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, BackgroundMid); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.PopupBg, BackgroundLight); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.Border, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.Separator, BorderDark); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.Text, TextPrimary); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, TextMuted); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.Button, ButtonDefault); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ButtonHovered); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ButtonActive); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.Header, BackgroundLight); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, BackgroundHover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, BackgroundSelected); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, BackgroundHeader); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, Border); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableBorderLight, BorderDark); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, BackgroundCard); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, BackgroundLight); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.FrameBg, BackgroundCard); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, BackgroundHover); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, BackgroundSelected); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, BackgroundDark); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, ButtonDefault); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, ButtonHovered); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, ButtonActive); colorCount++;

        ImGui.PushStyleColor(ImGuiCol.Tab, ButtonDefault); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TabHovered, AccentBlueDim); colorCount++;
        ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(AccentBlue.X, AccentBlue.Y, AccentBlue.Z, 0.25f)); colorCount++;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 3f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 3f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4f); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f)); styleCount++;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 4f)); styleCount++;

        return new ThemeScope(colorCount, styleCount);
    }

    private class ThemeScope : IDisposable
    {
        private readonly int colorCount;
        private readonly int styleCount;

        public ThemeScope(int colorCount, int styleCount)
        {
            this.colorCount = colorCount;
            this.styleCount = styleCount;
        }

        public void Dispose()
        {
            ImGui.PopStyleColor(this.colorCount);
            ImGui.PopStyleVar(this.styleCount);
        }
    }

    public static Vector4 GetStatusColor(HousingStatusCategory category) => category switch
    {
        HousingStatusCategory.Ready => AccentGreen,
        HousingStatusCategory.Soon => AccentYellow,
        HousingStatusCategory.Waiting => AccentOrange,
        HousingStatusCategory.Owned => AccentBlue,
        _ => TextSecondary
    };

    public static Vector4 GetStatusColorDim(HousingStatusCategory category) => category switch
    {
        HousingStatusCategory.Ready => AccentGreenDim,
        HousingStatusCategory.Soon => AccentYellowDim,
        HousingStatusCategory.Waiting => AccentOrangeDim,
        HousingStatusCategory.Owned => AccentBlueDim,
        _ => new Vector4(0.5f, 0.5f, 0.5f, 0.1f)
    };

    public static Vector4 GetRankColor(uint rank) => rank switch
    {
        8 => AccentGreen,
        >= 5 => AccentYellow,
        _ => TextSecondary
    };
}
