using JAFDTC.TacView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.TacView
{
    public interface IExtractor : IDisposable
    {
        IReadOnlyList<UnitItem> Extract(ExtractCriteria extractCriteria);
    }
}
