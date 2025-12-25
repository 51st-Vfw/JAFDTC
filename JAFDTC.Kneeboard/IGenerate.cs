using JAFDTC.Kneeboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Kneeboard
{
    public interface IGenerate : IDisposable
    {
        void GenerateKneeboards(GenerateCriteria generateCriteria);
    }
}
