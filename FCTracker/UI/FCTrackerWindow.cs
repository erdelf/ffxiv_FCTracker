namespace FCTracker.UI;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.GameHelpers;
using FCTracker.Services;
using FCTracker.UI.Views;
using NightmareUI.Censoring;
using System;
using System.Collections.Generic;
using System.Numerics;

public class FCTrackerWindow : Window, IDisposable
{
    private readonly IFCDataProvider dataProvider;
    private readonly FCTrackerLayout layout;
    private readonly Dictionary<string, IFCView> views = new();

    private string searchText = string.Empty;

    public FCTrackerWindow(IFCDataProvider dataProvider)
        : base("FC Tracker###FCTrackerMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.dataProvider = dataProvider;
        this.layout = new FCTrackerLayout(dataProvider);

        this.RegisterView(new AllFCsView());
        this.RegisterView(new UpcomingView());
        this.RegisterView(new ReadyNowView());
        this.RegisterView(new CharsView());

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(850, 550),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Size = new Vector2(1000, 700);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 1),
            Click = _ => FCTrackerPlugin.Plugin.ToggleConfigUi(),
            ShowTooltip = () => ImGui.SetTooltip("Open settings"),
        });
    }

    private void RegisterView(IFCView view) => this.views[view.Id] = view;

    public void Dispose() => GC.SuppressFinalize(this);

    public override void Draw()
    {
        using (FCTrackerTheme.Push())
        {
            string viewId = this.layout.Sidebar.ActiveViewId;
            if (!this.views.TryGetValue(viewId, out IFCView? view))
            {
                view = this.views["all"];
            }

            FCViewContext ctx = new()
            {
                Data = this.dataProvider,
                Sidebar = this.layout.Sidebar,
                SearchText = this.searchText,
            };

            (string title, string subtitle) = view.GetHeaderInfo(ctx);
            this.layout.DrawWithHeader(title, subtitle, () => view.Draw(ctx), this.DrawHeaderActions);
        }
    }

    private void DrawHeaderActions()
    {
        FCTrackerWidgets.Checkbox("Scramble Names", ref Censor.Config.Enabled);
        ImGui.SameLine();

        ImGui.SetNextItemWidth(150);
        using (ImRaii.PushColor(ImGuiCol.FrameBg, FCTrackerTheme.BackgroundCard))
            ImGui.InputTextWithHint("##Search", "Search...", ref this.searchText, 256);

        ImGui.SameLine();

        using (ImRaii.Disabled(!PlayerHelper.IsReady))
        {
            using (ImRaii.PushColor(ImGuiCol.Button, FCTrackerTheme.AccentBlueDim))
            using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.AccentBlue))
            using (ImRaii.PushFont(UiBuilder.IconFont))
                if (ImGui.Button(FontAwesomeIcon.SyncAlt.ToIconString(), new Vector2(28, 0)))
                    FCTrackerPlugin.Plugin.GetFCInfo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Refresh current character");

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.Button, FCTrackerTheme.AccentRed with { W = 0.15f }))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.AccentRed))
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString(), new Vector2(28, 0)))
            {
                if(ImGui.GetIO().KeyCtrl)
                    Configuration.Instance.ClearData();
                else
                    Configuration.Instance.RemoveCurrentFCData();
                Configuration.Instance.Save();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear current character's data\nHold Ctrl to clear all data");
    }
}
