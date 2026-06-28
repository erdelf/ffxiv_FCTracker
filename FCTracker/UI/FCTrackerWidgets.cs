namespace FCTracker.UI;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons.Interop;
using System.Collections.Generic;
using System.Numerics;

public static class FCTrackerWidgets
{
    public static void ColoredText(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(text);
    }

    public static void Icon(Vector4 color, FontAwesomeIcon icon)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        using (ImRaii.PushFont(UiBuilder.IconFont))
            ImGui.TextUnformatted(icon.ToIconString());
    }

    public static void IconLabel(Vector4 iconColor, FontAwesomeIcon icon, string label, Vector4? labelColor = null)
    {
        Icon(iconColor, icon);
        ImGui.SameLine();
        ColoredText(labelColor ?? iconColor, label);
    }

    public static bool Checkbox(ImU8String label, ref bool enabled)
    {
        using (ImRaii.PushColor(ImGuiCol.FrameBg, FCTrackerTheme.BackgroundHover))
        using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, FCTrackerTheme.BackgroundSelected))
        using (ImRaii.PushColor(ImGuiCol.CheckMark, FCTrackerTheme.AccentBlue))
        using (ImRaii.PushColor(ImGuiCol.Border, FCTrackerTheme.AccentBlueDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            return ImGui.Checkbox(label, ref enabled);
    }

    public static bool Button(string text, Vector4? color = null, Vector4? textColor = null)
    {
        using (ImRaii.PushColor(ImGuiCol.Button, color ?? FCTrackerTheme.ButtonDefault))
        using (ImRaii.PushColor(ImGuiCol.Text, textColor ?? FCTrackerTheme.TextPrimary))
            return ImGui.Button(text);
    }

    public static bool IconButton(FontAwesomeIcon icon, string id, Vector4? color = null, Vector4? textColor = null)
    {
        using (ImRaii.PushColor(ImGuiCol.Button, color ?? FCTrackerTheme.ButtonDefault))
        using (ImRaii.PushColor(ImGuiCol.Text, textColor ?? FCTrackerTheme.TextPrimary))
        using (ImRaii.PushFont(UiBuilder.IconFont))
            return ImGui.Button($"{icon.ToIconString()}###{id}", new Vector2(28, 0));
    }

    public static void Tooltip(string tooltip)
    {
        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
                {
                    ImGui.Text(tooltip);
                }
            }
        }
    }

    public static void TableHeadersRowWithTooltips(IReadOnlyDictionary<string, string> tooltips)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        int columnCount = ImGui.TableGetColumnCount();
        for (int column = 0; column < columnCount; column++)
        {
            if (!ImGui.TableSetColumnIndex(column))
                continue;

            string name = ImGui.TableGetColumnName(column);

            ImGui.PushID(column);

            if (tooltips.TryGetValue(name, out string? tip))
            {
                ImGui.TextUnformatted(name);
                ImGui.SameLine(0, 4);
                ImGuiComponents.HelpMarker(tip);
            }
            else
            {
                ImGui.TableHeader(name);
            }

            ImGui.PopID();
        }
    }
}