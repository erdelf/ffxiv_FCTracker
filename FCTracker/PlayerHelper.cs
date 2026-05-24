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
