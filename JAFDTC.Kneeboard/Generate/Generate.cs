using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;

namespace JAFDTC.Kneeboard.Generate
{
    public class Generate : IGenerate
    {
        public Generate() { }

        public IReadOnlyList<string> GenerateKneeboards(GenerateCriteria generateCriteria)
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

            var result = templates
                .AsParallel()
                .Select(p=> GenFactory(generateCriteria, p))
                .ToList();

            return result;
        }

        private static string GenFactory(GenerateCriteria generateCriteria, string template)
        {
            IGenerateKB generateKB = null;
            if (template.Contains("comm"))
                generateKB = new GenerateComms();
            else if (template.Contains("airfield"))
                generateKB = new GenerateAirfields();
            else if (template.Contains("flight"))
                generateKB = new GenerateFlights();
            else if (template.Contains("map"))
                generateKB = new GenerateMaps();
            else if (template.Contains("steer"))
                generateKB = new GenerateSteerpoints();
            else
                throw new NotSupportedException(template);

            return generateKB.Process(generateCriteria, template);
        }

        public void Dispose()
        {
        }
    }
}
