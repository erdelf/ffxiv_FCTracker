namespace FCTracker.UI.Views;

using System.Collections.Generic;
using System.Linq;
using FCTracker.Services;

public class FCViewContext
{
    public IFCDataProvider Data { get; init; } = null!;
    public FCTrackerSidebar Sidebar { get; init; } = null!;
    public string SearchText { get; init; } = string.Empty;

    public IReadOnlyList<FCData> GetFilteredFCs()
    {
        IEnumerable<FCData> fcs = this.Data.GetAllFCs();

        if (!string.IsNullOrEmpty(this.Sidebar.SelectedWorld))
        {
            fcs = fcs.Where(fc => fc.WorldName == this.Sidebar.SelectedWorld);
        }
        else if (!string.IsNullOrEmpty(this.Sidebar.SelectedDatacenter))
        {
            fcs = fcs.Where(fc => fc.Datacenter == this.Sidebar.SelectedDatacenter);
        }
        else if (!string.IsNullOrEmpty(this.Sidebar.SelectedRegion))
        {
            fcs = fcs.Where(fc => fc.Region == this.Sidebar.SelectedRegion);
        }

        if (!string.IsNullOrEmpty(this.SearchText))
        {
            string search = this.SearchText.ToLowerInvariant();
            fcs = fcs.Where(fc =>
                fc.FCName.ToLowerInvariant().Contains(search) ||
                fc.Tag.ToLowerInvariant().Contains(search) ||
                fc.MasterString.ToLowerInvariant().Contains(search) ||
                fc.WorldName.ToLowerInvariant().Contains(search));
        }

        return fcs.ToList();
    }
}
