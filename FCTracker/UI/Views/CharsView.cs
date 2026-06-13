namespace FCTracker.UI.Views;

using Dalamud.Bindings.ImGui;
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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;

public class CharsView : IFCView
{
    public string Id => "chars";

    public (string Title, string Subtitle) GetHeaderInfo(FCViewContext ctx) =>
        ("Characters", $"{GetCharacters(ctx.Data).Count} Chars pending");

    public void Draw(FCViewContext ctx)
    {
        using ImRaii.ChildDisposable scrollArea = ImRaii.Child("##UpcomingScroll", Vector2.Zero, false);
        if (!scrollArea.Success) 
            return;

        ImGui.SetCursorPos(new Vector2(14, 12));

        DrawReadyBanner(ctx);
        DrawTimeline(ctx);
    }

    public static IReadOnlyList<CharData> GetCharacters(IFCDataProvider dataProvider)
    {
        ICharDataProvider charData = dataProvider.CharData();
        IReadOnlyList<CharData> chars = Configuration.Instance.CharViewData.CharsWithFC ?
                                            Configuration.Instance.CharViewData.CharsWithFCWithHouse ?
                                                charData.GetAllChars() :
                                                charData.GetAllCharsWithFCHouse() :
                                            charData.GetAllCharsWithoutFC();
        return chars;
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
            {
                CharViewData charViewData = Configuration.Instance.CharViewData;

                bool save = false;

                if (FCTrackerWidgets.Checkbox($"Show Chars with FCs", ref charViewData.CharsWithFC))
                    save = true;

                if(charViewData.CharsWithFC)
                {
                    ImGui.SameLine();
                    if(FCTrackerWidgets.Checkbox($"Show Chars with FCs with Houses", ref charViewData.CharsWithFCWithHouse))
                        save = true;
                }

                if (save)
                {
                    Configuration.Instance.CharViewData = charViewData;
                    Configuration.Instance.Save();
                }
            }

            ImGuiComponents.HelpMarker("This is not what this is designed for, but we carry enough data for it to be valuable regardless. maybe");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 12);
    }

    private static void DrawTimeline(FCViewContext ctx)
    {
        IReadOnlyList<CharData> chars = GetCharacters(ctx.Data);
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


        using var table = ImRaii.Table("##CharTable", 6, flags);
        if (!table.Success) 
            return;

        IEnumerable<IGrouping<World, CharData>> charWorlds = chars.GroupBy(ch => ch.World!.Value).OrderBy(g => g.Key.DataCenter.RowId);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Char",          ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Combat Lvl",    ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Gathering Lvl", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Gil",           ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Leves",         ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("##Spacer",      ImGuiTableColumnFlags.WidthStretch);

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
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.TextMuted, $"{worldCount}");
    }

    private static void DrawSection(World world, List<CharData> chars)
    {
        DrawWorldGroupHeader(world, chars);
        ImGui.TableNextRow();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(FCTrackerTheme.BackgroundSidebar));

        foreach (CharData ch in chars)
            DrawItem(ch);
    }

    private static void DrawItem(CharData ch)
    {
        ImGui.TableNextRow();

        if (ch.CID == FCTrackerPlugin.LoggedInCID)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, FCTrackerTheme.RowHighlightColor);

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

        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerCombatLevelColor(ch.HighestLevelCombat), ch.HighestLevelCombat.ToString());
        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerGatheringLevelColor(ch.HighestLevelGathering), ch.HighestLevelGathering.ToString());
        ImGui.TableNextColumn();

        if (ECommonsIPC.AllaganTools.Available)
        {
            uint gilCount = ECommonsIPC.AllaganTools.ItemCount(1u, ch.CID, -1);
            FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerGilColor(gilCount), gilCount.ToString("##,#"));
            if (ThreadLoadImageHandler.TryGetIconTextureWrap(ExcelItemHelper.Get(1)!.Value.Icon, false, out IDalamudTextureWrap? tex))
            {
                ImGui.SameLine(0, 0);
                ImGui.Image(tex.Handle, new Vector2(ImGuiHelpers.GetButtonSize("X").Y));
            }
        }

        ImGui.TableNextColumn();

        FCTrackerWidgets.ColoredText(FCTrackerTheme.GetPlayerLeveColor(ch.LeveAllowancesNow), ch.LeveAllowancesNow.ToString());

        ImGui.TableNextColumn();

    }
}
