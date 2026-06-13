namespace FCTracker.Windows;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Interop;
using FCTracker.UI;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using NightmareUI.Censoring;
using System;
using System.Collections.Generic;
using System.Numerics;

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

            using (ImRaii.ChildDisposable body = ImRaii.Child("##ConfigBody", new Vector2(0, 42), false))
            {
                if (!body.Success)
                    return;


                ImGui.SetCursorPos(new Vector2(14, 12));
                /*
                FCTrackerWidgets.IconLabel(FCTrackerTheme.TextSecondary, FontAwesomeIcon.Cogs, "No settings yet — coming soon.");
                ImGui.SetCursorPosX(14);*/

                if(FCTrackerWidgets.Checkbox("Scramble Names", ref Censor.Config.Enabled))
                    Configuration.Instance.Save();
            }
            DrawImportData();
        }
    }

    private static void DrawHeader()
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.BackgroundHeader))
        {
            using ImRaii.ChildDisposable headerChild = ImRaii.Child("##ConfigHeader", new Vector2(0, 42), true, ImGuiWindowFlags.NoScrollbar);
            if (!headerChild.Success) 
                return;

            ImGui.SetCursorPos(new Vector2(14, 11));

            FCTrackerWidgets.Icon(FCTrackerTheme.AccentBlue, FontAwesomeIcon.Cog);

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, "Settings");

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextSecondary, "  Configure FC Tracker");
        }
        ImGui.Spacing();
    }

    private static void DrawImportData()
    {
        using ImRaii.ChildDisposable body = ImRaii.Child("##ConfigBody-ImportData", Vector2.Zero, false);
        if (!body.Success)
            return;

        ImGui.SetCursorPos(new Vector2(14, 8));
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextBright, "Import other accounts data");

        ImGui.Spacing();


        for (int index = 0; index < Configuration.Instance.DataImportConfig.Count; index++)
        {
            ImGui.SetCursorPosX(10);

            DataImportConfig importConfig = Configuration.Instance.DataImportConfig[index];

            bool enabled = importConfig.Enabled;
            if (FCTrackerWidgets.Checkbox($"###DataImport_{index}", ref enabled))
            {
                importConfig.Enabled = enabled;
                Configuration.Instance.RefreshImportedData();
                Configuration.Instance.Save();
            }

            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, $"{index + 1}: {Censor.Hide(importConfig.Path)}");
            ImGui.SameLine();
            if (FCTrackerWidgets.IconButton(FontAwesomeIcon.TrashAlt, FCTrackerTheme.AccentRed with { W = 0.15f }, FCTrackerTheme.AccentRed))
            {
                Configuration.Instance.DataImportConfig.RemoveAt(index);
                index--;

                Configuration.Instance.RefreshImportedData();
                Configuration.Instance.Save();
            }
        }

        ImGui.SetCursorPosX(14);
        if (FCTrackerWidgets.IconButton(FontAwesomeIcon.File, FCTrackerTheme.AccentBlueDim, FCTrackerTheme.AccentBlue))
            OpenFileDialog.SelectFile(NewImportData, title: "Select FC Tracker Data File", fileTypes: [("Json", ["json"])]);
        ImGui.SameLine();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, "Add new data file");

        ImGui.SetCursorPosX(14);

        if (FCTrackerWidgets.IconButton(FontAwesomeIcon.SyncAlt, FCTrackerTheme.AccentBlueDim, FCTrackerTheme.AccentBlue))
            Configuration.Instance.RefreshImportedData();
        ImGui.SameLine();
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, "Refresh Data");
    }

    private static void NewImportData(OpenFileName ofn)
    {
        try
        {

            Configuration.Instance.DataImportConfig.Add(new DataImportConfig
                                                        {
                                                            Path    = ofn.file,
                                                            Enabled = false
                                                        });
            Configuration.Instance.Save();
        }
        catch (Exception e)
        {
            Svc.Log.Error(e, $"Error occurred while loading data");
        }
    }
}
