namespace FCTracker.Services;

using System.Collections.Generic;

public interface IFCDataProvider
{
    ICharDataProvider     CharData();
    IReadOnlyList<FCData> GetAllFCs();

    IReadOnlyList<FCData> GetEligibleFCs();
    IReadOnlyList<FCData> GetUpcomingFCs();
    IReadOnlyList<FCData> GetOwnedHousingFCs();

    int GetTotalCount();
    int GetReadyCount();
    int GetUpcomingCount();
    int GetPending7DayCount();
    int GetPending30DayCount();

    IEnumerable<string> GetRegions();
    IEnumerable<string> GetDatacenters();
    IEnumerable<string> GetWorlds();

    Dictionary<string, List<string>> GetDatacentersByRegion();
    Dictionary<string, List<string>> GetWorldsByDatacenter();

    int GetFCCountForWorld(string world);
    int GetFCCountForDatacenter(string datacenter);
    int GetFCCountForRegion(string region);

    (int Done, int Ready, int Upcoming, int Total) GetStatusCountsForWorld(string world);
    (int Done, int Ready, int Upcoming, int Total) GetStatusCountsForDatacenter(string datacenter);
    (int Done, int Ready, int Upcoming, int Total) GetStatusCountsForRegion(string region);
}
