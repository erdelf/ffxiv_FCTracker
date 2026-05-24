namespace FCTracker;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons;
using ECommons.Configuration;
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
using AutoRetainerAPI.Configuration;
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
    public Dictionary<ulong, FCData> FCData { get; set; } = [];

    public static  AutoRetainerAPI.AutoRetainerApi AR_API = new();


    private static ARData? arData;

    public static ARData ARData
    {
        get
        {
            if (arData.HasValue)
                return arData.Value;

            ARData data = new();

            foreach (FCData fcData in Instance.AllFCData)
            {
                ARData? fcARData = fcData.AutoRetainerData;
                data.RepairCount += fcARData?.RepairCount ?? 0;
                data.FuelCount += fcARData?.FuelCount ?? 0;
            }
            arData = data;

            return arData.Value;
        }
    }
    public static void ARDataBust() => arData = null;

    public IEnumerable<FCData> AllFCData => this.FCData.Values;
    public ulong? GetFCIdForCID(ulong cid) => 
        this.charByCID.TryGetValue(cid, out CharData charData) ? charData.FC : null;
    
    public void ClearData() => 
        this.FCData.Clear();

    public void RemoveCurrentFCData()
    {
        if(Player.Available)
            if(this.charByCID.TryGetValue(Player.CID, out CharData charData))
                if(charData.FC.HasValue)
                    this.FCData.Remove(charData.FC.Value);
    }

    public void UpdateCurrentCharData()
    {
        this.charByCID[Player.CID] = new CharData
                                     {
                                         CID              = Player.CID,
                                         Name             = Player.Name,
                                         WorldId          = Player.HomeWorld.RowId,
                                         GrandCompany     = (GrandCompany)Player.GrandCompany,
                                         GrandCompanyRank = PlayerHelper.GetGrandCompanyRank(),
                                         HighestLevel     = PlayerHelper.GetHighestLevelFromSheet(),
                                         LeveAllowances = Math.Min(100, PlayerHelper.LeveAllowances + 3),
                                         LeveAllowanceTime = QuestManager.GetNextLeveAllowancesDateTime()
                                     };

        this.Save();
    }

    public unsafe void UpdateCurrentFCData()
    {
        if (!Player.Available)
            return;

        InfoProxyFreeCompany* fcProxy = InfoProxyFreeCompany.Instance();

        if (fcProxy->Id == 0)
            return;

        this.UpdateCurrentCharData();

        this.charByCID[Player.CID] = this.charByCID[Player.CID] with {FC = fcProxy->Id};

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
            FCData.HouseInfo.ResidentialAetheryteKind? aetheryteKind = FCTracker.FCData.HouseInfo.GetResidentialAetheryteByTerritoryType(houseId.TerritoryTypeId);
            if (!fcData.HasHouse                       ||
                fcData.House!.Ward != houseId.WardIndex ||
                fcData.House.Plot != houseId.PlotIndex ||
                fcData.House.City != aetheryteKind)
            {
                FCData.HouseInfo houseInfo = new()
                                             {
                                                 Ward = houseId.WardIndex,
                                                 Plot = houseId.Unit.PlotIndex
                                             };
                if (aetheryteKind != null)
                    houseInfo.City = aetheryteKind.Value;
                fcData.House = houseInfo;
            }
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
    public required ulong        CID;
    public          string       Name;
    public          uint         WorldId;
    public          ulong?       FC;

    public GrandCompany GrandCompany;
    public uint         GrandCompanyRank;

    public short HighestLevel;

    public int      LeveAllowances;
    public DateTime LeveAllowanceTime;


    [JsonIgnore]
    public World? World => field ??= ExcelWorldHelper.Get(this.WorldId);
    [JsonIgnore]
    public string WorldName => this.World?.Name.ExtractText() ?? "???";

    public string GetName() =>
        this.Name.Length != 0 ? Censor.Character(this.Name, this.WorldName) : this.CID.ToString();

    public int LeveAllowancesNow
    {
        get
        {
            if (this.LeveAllowances is <= 0 or >= 100)
                return this.LeveAllowances;

            TimeSpan timePassed = DateTime.UtcNow - this.LeveAllowanceTime;
            if (timePassed.Ticks <= 0)
                return this.LeveAllowances-3;
            return this.LeveAllowances + (int)(timePassed.TotalHours / 12);
        }
    }

    public readonly override int GetHashCode() =>
        this.CID.GetHashCode();
}

[JsonObject(MemberSerialization.OptOut)]
public struct ARData
{
    public int RepairCount { get; set; }
    public int FuelCount   { get; set; }
}

public enum HousingStatusCategory
{
    Ready,   // 30+ days, rank ≥6, no house
    Soon,    // <=7 days remaining
    Waiting, // >7 days remaining
    NeverVisited,
    VisitedIn7Days,
    VisitedIn30Days,
    VisitedIn40Days,
    DemolitionImminent,
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


    [JsonIgnore]
    private ARData? autoRetainerData;

    [JsonIgnore]
    public ARData? AutoRetainerData
    {
        get
        {
            if(this.autoRetainerData.HasValue)
                return this.autoRetainerData.Value;

            if (Configuration.AR_API.Ready && this.MemberCIDs.Count != 0)
            {
                ARData arData = new();

                foreach (ulong memberCID in this.MemberCIDs)
                {
                    OfflineCharacterData data = Configuration.AR_API.GetOfflineCharacterData(memberCID);
                    arData.RepairCount += data.RepairKits;
                    arData.FuelCount   += data.Ceruleum;
                }

                this.autoRetainerData = arData;
            }
            return this.autoRetainerData;
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class HouseInfo
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
            TerritoryType? t = ExcelTerritoryHelper.Get(territoryType);
            if (t == null) 
                return null;
            return t.Value.PlaceNameRegion.RowId switch
            {
                2402 => ResidentialAetheryteKind.Shirogane,
                25 => ResidentialAetheryteKind.Foundation,
                23 => ResidentialAetheryteKind.LavenderBeds,
                24 => ResidentialAetheryteKind.Goblet,
                22 => ResidentialAetheryteKind.Limsa,
                _ => null
            };
        }

        public ResidentialAetheryteKind City { get; set; }
        public byte Ward { get; set; }
        public byte Plot { get; set; }
        public DateTime? LastVisited { get; set; }

        public void Visited() => 
            this.LastVisited = DateTime.UtcNow;

        [JsonIgnore]
        public int DaysSinceLastVisit =>
            this.LastVisited.HasValue ? (int)(DateTime.UtcNow - this.LastVisited.Value).TotalDays : int.MaxValue;

        public HousingStatusCategory GetVisitationStatus()
        {
            if (this.LastVisited == null)
                return HousingStatusCategory.NeverVisited;

            TimeSpan timeSinceVisit = DateTime.UtcNow - this.LastVisited.Value;
            return timeSinceVisit.Days switch
            {
                < 7 => HousingStatusCategory.VisitedIn7Days,
                < 30 => HousingStatusCategory.VisitedIn30Days,
                < 40 => HousingStatusCategory.VisitedIn40Days,
                _ => HousingStatusCategory.DemolitionImminent,
            };
        }
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
        this.FoundingDate == default ? DateTime.MaxValue : this.FoundingDate.AddDays(30);

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

    public void RecacheARData()
    {
        this.autoRetainerData = null;
        Configuration.ARDataBust();
    }

    public HousingStatusCategory GetStatusCategory() => 
        this.HasHouse ?
            this.House!.GetVisitationStatus() : 
            this.IsEligible ? 
                HousingStatusCategory.Ready : 
                this.DaysUntilEligible <= 7 ? 
                    HousingStatusCategory.Soon : 
                    HousingStatusCategory.Waiting;

    public string GetHousingStatusText() =>
        this.HasHouse ?
            $"{this.House!.City} - Ward {this.House.Ward + 1} - Plot {this.House.Plot + 1}" :
            this.IsEligible ?
                "Eligible" :
                this.FoundingDate == default ?
                    "Not yet founded" :
                    $"{this.DaysUntilEligible}d left";

    public string GetHousingDemolitionText() =>
        this.HasHouse ?
            $"{(this.House!.LastVisited != null ? $"Last Visited: {(this.House.DaysSinceLastVisit > 0 ? $"{this.House.DaysSinceLastVisit}d ago" : "Today")}" : "Never visited")}" :
            string.Empty;
}