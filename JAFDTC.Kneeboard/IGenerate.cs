using JAFDTC.Kneeboard.Models;

namespace JAFDTC.Kneeboard
{
    public interface IGenerate : IDisposable
    {
        /// <summary>
        /// Generate all the Airframe Templates
        /// </summary>
        /// <param name="generateCriteria"></param>
        /// <returns>Filepath to all generated files</returns>
        IReadOnlyList<string> GenerateKneeboards(GenerateCriteria generateCriteria);
    }
}
