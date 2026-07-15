namespace FCTracker
{
    using Dalamud.Game.ClientState.Conditions;
    using Dalamud.Game.Text.SeStringHandling;
    using Dalamud.Utility;
    using ECommons;
    using ECommons.DalamudServices;
    using ECommons.ExcelServices;
    using ECommons.GameHelpers;
    using FFXIVClientStructs.FFXIV.Client.Game;
    using FFXIVClientStructs.FFXIV.Client.Game.Control;
    using FFXIVClientStructs.FFXIV.Client.Game.UI;
    using Lumina.Excel.Sheets;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;

    internal static class PlayerHelper
    {
        private static unsafe bool IsValid =>
            Control.GetLocalPlayer() != null
         && ThreadSafety.IsMainThread
         && Svc.Condition.Any()
         && !Svc.Condition[ConditionFlag.BetweenAreas]
         && !Svc.Condition[ConditionFlag.BetweenAreas51]
         && !Svc.Condition[ConditionFlag.WatchingCutscene]
         && !Svc.Condition[ConditionFlag.WatchingCutscene78]
         && Player.Available
         && Player.Interactable;

        public static bool IsJumping => Svc.Condition.Any()
                                     && (Svc.Condition[ConditionFlag.Jumping]
                                      || Svc.Condition[ConditionFlag.Jumping61]);

        private static unsafe bool IsAnimationLocked => ActionManager.Instance()->AnimationLock > 0;

        public static bool IsReady => IsValid && !IsOccupied;

        private static bool IsOccupied => GenericHelpers.IsOccupied() || Svc.Condition[ConditionFlag.Jumping61];

        public static bool IsReadyFull => IsValid && !IsOccupiedFull;

        private static bool IsOccupiedFull => IsOccupied || IsAnimationLocked;

        public static unsafe int LeveAllowances => QuestManager.Instance()->NumLeveAllowances;

        internal static unsafe GrandCompany GetGrandCompany() => (GrandCompany)PlayerState.Instance()->GrandCompany;

        internal static unsafe uint GetGrandCompanyRank() => PlayerState.Instance()->GetGrandCompanyRank();

        internal static unsafe List<(uint cj, short level)> GetHighestCombatLevelsFromSheet()
        {
            PlayerState* playerState = PlayerState.Instance();
            List<(ClassJob cj, short level)> valueTuples = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role > 0).
                                                                                Select(cj => (cj: cj, level: playerState->ClassJobLevels[cj.ExpArrayIndex])).
                                                                                Where(x => x.level > 0).Where(x => x.level > 0).ToList();

            IEnumerable<(ClassJob cj, short level)> enumerable = valueTuples.
                Where(x => x.cj.ClassJobParent.RowId != x.cj.RowId ||
                           valueTuples.TrueForAll(p => x.cj.RowId == p.cj.RowId || p.cj.ClassJobParent.RowId != x.cj.RowId));

            return enumerable.Select(x => (x.cj.RowId, x.level)).OrderByDescending(x => x.level).ToList();
        }

        internal static unsafe List<(uint cj, short level)> GetGatheringLevelsFromSheet()
        {
            PlayerState* playerState = PlayerState.Instance();

            return Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.ClassJobCategory.RowId == 32).
                       Select(cj => (cj: cj.RowId, level: playerState->ClassJobLevels[cj.ExpArrayIndex])).
                       Where(x => x.level > 0).Where(x => x.level > 0).OrderByDescending(x => x.level).ToList();
        }


        internal static unsafe short GetCurrentLevelFromSheet(Job job)
        {
            PlayerState* playerState = PlayerState.Instance();
            return playerState->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault((uint)job)?.ExpArrayIndex ?? 0];
        }

        internal static unsafe short GetHighestLevelFromSheet()
        {
            PlayerState* playerState = PlayerState.Instance();
            return playerState->ClassJobLevels.ToArray().MaxSafe();
        }

        internal static BitmapFontIcon GetGCFontIcon(GrandCompany gc) =>
            gc switch
            {
                GrandCompany.TwinAdder => BitmapFontIcon.BlackShroud,
                GrandCompany.ImmortalFlames => BitmapFontIcon.Thanalan,
                GrandCompany.Maelstrom => BitmapFontIcon.LaNoscea,
                _ => BitmapFontIcon.BlueStarProblem
            };
    }
}
