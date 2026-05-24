using System;
using System.Collections.Generic;
using System.Text;

namespace FCTracker.Services
{
    using Lumina.Excel.Sheets;

    public interface ICharDataProvider
    {
        IReadOnlyList<CharData> GetAllChars();
        IReadOnlyList<CharData> GetAllCharsWithoutFC();
        int                     GetCharCountForWorld(World world);
    }
}
