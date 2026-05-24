namespace FCTracker.UI.Views;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.IPC;
using Lumina.Excel.Sheets;
using NightmareUI.Censoring;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;

public class Chars_NoFC : IFCView
{
    public string Id => "chars-no-fc";

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx) =>
        ("No FC Chars", $"{ctx.Data.CharData().GetAllCharsWithoutFC().Count} Chars pending");

    private static bool allChars = false;

    public void Draw(FCViewContext ctx)
    {
        using ImRaii.ChildDisposable scrollArea = ImRaii.Child("##UpcomingScroll", Vector2.Zero, false);
        if (!scrollArea.Success) 
            return;

        ImGui.SetCursorPos(new Vector2(14, 12));

        DrawReadyBanner(ctx);
        DrawTimeline(ctx);
    }

    private static void DrawReadyBanner(FCViewContext ctx)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, FCTrackerTheme.AccentGreenDim))
        using (ImRaii.PushColor(ImGuiCol.Border, FCTrackerTheme.AccentGreen with { W = 0.3f }))
        {
            using ImRaii.ChildDisposable banner = ImRaii.Child("##ConfigBanner", new Vector2(ImGui.GetContentRegionAvail().X - 28, 36), true);
            if (!banner.Success) 
                return;

            ImGui.SetCursorPos(new Vector2(12, 8));

            using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.AccentGreen))
                FCTrackerWidgets.Checkbox($"Show Chars with FCs", ref allChars);
            ImGuiComponents.HelpMarker("This is not what this is designed for, but we carry enough data for it to be valuable regardless. maybe");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12);
    }

    private static void DrawTimeline(FCViewContext ctx)
    {
        ICharDataProvider       charData = ctx.Data.CharData();
        IReadOnlyList<CharData> chars    = allChars ? charData.GetAllChars() : charData.GetAllCharsWithoutFC();
        if (chars.Count == 0)
        {
            ImGui.SetCursorPos(new Vector2(14, ImGui.GetCursorPosY() + 20));
            FCTrackerWidgets.IconLabel(FCTrackerTheme.TextSecondary, FontAwesomeIcon.CalendarAlt,
                "No Chars without FC.");
            return;
        }

        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY        |
                                      ImGuiTableFlags.PadOuterX      |
                                      ImGuiTableFlags.SizingFixedFit |
                                      ImGuiTableFlags.Resizable;


        using var table = ImRaii.Table("##UpcomingTable", 4, flags);
        if (!table.Success) 
            return;

        IEnumerable<IGrouping<World, CharData>> charWorlds = chars.GroupBy(ch => ch.World!.Value).OrderBy(g => g.Key.DataCenter.RowId);

        //ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 65);
        //ImGui.TableSetupColumn("Days", ImGuiTableColumnFlags.WidthFixed, 45);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Char",     ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Lvl",      ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Gil",      ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Leves",    ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("##Spacer", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushColor(ImGuiCol.TableHeaderBg, FCTrackerTheme.BackgroundHeader))
        using (ImRaii.PushColor(ImGuiCol.Text, FCTrackerTheme.TextSecondary))
            ImGui.TableHeadersRow();

        foreach (IGrouping<World, CharData> charGroup in charWorlds)
            DrawSection(charGroup.Key, charGroup.ToList());
    }

    private static void DrawWorldGroupHeader(World world, List<CharData> chars)
    {
        int worldCount = chars.Count;

        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.BackgroundSidebar));

        ImGui.TableNextColumn();
        FCTrackerWidgets.IconLabel(FCTrackerTheme.TextPrimary, FCTrackerTheme.GetRegionIcon(FCTrackerTheme.RegionString(world)), Censor.World(world.Name.ExtractText()));

        if (!string.IsNullOrEmpty(world.DataCenter.ValueNullable?.Name.ExtractText()))
        {
            ImGui.SameLine();
            FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"·  {world.DataCenter.ValueNullable?.Name.ExtractText()}");
        }

        ImGui.TableNextColumn();
        
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"{worldCount}");

        ImGui.TableNextColumn();
    }

    private static void DrawSection(World world, List<CharData> chars)
    {
        DrawWorldGroupHeader(world, chars);
        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.BackgroundSidebar));

        foreach (CharData ch in chars)
            DrawItem(ch);


        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Dummy(new Vector2(0, 4));
    }

    private static void DrawItem(CharData ch)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        bool selectable = ImGui.Selectable("##CharCell" + ch.CID);
        ImGui.SetItemAllowOverlap();
        ImGui.SameLine(0, 0);

        ImGuiHelpers.CompileSeStringWrapped($"<icon({(uint)PlayerHelper.GetGCFontIcon(ch.GrandCompany)})>");

        ImGui.SameLine(0, 4);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetRankColor(ch.GrandCompanyRank), $"{ch.GrandCompanyRank}");

        ImGui.SameLine(0, 8);
        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, Censor.Character(ch.Name));

        if (selectable)
            ECommonsIPC.Lifestream.ChangeCharacter(ch.Name, ch.WorldName);

        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerLevelColor(ch.HighestLevel), ch.HighestLevel.ToString());

        ImGui.TableNextColumn();

        if (ECommonsIPC.AllaganTools.Available)
        {
            uint gilCount = ECommonsIPC.AllaganTools.ItemCount(1u, ch.CID, -1);
            FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerGilColor(gilCount), gilCount.ToString("##,#"));
            if (ThreadLoadImageHandler.TryGetIconTextureWrap(ExcelItemHelper.Get(1).Value.Icon, false, out IDalamudTextureWrap? tex))
            {
                ImGui.SameLine(0, 0);
                ImGui.Image(tex.Handle, new Vector2(ImGuiHelpers.GetButtonSize("X").Y));
            }
        }

        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextPrimary, ch.LeveAllowancesNow.ToString());

        ImGui.TableNextColumn();

    }
}
