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
using System.Text;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;

[JsonObject(MemberSerialization.OptIn)]
public class Configuration
{
    public static Configuration Instance { get; set; } = null!;

    [JsonProperty]
    public int ConfigVersion { get; set; } = 0;

    [JsonProperty]
    private Dictionary<ulong, ulong> CIDToFCId { get; set; } = [];

    [JsonProperty]
    private Dictionary<ulong, FCData> FCData { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<FCData> AllFCData => this.FCData.Values;
    public ulong GetFCIdForCID(ulong cid) => this.CIDToFCId.TryGetValue(cid, out ulong fcId) ? fcId : 0;
    
    public void ClearData() => this.FCData.Clear();

    public unsafe void UpdateFCData()
    {
        InfoProxyFreeCompany* fcProxy = InfoProxyFreeCompany.Instance();

        this.CIDToFCId[Player.CID] = fcProxy->Id;

        if (!this.FCData.TryGetValue(fcProxy->Id, out FCData? fcData))
        {
            fcData = new FCData
                     {
                         HomeWorldId  = fcProxy->HomeWorldId,
                         Id           = fcProxy->Id,
                         GrandCompany = fcProxy->GrandCompany,
                     };
        }



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


        arrayData = RaptureAtkModule.Instance()->GetStringArrayData(64);
        if (arrayData->Size > 2)
        {
            CStringPointer x        = arrayData->StringArray[3];
            SeString       seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(x));
            fcData.HouseTemp = seString.GetText();
        }


        fcData.FCName   = fcProxy->NameString;
        fcData.TotalMembers = fcProxy->TotalMembers;
        fcData.MasterString = fcProxy->MasterString;
        fcData.Rank         = fcProxy->Rank;
        

        this.FCData[fcProxy->Id] = fcData;

        this.Save();
    }

    public FCData? Data { get; set; }

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
    public string HouseTemp { get; set; } = string.Empty;
}