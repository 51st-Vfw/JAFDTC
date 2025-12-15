using JAFDTC.File.Models;
using JAFDTC.Models.Units;

namespace JAFDTC.File
{
    public interface IExtractor : IDisposable
    {
        IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria extractCriteria);
    }
}
