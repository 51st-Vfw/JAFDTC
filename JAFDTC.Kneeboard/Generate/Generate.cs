using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Kneeboard.Generate
{
    public class Generate : IGenerate
    {
        public Generate() { }

        public void GenerateKneeboards(GenerateCriteria generateCriteria)
        {
            generateCriteria.Required();
            generateCriteria.PathOutput.Required();

            //var destinationPath = ""; // out path, airframe, etc 
            //if (!System.IO.Directory.Exists(destinationPath))
            //    throw new DirectoryNotFoundException($"KB Destination Directory Not Found: {destinationPath}");

            /*
             * validate criteria, paths, templates, etc etc
             * 
             * delete existing KBs based on paths/filenames
             * 
             * for each kb type generate its KB(s)
             * 
             */
        }

        public void Dispose()
        {
        }
    }
}
