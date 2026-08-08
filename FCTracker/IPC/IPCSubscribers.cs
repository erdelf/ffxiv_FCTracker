namespace FCTracker.IPC
{
    using ECommons.DalamudServices;
    using ECommons.EzIpcManager;
    using ECommons.IPC.Subscribers;
    using ECommons.Throttlers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HouseHunterIPC : IPCBase
    {
        public static HouseHunterIPC Instance { get; private set; } = new();

        public HouseHunterIPC()
        {
        }

        public HouseHunterIPC(SafeWrapper wrapper) : base(wrapper)
        {
        }

        public override string InternalName { get; } = "HouseHunter";

        /// <summary>
        /// Retrieves list of characters which HouseHunter knows about. 
        /// </summary>
        [EzIPC("GetRegisteredCharacters")] public Func<Dictionary<ulong, string>> GetRegisteredCharacters;
        /// <summary>
        /// Retrieves saved lottery data for specified CID, or null if character is not found.
        /// </summary>
        [EzIPC("GetLotteryData")] public Func<ulong, LotterySaveData?> GetLotteryData;

        private Dictionary<ulong, LotterySaveData?> cachedLotteryData = [];

        public LotterySaveData? GetLotteryDataForCID(ulong cid)
        {
            this.cachedLotteryData ??= [];
            if (this.Available && cid != 0 && (!this.cachedLotteryData.ContainsKey(cid) || EzThrottler.Throttle($"LotteryCheck_{cid}", 300_000)))
            {
                LotterySaveData? lotteryData = this.GetLotteryData?.Invoke(cid);

                Svc.Log.Debug($"Lottery data polled from IPC for CID {cid}: {lotteryData?.IsParticipating}");

                this.cachedLotteryData[cid] = lotteryData;
            }
            return this.cachedLotteryData.GetValueOrDefault(cid, null);
        }

        public void ClearCacheFor(ulong CID) =>
            this.cachedLotteryData.Remove(CID);

        public IEnumerable<LotterySaveData?> GetLotteryDataForFC(FCData fc)
        {
            foreach (ulong cid in fc.MemberCIDs)
            {
                LotterySaveData? lotteryDataForCID = this.GetLotteryDataForCID(cid);
                if(lotteryDataForCID is { IsParticipating: true, BuyerType: BuyerType.FreeCompany })
                    yield return lotteryDataForCID;
            }
        }

        public string? GetLotteryTextForFC(FCData fc)
        {
            if (!this.Available)
                return null;

            IEnumerable<LotterySaveData?> data = this.GetLotteryDataForFC(fc).ToList();
            return data.Any() ?
                       "Bidding on " + string.Join(", ", data.Select(d => $"{FCData.HouseInfo.GetResidentialAetheryteByTerritoryType(d!.Territory)} Ward {d!.Ward + 1} - Plot {d!.Plot + 1}")) : 
                       null;
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class LotterySaveData
        {
            public bool          IsParticipating = false;
            public uint          Ward;
            public uint          Plot;
            public uint          Territory;
            public uint          CharaGil;
            public BuyerType     BuyerType;
            public LotteryStatus LotteryStatus;
            public long          LastUpdate;
            public string        PlayerName;
            public uint          Number;
        }

        public enum BuyerType : byte
        {
            FreeCompany = 1,
            Private     = 2,
            Unknown     = 0xFF,
        }

        public enum LotteryStatus : byte
        {
            EntryPeriod   = 1,
            ResultsPeriod = 2,
            Unknown       = 0xFF,
        }
    }

}
