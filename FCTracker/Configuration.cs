namespace FCTracker;

using AutoRetainerAPI.Configuration;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.IPC;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using NightmareUI.Censoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UI;
using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;

[JsonObject(MemberSerialization.OptIn)]
public class Configuration
{
    public static Configuration Instance { get; set; } = null!;

    [JsonProperty]
    public GatheredData GatheredData { get; set; } = new();
    public List<GatheredData> ImportedData { get; set; } = [];

    [JsonProperty]
    public List<DataImportConfig> DataImportConfig {get; set; } = [];

    [JsonProperty]
    public int ConfigVersion { get; set; } = 0;

    [JsonProperty]
    public CharViewData CharViewData { get; set; }

    public static  AutoRetainerAPI.AutoRetainerApi AR_API = new();

    private static ARData? arData;

    public static ARData ARData
    {
        get
        {
            if (arData.HasValue)
                return arData.Value;

            ARData data = new();

            foreach (FCData fcData in Instance.AllFCData.Values)
            {
                ARData? fcARData = fcData.AutoRetainerData;
                data.RepairCount += fcARData?.RepairCount ?? 0;
                data.FuelCount += fcARData?.FuelCount ?? 0;
            }
            arData = data;

            return arData.Value;
        }
    }

    public void RefreshImportedData()
    {
        this.ImportedData.Clear();

        foreach (DataImportConfig config in this.DataImportConfig)
        {
            GatheredData? data = config.LoadData();
            if (data != null)
                this.ImportedData.Add(data);
        }

        ARDataBust();
    }

    public static void ARDataBust() => arData = null;
    
    public Dictionary<ulong, CharData> AllCharData
    {
        get
        {
            Dictionary<ulong, CharData> allCharData = new(this.GatheredData.CharByCID);
            foreach (GatheredData data in this.ImportedData)
                allCharData.AddRange(data.CharByCID);

            return allCharData;
        }
    }

    public Dictionary<ulong, FCData> AllFCData
    {
        get
        {
            Dictionary<ulong, FCData> allFCData = new(this.GatheredData.FCData);
            foreach (GatheredData data in this.ImportedData)
                allFCData.AddRange(data.FCData);

            return allFCData;
        }
    }

    public ulong? GetFCIdForCID(ulong cid) => 
        this.GatheredData.CharByCID.TryGetValue(cid, out CharData charData) ? charData.FC : null;
    
    public void ClearData() => 
        this.GatheredData.FCData.Clear();

    public void RemoveCurrentFCData()
    {
        if(Player.Available)
            if(this.GatheredData.CharByCID.TryGetValue(Player.CID, out CharData charData))
                if(charData.FC.HasValue)
                    this.GatheredData.FCData.Remove(charData.FC.Value);
    }

    public void UpdateCharData(CharData ch)
    {
        this.GatheredData.CharByCID[ch.CID] = ch;
        this.Save();
    }

    public void UpdateCurrentCharData()
    {
        if(!this.GatheredData.CharByCID.TryGetValue(Player.CID, out CharData charData))
            charData = new CharData { CID = Player.CID };

        this.GatheredData.CharByCID[Player.CID] = charData with
                                     {
                                         Name              = Player.Name,
                                         WorldId           = Player.HomeWorld.RowId,
                                         GrandCompany      = (GrandCompany)Player.GrandCompany,
                                         GrandCompanyRank  = PlayerHelper.GetGrandCompanyRank(),
                                         LeveAllowances    = Math.Min(100, PlayerHelper.LeveAllowances + 3),
                                         LeveAllowanceTime = QuestManager.GetNextLeveAllowancesDateTime(),
                                         HighestLevelCombat = PlayerHelper.GetHighestCombatLevelFromSheet(),
                                         HighestLevelGathering = PlayerHelper.GetHighestGatheringLevelFromSheet(),
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

        this.GatheredData.CharByCID[Player.CID] = this.GatheredData.CharByCID[Player.CID] with {FC = fcProxy->Id};

        if (!this.GatheredData.FCData.TryGetValue(fcProxy->Id, out FCData? fcData))
        {
            fcData = new FCData
                     {
                         HomeWorldId  = fcProxy->HomeWorldId,
                         Id           = fcProxy->Id,
                         GrandCompany = fcProxy->GrandCompany,
                     };
        }

        fcData.MemberCIDs.Add(Player.CID);

        int date = AgentFreeCompanyProfile.Instance()->FoundationDate;
        fcData.FoundingDate = DateTimeOffset.FromUnixTimeSeconds(date).DateTime;


        StringArrayData* arrayData = RaptureAtkModule.Instance()->GetStringArrayData(48);
        if (arrayData->Size > 1)
        {
            CStringPointer x        = arrayData->StringArray[2];
            SeString       seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(x));

            fcData.Tag = seString.GetText();
        }

        fcData.FCPoints = *(int*)((nint)AgentModule.Instance()->GetAgentByInternalId(AgentId.FreeCompanyCreditShop) + 256);

        HouseId houseId = HousingManager.GetOwnedHouseId(EstateType.FreeCompanyEstate);
        if (houseId.Unit.Value < 255)
        {
            FCData.HouseInfo.ResidentialAetheryteKind? aetheryteKind = FCData.HouseInfo.GetResidentialAetheryteByTerritoryType(houseId.TerritoryTypeId);
            if (!fcData.HasHouse                        ||
                fcData.House!.Ward != houseId.WardIndex ||
                fcData.House.Plot != houseId.PlotIndex  ||
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

        this.GatheredData.FCData[fcProxy->Id] = fcData;

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
public class DataImportConfig
{
    public bool Enabled { get; set; }
    public required string Path { get; set; }

    [JsonIgnore]
    public GatheredData? Data;

    public GatheredData? LoadData()
    {
        if (!this.Enabled)
            return null;
        try
        {

            if(File.Exists(this.Path))
            {
                string json;
                using (StreamReader streamReader = new(this.Path, Encoding.UTF8))
                    json = streamReader.ReadToEnd();

                Configuration? node    = JsonConvert.DeserializeObject<Configuration>(json);
                if (node?.GatheredData != null)
                {
                    node.GatheredData.ImportSourceConfig = this;

                    foreach (ulong fcID in node.GatheredData.FCData.Keys)
                        node.GatheredData.FCData[fcID].SourceData = node.GatheredData;

                    foreach (ulong cid in node.GatheredData.CharByCID.Keys)
                        node.GatheredData.CharByCID[cid] = node.GatheredData.CharByCID[cid] with { SourceData = node.GatheredData };

                    return this.Data = node.GatheredData;
                }
            }
        }
        catch (Exception e)
        {
            Svc.Log.Error(e, $"Error occurred while loading data from {this.Path}");
            return null;
        }
        return null;
    }
}

[JsonObject(MemberSerialization.OptOut)]
public class GatheredData
{
    public GatheredData()
    {
    }

    [JsonIgnore]
    public DataImportConfig? ImportSourceConfig;

    public Dictionary<ulong, CharData> CharByCID { get; set; } = [];

    public Dictionary<ulong, FCData> FCData { get; set; } = [];
}

[JsonObject(MemberSerialization.OptOut)]
public struct CharViewData
{
    public bool CharsWithFC;

    public bool CharsWithFCWithHouse;
}

[JsonObject(MemberSerialization.OptOut)]
public struct CharData
{
    [JsonIgnore]
    public GatheredData SourceData
    {
        get => field ??= Configuration.Instance.GatheredData;
        set;
    }

    public required ulong        CID;
    public          string       Name;
    public          uint         WorldId;
    public          ulong?       FC;

    [JsonProperty]
    private uint gil;
    [JsonIgnore]
    public uint Gil
    {
        get
        {
            if (ECommonsIPC.AllaganTools.Available && this.SourceData.ImportSourceConfig == null && EzThrottler.Throttle("GilCheck_" + this.CID, 300_0000))
            {
                this.gil = ECommonsIPC.AllaganTools.ItemCount(1u, this.CID, -1);
                Configuration.Instance.UpdateCharData(this);
            }

            return this.gil;
        }
    }

    public GrandCompany GrandCompany;
    public uint         GrandCompanyRank;

    public short HighestLevelCombat;
    public short HighestLevelGathering;

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
            return timePassed.Ticks <= 0 ?
                       this.LeveAllowances - 3 :
                       timePassed.TotalDays > 30 ?
                           100 :
                           Math.Min(100, this.LeveAllowances + (int)(timePassed.TotalHours / 12));
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
    private GatheredData? sourceData;
    [JsonIgnore] 
    public GatheredData SourceData { 
        get => this.sourceData ??= Configuration.Instance.GatheredData;
        set => this.sourceData = value;
    }

    [JsonIgnore]
    public World? World => field ??= ExcelWorldHelper.Get(this.HomeWorldId);
    public ulong          Id           { get; set; }
    public string         FCName       { get; set; } = string.Empty;
    public string         Tag          { get; set; } = string.Empty;
    public uint           TotalMembers { get; set; }
    public string         MasterString { get; set; } = string.Empty;
    public uint           HomeWorldId  { get; set; }
    public GrandCompany   GrandCompany { get; set; }
    public uint           Rank         { get; set; }
    public DateTime       FoundingDate { get; set; }
    public int            FCPoints     { get; set; }
    public HashSet<ulong> MemberCIDs   { get; set; } = [];

    [JsonProperty]
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
                    if(data != null)
                    {
                        arData.RepairCount += data.RepairKits;
                        arData.FuelCount   += data.Ceruleum;
                    }
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
    public string Region => FCTrackerTheme.RegionString(this.World);

    [JsonIgnore]
    public bool HasHouse => 
        this.House != null;

    [JsonIgnore]
    public TimeSpan TimeSinceFounded =>
        this.FoundingDate == default ?
            TimeSpan.Zero :
            DateTime.Now - this.FoundingDate;

    [JsonIgnore]
    public TimeSpan TimeUntilEligible => 
        new TimeSpan(30, 0, 0, 0) - this.TimeSinceFounded;

    [JsonIgnore]
    public bool IsEligible => 
        this.TimeSinceFounded.TotalDays >= 30 && this.Rank >= 6 && !this.HasHouse;

    [JsonIgnore]
    public DateTime EligibilityDate => 
        this.FoundingDate == default ? DateTime.MaxValue : this.FoundingDate.AddDays(30);

    [JsonIgnore]
    private bool? masterAvailable;

    [JsonIgnore]
    public bool MasterAvailable =>
        this.masterAvailable ??= this.SourceData.ImportSourceConfig == null && Configuration.Instance.GatheredData.CharByCID.Any(ch => this.MemberCIDs.Contains(ch.Value.CID) && ch.Value.Name == this.MasterString);

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
                this.TimeUntilEligible.TotalDays <= 7 ? 
                    HousingStatusCategory.Soon : 
                    HousingStatusCategory.Waiting;

    public string GetHousingStatusText() =>
        this.HasHouse ?
            $"{this.House!.City} - Ward {this.House.Ward + 1} - Plot {this.House.Plot + 1}" :
            this.IsEligible ?
                "Eligible" :
                this.FoundingDate == default ?
                    "Not yet founded" :
                    this.TimeSinceFounded.TotalDays >= 30 ?
                        "30d passed. Check Upcoming tab" :
                        $@"{this.@TimeUntilEligible:%d\d\ %h\h} left";

    public string GetHousingDemolitionText() =>
        this.HasHouse ?
            $"{(this.House!.LastVisited != null ? $"Last Visited: {(this.House.DaysSinceLastVisit > 0 ? $"{this.House.DaysSinceLastVisit}d ago" : "Today")}" : "Never visited")}" :
            string.Empty;

    [JsonIgnore]
    public bool LoggedIn =>
        FCTrackerPlugin.LoggedInCID.HasValue && this.MemberCIDs.Contains(FCTrackerPlugin.LoggedInCID.Value);
}