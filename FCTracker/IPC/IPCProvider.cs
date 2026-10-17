using ECommons.EzIpcManager;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCTracker.IPC;

using System.Diagnostics.CodeAnalysis;
using System.Linq;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class IPCProvider
{
    internal IPCProvider() =>
        EzIPC.Init(this);

    [EzIPC] public int GetIPCVersion() => 1;

    [EzIPC] public FCData.HouseInfo? GetHousingStatus(ulong CID) =>
        Configuration.Instance.AllFCData.TryGetValue(CID, out FCData? fcData) ? fcData.House : null;

    [EzIPC] public FCData.HouseInfo? GetHousingStatusByChar(ulong CID)
    {
        if(Configuration.Instance.AllCharData.TryGetValue(CID, out CharData charData))
            if (charData.FC.HasValue)
                if (Configuration.Instance.AllFCData.TryGetValue(charData.FC.Value, out FCData? fcData))
                    return fcData.House;

        return null;
    }

    [EzIPC] public Dictionary<ulong, FCData> GetAllFCData(bool onlyCurrentAccount) =>
        onlyCurrentAccount ? Configuration.Instance.GatheredData.FCData : Configuration.Instance.AllFCData;

    [EzIPC] public Dictionary<ulong, CharData> GetAllCharData(bool onlyCurrentAccount) =>
        onlyCurrentAccount ? Configuration.Instance.GatheredData.CharByCID : Configuration.Instance.AllCharData;

    [EzIPC] public FCData? GetFCData(ulong CID) =>
        Configuration.Instance.AllFCData.GetValueOrDefault(CID);

    [EzIPC] public CharData? GetCharData(ulong CID) =>
        Configuration.Instance.AllCharData.GetValueOrDefault(CID);

    [EzIPC] public IEnumerable<ulong> RegisteredFCs(bool onlyCurrentAccount) =>
        onlyCurrentAccount ? Configuration.Instance.GatheredData.FCData.Keys : Configuration.Instance.AllFCData.Keys;

    [EzIPC] public IEnumerable<ulong> RegisteredChars(bool onlyCurrentAccount) =>
        onlyCurrentAccount ? Configuration.Instance.GatheredData.CharByCID.Keys : Configuration.Instance.AllCharData.Keys;

    [EzIPC] public DateTime GetEligibilityDate(ulong FCCID) =>
        Configuration.Instance.AllFCData.TryGetValue(FCCID, out FCData? fcData) ? fcData.EligibilityDate : DateTime.MinValue;

    #region Inventory
    [EzIPC] public Dictionary<uint, int> GetCharacterItems(ulong CID) => 
        Configuration.Instance.AllCharData.TryGetValue(CID, out CharData charData) ? charData.InventoryData.CachedItemCounts : [];

    [EzIPC] public int GetCharacterItemCount(ulong CID, uint itemId) =>
        Configuration.Instance.AllCharData.TryGetValue(CID, out CharData charData) ? charData.InventoryData.GetItemCount(itemId) : 0;

    [EzIPC] public Dictionary<uint, int> GetFCItems(ulong CID) =>
        Configuration.Instance.AllFCData.TryGetValue(CID, out FCData? fcData) ? 
            fcData.MemberCIDsResolved?.Select(c => c.InventoryData.CachedItemCounts).Aggregate(new Dictionary<uint, int>(), (dict, next) =>
                                                                                                                            {
                                                                                                                                foreach (KeyValuePair<uint, int> kvp in next) 
                                                                                                                                    dict[kvp.Key] = dict.GetValueOrDefault(kvp.Key) + kvp.Value;
                                                                                                                                return dict;
                                                                                                                            }) ?? [] : 
            [];

    [EzIPC] public int GetFCItemCount(ulong CID, uint itemId) =>
        Configuration.Instance.AllFCData.TryGetValue(CID, out FCData? fcData) ? fcData.MemberCIDsResolved?.Sum(c => c.InventoryData.GetItemCount(itemId)) ?? 0 : 0;

    [EzIPC] public int GetItemCount(uint itemID, bool onlyCurrentAccount) =>
        (onlyCurrentAccount ? Configuration.Instance.GatheredData.CharByCID : Configuration.Instance.AllCharData).Sum(charData => charData.Value.InventoryData.GetItemCount(itemID));
#endregion

    [EzIPC] public ARData? GetAutoRetainerDataFC(ulong CID) =>
        Configuration.Instance.AllFCData.TryGetValue(CID, out FCData? fcData) ? fcData.AutoRetainerData : null;

    [EzIPC] public ARData? GetAutoRetainerData(bool onlyCurrentAccount) =>
        (onlyCurrentAccount ? Configuration.Instance.GatheredData.FCData.Values : Configuration.Instance.AllFCData.Values).Aggregate(new ARData(), (acc, next) => acc.Merge(next.AutoRetainerData));
}