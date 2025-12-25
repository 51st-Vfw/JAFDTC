using JAFDTC.Kneeboard.Models;

namespace JAFDTC.Kneeboard.Generate
{
    internal interface IGenerateKB : IDisposable
    {
        string Process(GenerateCriteria generateCriteria, string templateFilePath);
    }
}
