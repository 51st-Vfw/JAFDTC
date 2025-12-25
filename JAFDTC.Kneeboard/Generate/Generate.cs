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

        public string[] GenerateKneeboards(GenerateCriteria generateCriteria)
        {
            generateCriteria.Required();
            generateCriteria.PathOutput.Required();
            generateCriteria.PathTemplates.Required();
            generateCriteria.Airframe.Required();
            generateCriteria.Name.Required();

            var destinationPath = Path.Combine(generateCriteria.PathOutput, generateCriteria.Airframe); // out path, airframe, etc 
            if (!System.IO.Directory.Exists(destinationPath))
                throw new DirectoryNotFoundException($"KB Destination Directory Not Found: {destinationPath}");

            var templatePath = Path.Combine(generateCriteria.PathTemplates, generateCriteria.Airframe); // out path, airframe, etc 
            if (!System.IO.Directory.Exists(templatePath))
                throw new DirectoryNotFoundException($"KB Template Directory Not Found: {templatePath}");

            var templates = System.IO.Directory.GetFiles(templatePath, "*.svg");
            if (templates.IsEmpty())
                throw new FileNotFoundException("Template Directory has no files!");

            /*
             * validate criteria, paths, templates, etc etc
             * 
             * delete existing KBs based on paths/filenames
             * 
             * for each kb type generate its KB(s)
             * 
             */

            //temo for now..
            var generatedFiles = System.IO.Directory.GetFiles(destinationPath, "*.png");
            return generatedFiles;
        }

        public void Dispose()
        {
        }
    }
}
