using JAFDTC.Kneeboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
