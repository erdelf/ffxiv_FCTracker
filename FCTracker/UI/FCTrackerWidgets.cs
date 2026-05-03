namespace FCTracker.UI;

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

public static class FCTrackerWidgets
{
    public static void ColoredText(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(text);
        }
    }

    public static void Icon(Vector4 color, FontAwesomeIcon icon)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(icon.ToIconString());
        }
    }

    public static void IconLabel(Vector4 iconColor, FontAwesomeIcon icon, string label, Vector4? labelColor = null)
    {
        Icon(iconColor, icon);
        ImGui.SameLine();
        ColoredText(labelColor ?? iconColor, label);
    }
}
