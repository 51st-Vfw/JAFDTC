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

            if (!string.IsNullOrWhiteSpace(generateCriteria.PathLogo))
                if (!System.IO.File.Exists(generateCriteria.PathLogo))
                    throw new FileNotFoundException($"Logo Not Found: {generateCriteria.PathLogo}");

            var templates = System.IO.Directory.GetFiles(templatePath, "*.svg");
            if (templates.IsEmpty())
                throw new FileNotFoundException("Template Directory has no files!");

            var result = templates
                .AsParallel()
                .Select(p =>
                {
                    using var pkb = new KneeboardBuilder(generateCriteria, p);
                    return pkb.Build();
                })
                .ToList();

            return result;
        }

        public void Dispose()
        {
        }
    }
}
