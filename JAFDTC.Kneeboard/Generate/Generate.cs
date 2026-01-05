// ********************************************************************************************************************
//
// Generate.cs -- kneeboard generator
//
// Copyright(C) 2026 rage
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

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
            generateCriteria.Mission.Required();
            generateCriteria.Mission.Owner.Required();
            generateCriteria.Mission.Packages.Required();
            generateCriteria.Mission.Packages[0].Required();
            generateCriteria.Mission.Packages[0].Flights.Required();
            generateCriteria.Mission.Packages[0].Flights[0].Required();

            //restict to what we currently support
            if (generateCriteria.Mission.Packages.Count != 1)
                throw new NotSupportedException("Currently only missions with a single Package are supported.");
            if (generateCriteria.Mission.Packages[0].Flights.Count != 1)
                throw new NotSupportedException("Currently only missions with a single Flight are supported.");

            //if (!string.IsNullOrWhiteSpace(generateCriteria.PathLogo))
            //    if (!File.Exists(generateCriteria.PathLogo))
            //        throw new FileNotFoundException($"Logo Not Found: {generateCriteria.PathLogo}");

            foreach (var package in generateCriteria.Mission.Packages)
                foreach (var flight in package.Flights) //KBs are at a Flight level for now.. maybe in future we would have mission/package levels (but those are just structural for now)
                {
                    var destinationPath = generateCriteria.PathOutput;
                    if (!Directory.Exists(destinationPath))
                        throw new DirectoryNotFoundException($"KB Destination Directory Not Found: {destinationPath}");

                    var templatePath = generateCriteria.PathTemplates;
                    if (!Directory.Exists(templatePath))
                        throw new DirectoryNotFoundException($"KB Template Directory Not Found: {templatePath}");

                    var templates = Directory.GetFiles(templatePath, "*.svg");
                    if (templates.IsEmpty())
                        throw new FileNotFoundException($"Template Directory has no files: {templatePath}");
                }

            using var mapper = new Mapper();
            var dict = mapper.Map(generateCriteria);

            var result = new List<string>();
            foreach (var package in generateCriteria.Mission.Packages)
            {
                foreach(var flight in package.Flights) //KBs are at a Flight level for now.. maybe in future we would have mission/package levels (but those are just structural for now)
                {
                    var templatePath = generateCriteria.PathTemplates;
                    var templates = Directory.GetFiles(templatePath, "*.svg");

                    foreach (var template in templates)
                    {
                        var safeFileName = string.Concat($"{generateCriteria.Name}-{generateCriteria.Mission.Name}-{flight.Name}-{Path.GetFileNameWithoutExtension(template)}").Replace(" ", "-");
                        foreach (var c in Path.GetInvalidFileNameChars())
                            safeFileName = safeFileName.Replace(c, '-');

                        var destinationPath = Path.Combine(generateCriteria.PathOutput, $"{safeFileName}.png");

                        using (var pkb = new Builder(dict, template, destinationPath))
                            pkb.Build();

                        result.Add(destinationPath);
                    }
                }
            }

            mapper.Dispose();

            return result;
        }

        public void Dispose()
        {
        }
    }
}
