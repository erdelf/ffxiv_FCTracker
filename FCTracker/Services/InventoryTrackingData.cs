using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;

namespace FCTracker.Services
{
    using ECommons.DalamudServices;
    using ECommons.IPC;
    using ECommons.Throttlers;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryTrackingData
    {
        [JsonProperty]
        public ulong CID;

        [JsonObject(MemberSerialization.OptOut)]
        private record InventoryItemData(uint Id, int Quantity);

        [JsonProperty]
        private Dictionary<InventoryType, HashSet<InventoryItemData>> inventoryContainers = [];

        private Dictionary<uint, int>? cachedItemCounts;

        private Dictionary<uint, int> CachedItemCounts
        {
            get
            {
                if (this.cachedItemCounts == null)
                {
                    this.cachedItemCounts = new Dictionary<uint, int>();
                    foreach (HashSet<InventoryItemData> container in this.inventoryContainers.Values)
                    {
                        foreach (InventoryItemData item in container)
                            if (this.cachedItemCounts.TryGetValue(item.Id, out int count))
                                this.cachedItemCounts[item.Id] = count + item.Quantity;
                            else
                                this.cachedItemCounts[item.Id] = item.Quantity;
                    }
                }
                return this.cachedItemCounts;
            }
        }

        public int Gil => this.GetItemCount(1);

        public int GetItemCount(uint id)
        {
            if (ECommonsIPC.AllaganTools.Available && this.CID != 0 && EzThrottler.Throttle($"ItemCheck_{id}_{this.CID}", 300_0000))
            {
                this.cachedItemCounts     ??= [];
                this.cachedItemCounts[id] =   (int) ECommonsIPC.AllaganTools.ItemCount(id, this.CID, -1);
            }

            return this.CachedItemCounts.GetValueOrDefault(id, 0);
        }

        public void WipeInventory()
        {
            this.inventoryContainers.Clear();
            Configuration.Instance.Save();
        }

        public unsafe void RefreshInventoryData()
        {
            if (!Configuration.Instance.GlobalData.GatherDataSelf || ECommonsIPC.AllaganTools.Available)
                return;

            InventoryManager* inventory = InventoryManager.Instance();

            foreach (InventoryType type in Enum.GetValues<InventoryType>())
            {
                InventoryContainer* container = inventory->GetInventoryContainer(type);

                if (container != null && container->IsLoaded)
                {
                    this.inventoryContainers[type] = [];
                    for (int i = 0; i < container->Size; i++)
                    {
                        InventoryItem inventoryItem = container->Items[i];
                        if(inventoryItem.ItemId != 0)
                            this.inventoryContainers[type].Add(new InventoryItemData(inventoryItem.ItemId, inventoryItem.Quantity));
                    }
                }
            }

            this.cachedItemCounts = null;

            Configuration.Instance.Save();
        }
    }
}