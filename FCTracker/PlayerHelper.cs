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
        public static unsafe bool IsValid =>
            Control.GetLocalPlayer() != null
         && ThreadSafety.IsMainThread
         && Svc.Condition.Any()
         && !Svc.Condition[ConditionFlag.BetweenAreas]
         && !Svc.Condition[ConditionFlag.BetweenAreas51]
         && Player.Available
         && Player.Interactable;

        public static bool IsJumping => Svc.Condition.Any()
                                     && (Svc.Condition[ConditionFlag.Jumping]
                                      || Svc.Condition[ConditionFlag.Jumping61]);

        public static unsafe bool IsAnimationLocked => ActionManager.Instance()->AnimationLock > 0;

        public static bool IsReady => IsValid && !IsOccupied;

        public static bool IsOccupied => GenericHelpers.IsOccupied() || Svc.Condition[ConditionFlag.Jumping61];

        public static bool IsReadyFull => IsValid && !IsOccupiedFull;

        public static bool IsOccupiedFull => IsOccupied || IsAnimationLocked;
    }
}
