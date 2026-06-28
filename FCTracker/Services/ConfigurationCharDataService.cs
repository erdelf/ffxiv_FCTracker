using System;
using System.Collections.Generic;
using System.Text;

namespace FCTracker.Services
{
    using System.Linq;
    using Lumina.Excel.Sheets;

    internal class ConfigurationCharDataService : ICharDataProvider
    {
        private static IEnumerable<CharData> Source => Configuration.Instance?.AllCharData.Values.ToList() ?? [];

        public IReadOnlyList<CharData> GetAllChars()            => Source.ToList();
        public IReadOnlyList<CharData> GetAllCharsWithFCHouse() => 
            Source.Where(c => c.FC == null || !(Configuration.Instance.AllFCData.TryGetValue(c.FC.Value, out FCData? fcData) && fcData.HasHouse)).ToList();

        public IReadOnlyList<CharData> GetAllCharsWithoutFC()            => Source.Where(c => c.FC == null).ToList();
        public int                     GetCharCountForWorld(World world) => Source.Count(c => c.World!.Value.RowId == world.RowId);
    }
}
