using JAFDTC.Kneeboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Kneeboard.Generate
{
    internal interface IGenerateKB : IDisposable
    {
        string Process(GenerateCriteria generateCriteria, string templateFilePath);
    }
}
