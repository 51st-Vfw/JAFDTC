using JAFDTC.TacView.Models;

namespace JAFDTC.TacView
{
    public interface IExtractor : IDisposable
    {
        IReadOnlyList<UnitItem> Extract(ExtractCriteria extractCriteria);
    }
}
