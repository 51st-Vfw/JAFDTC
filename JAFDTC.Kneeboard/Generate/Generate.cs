using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;

namespace JAFDTC.Kneeboard.Generate
{
    public class Generate : IGenerate
    {
        public IReadOnlyList<string> GenerateKneeboards(GenerateCriteria generateCriteria)
        {
            generateCriteria.Required();
            generateCriteria.PathOutput.Required();
            generateCriteria.PathTemplates.Required();
            generateCriteria.Name.Required();
            generateCriteria.Owner.Required();
            generateCriteria.Mission.Required();
            generateCriteria.Mission.Packages.Required();
            generateCriteria.Mission.Packages[0].Required();
            generateCriteria.Mission.Packages[0].Flights.Required();
            generateCriteria.Mission.Packages[0].Flights[0].Required();

            if (!string.IsNullOrWhiteSpace(generateCriteria.PathLogo))
                if (!File.Exists(generateCriteria.PathLogo))
                    throw new FileNotFoundException($"Logo Not Found: {generateCriteria.PathLogo}");

            foreach (var package in generateCriteria.Mission.Packages)
                foreach (var flight in package.Flights) //KBs are at a Flight level for now.. maybe in future we would have mission/package levels (but those are just structural for now)
                {
                    var destinationPath = Path.Combine(generateCriteria.PathOutput, flight.Aircraft);
                    if (!Directory.Exists(destinationPath))
                        throw new DirectoryNotFoundException($"KB Destination Directory Not Found: {destinationPath}");

                    var templatePath = Path.Combine(generateCriteria.PathTemplates, flight.Aircraft);
                    if (!Directory.Exists(templatePath))
                        throw new DirectoryNotFoundException($"KB Template Directory Not Found: {templatePath}");

                    var templates = Directory.GetFiles(templatePath, "*.svg");
                    if (templates.IsEmpty())
                        throw new FileNotFoundException($"Template Directory has no files: {templatePath}");
                }

            var dict = generateCriteria.ToDataDictionary();

            var result = new List<string>();
            foreach (var package in generateCriteria.Mission.Packages)
            {
                foreach(var flight in package.Flights) //KBs are at a Flight level for now.. maybe in future we would have mission/package levels (but those are just structural for now)
                {
                    var templatePath = Path.Combine(generateCriteria.PathTemplates, flight.Aircraft);
                    var templates = Directory.GetFiles(templatePath, "*.svg");

                    foreach (var template in templates)
                    {
                        var safeFileName = string.Concat($"{generateCriteria.Name}_{Path.GetFileNameWithoutExtension(template)}");
                        foreach (var c in Path.GetInvalidFileNameChars())
                            safeFileName = safeFileName.Replace(c, '-');

                        var destinationPath = Path.Combine(generateCriteria.PathOutput, flight.Aircraft, $"{safeFileName}.png");

                        using (var pkb = new Builder(dict, template, destinationPath))
                            pkb.Build();

                        result.Add(destinationPath);
                    }
                }
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
