namespace FCTracker;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using NightmareUI.Censoring;
using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;

[JsonObject(MemberSerialization.OptIn)]
public class Configuration
{
    public static Configuration Instance { get; set; } = null!;

    [JsonProperty]
    public int ConfigVersion { get; set; } = 0;

    [JsonProperty]
    public Dictionary<ulong, CharData> charByCID = [];


    [JsonProperty]
    private Dictionary<ulong, FCData> FCData { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<FCData> AllFCData => this.FCData.Values;
    public ulong GetFCIdForCID(ulong cid) => 
        this.charByCID.TryGetValue(cid, out CharData charData) ? charData.FC : 0;
    
    public void ClearData() => 
        this.FCData.Clear();

    public void RemoveCurrentFCData()
    {
        if(Player.Available)
            if(this.charByCID.TryGetValue(Player.CID, out CharData charData))
                this.FCData.Remove(charData.FC);
    }

    public unsafe void UpdateCurrentFCData()
    {
        if (!Player.Available)
            return;

        InfoProxyFreeCompany* fcProxy = InfoProxyFreeCompany.Instance();

        if (fcProxy->Id == 0)
            return;

        this.charByCID[Player.CID] = new CharData
                              {
                                  CID   = Player.CID,
                                  Name  = Player.Name,
                                  World = Player.CurrentWorldName,
                                  FC = fcProxy->Id
                              };

        if (!this.FCData.TryGetValue(fcProxy->Id, out FCData? fcData))
        {
            fcData = new FCData
                     {
                         HomeWorldId  = fcProxy->HomeWorldId,
                         Id           = fcProxy->Id,
                         GrandCompany = fcProxy->GrandCompany,
                     };
        }

        fcData.MemberCIDs.Add(Player.CID);



        StringArrayData* arrayData = RaptureAtkModule.Instance()->GetStringArrayData(49);
        if (arrayData->Size > 4)
        {
            CStringPointer x        = arrayData->StringArray[6];
            SeString       seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(x));
            string         text     = seString.GetText();

            if (DateTime.TryParse(text, out DateTime dt))
                fcData.FoundingDate = dt;
        }

        arrayData = RaptureAtkModule.Instance()->GetStringArrayData(48);
        if (arrayData->Size > 1)
        {
            CStringPointer x        = arrayData->StringArray[2];
            SeString       seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(x));

            fcData.Tag = seString.GetText();
        }

        HouseId houseId = HousingManager.GetOwnedHouseId(EstateType.FreeCompanyEstate);
        if (houseId.Unit.Value < 255)
        {
            FCData.HouseInfo houseInfo = default;
            houseInfo.Ward = houseId.WardIndex;
            houseInfo.Plot = houseId.Unit.PlotIndex;
            FCData.HouseInfo.ResidentialAetheryteKind? residentialAetheryteKind = FCTracker.FCData.HouseInfo.GetResidentialAetheryteByTerritoryType(houseId.TerritoryTypeId);
            if (residentialAetheryteKind != null)
                houseInfo.City = residentialAetheryteKind.Value;
            fcData.House = houseInfo;
        }

        fcData.FCName       = fcProxy->NameString;
        fcData.TotalMembers = fcProxy->TotalMembers;
        fcData.MasterString = fcProxy->MasterString;
        fcData.Rank         = fcProxy->Rank;
        

        this.FCData[fcProxy->Id] = fcData;

        this.Save();
    }

    public void Save()
    {
        EzConfig.Save();
    }
}

public class FCTrackerSerializationFactory : DefaultSerializationFactory, ISerializationFactory
{
    public override string DefaultConfigFileName { get; } = "FCTrackerConfig.json";

    public new string Serialize(object config) =>
        base.Serialize(config);

    public override byte[] SerializeAsBin(object config) =>
        Encoding.UTF8.GetBytes(this.Serialize(config));
}

[JsonObject(MemberSerialization.OptOut)]
public struct CharData
{
    public required ulong  CID;
    public          string Name;
    public          string World;
    public          ulong  FC;

    public readonly string GetName() =>
        this.Name.Length != 0 ? Censor.Character(this.Name, this.World) : this.CID.ToString();

    public readonly override int GetHashCode() =>
        this.CID.GetHashCode();
}


public enum HousingStatusCategory
{
    Ready,   // 30+ days, rank ≥6, no house
    Soon,    // <=7 days remaining
    Waiting, // >7 days remaining
    Owned    // HouseTemp populated
}

[JsonObject(MemberSerialization.OptOut)]
public class FCData
{
    [JsonIgnore]
    public World? World => field ??= ExcelWorldHelper.Get(this.HomeWorldId);
    public ulong   Id           { get; set; }
    public string FCName       { get; set; } = string.Empty;
    public string Tag          { get; set; } = string.Empty;
    public uint    TotalMembers { get; set; }
    public string       MasterString { get; set; } = string.Empty;
    public uint         HomeWorldId  { get; set; }
    public GrandCompany GrandCompany { get; set; }
    public uint         Rank         { get; set; } // 6 needed for Housing
    public DateTime     FoundingDate { get; set; } // 30 days needed for Housing
    public HashSet<ulong> MemberCIDs { get; set; } = [];

    [JsonObject(MemberSerialization.OptOut)]
    public struct HouseInfo
    {
        // From lifestream
        public enum ResidentialAetheryteKind
        {
            Goblet       = 9,
            LavenderBeds = 2,
            Limsa        = 8,
            Foundation   = 70,
            Shirogane    = 111,
        }

        public static ResidentialAetheryteKind? GetResidentialAetheryteByTerritoryType(uint territoryType)
        {
            var t = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryType);
            if (t                             == null) return null;
            if (t.Value.PlaceNameRegion.RowId == 2402) return ResidentialAetheryteKind.Shirogane;
            if (t.Value.PlaceNameRegion.RowId == 25) return ResidentialAetheryteKind.Foundation;
            if (t.Value.PlaceNameRegion.RowId == 23) return ResidentialAetheryteKind.LavenderBeds;
            if (t.Value.PlaceNameRegion.RowId == 24) return ResidentialAetheryteKind.Goblet;
            if (t.Value.PlaceNameRegion.RowId == 22) return ResidentialAetheryteKind.Limsa;
            return null;
        }

        public ResidentialAetheryteKind City { get; set; }
        public byte Ward { get; set; }
        public byte Plot { get; set; }
    }

    public HouseInfo? House { get; set; }

    [JsonIgnore]
    public string WorldName => this.World?.Name.ToString() ?? "??";

    [JsonIgnore]
    public string Datacenter => this.World?.DataCenter.ValueNullable?.Name.ToString() ?? string.Empty;

    [JsonIgnore]
    public string Region
    {
        get
        {
            uint? regionId = this.World?.DataCenter.ValueNullable?.Region.RowId;
            return regionId switch
            {
                1 => "JP",
                2 => "NA",
                3 => "EU",
                4 => "OCE",
                _ => "??"
            };
        }
    }

    [JsonIgnore]
    public bool HasHouse => 
        this.House != null;

    [JsonIgnore]
    public int DaysSinceFounded => 
        this.FoundingDate == default ? 
            0 : 
            (int)(DateTime.Now - this.FoundingDate).TotalDays;

    [JsonIgnore]
    public int DaysUntilEligible
    {
        get
        {
            int remaining = 30 - this.DaysSinceFounded;
            return remaining > 0 ? remaining : 0;
        }
    }

    [JsonIgnore]
    public bool IsEligible => 
        this.DaysSinceFounded >= 30 && this.Rank >= 6 && !this.HasHouse;

    [JsonIgnore]
    public DateTime EligibilityDate => 
        this.FoundingDate == default ? DateTime.Now : this.FoundingDate.AddDays(30);

    [JsonIgnore]
    private bool? masterAvailable;
    [JsonIgnore]
    public bool MasterAvailable =>
        this.masterAvailable ??= Configuration.Instance.charByCID.Any(ch => this.MemberCIDs.Contains(ch.Value.CID) && ch.Value.Name == this.MasterString);

    public void AddMember(ulong cid)
    {
        this.MemberCIDs.Add(cid);
        this.masterAvailable = null;
    }

    public HousingStatusCategory GetStatusCategory() => 
        this.HasHouse ? 
            HousingStatusCategory.Owned : 
            this.IsEligible ? 
                HousingStatusCategory.Ready : 
                this.DaysUntilEligible <= 7 ? 
                    HousingStatusCategory.Soon : 
                    HousingStatusCategory.Waiting;

    public string GetHousingStatusText() => 
        this.HasHouse ? 
            $"{this.House?.City} - Ward {this.House?.Ward + 1} - Plot {this.House?.Plot + 1}" : 
            this.IsEligible ?
                "Eligible" : 
                $"{this.DaysUntilEligible}d left";
}