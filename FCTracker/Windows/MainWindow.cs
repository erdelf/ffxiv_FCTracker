namespace FCTracker.Windows;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using Henchman.Windows.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ECommons.GameHelpers;
using NightmareUI.Censoring;

public class MainWindow : Window, IDisposable
{
    public MainWindow() : base("FC Tracker##FCTrackerMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
                               {
                                   MinimumSize = new Vector2(375,            330),
                                   MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
                               };

        this.fcTable = new Table<FCData>("##FCList",
                                         new List<TableColumn<FCData>>
                                         {
                                             new("Name", x => Censor.Character(x.FCName), FilterType: FilterType.String, Alignment: ColumnAlignment.Center),
                                             new("World", x => Censor.World(x.World?.Name.ToString() ?? "??"), FilterType: FilterType.MultiSelect, Alignment: ColumnAlignment.Center),
                                             new("Rank", x => x.Rank.ToString(), FilterType : FilterType.MultiSelect, Alignment: ColumnAlignment.Center),
                                             new("Master", x => Censor.Character(x.MasterString), Alignment: ColumnAlignment.Center),
                                             new("Members", x => x.TotalMembers.ToString(), Alignment: ColumnAlignment.Center),
                                             new("Founding Date", x => DateTime.Now.Subtract(x.FoundingDate).Days.ToString(), Alignment: ColumnAlignment.Center)
                                         },
                                         () => Configuration.Instance.AllFCData,
                                         highlightPredicate: x => x.Id == Configuration.Instance.GetFCIdForCID(Player.CID),
                                         size: new Vector2(0, 0)
                                        );
    }

    public void Dispose() => 
        GC.SuppressFinalize(this);

    private readonly Table<FCData> fcTable;

    public override void Draw()
    {
        using (ImRaii.Disabled(!PlayerHelper.IsReady))
        {
            if (ImGui.Button("Refresh Current Char"))
                FCTracker.Plugin.GetFCInfo();
        }
        ImGui.SameLine();
        ImGui.Checkbox("Scramble Names", ref Censor.Config.Enabled);

        ImGui.Spacing();

        using (ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            this.fcTable.Draw();
        }
    }
}
