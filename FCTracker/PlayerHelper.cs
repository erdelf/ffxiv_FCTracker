namespace FCTracker
{
    using Dalamud.Game.ClientState.Conditions;
    using Dalamud.Utility;
    using ECommons;
    using ECommons.DalamudServices;
    using ECommons.GameHelpers;
    using FFXIVClientStructs.FFXIV.Client.Game;
    using FFXIVClientStructs.FFXIV.Client.Game.Control;

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
    }
}
