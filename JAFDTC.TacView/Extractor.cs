// ********************************************************************************************************************
//
// Extractor.cs -- <one_line_descripti8on>
//
// Copyright(C) 2025 rage
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
using JAFDTC.File.ACMI.Extensions;
using JAFDTC.File.ACMI.Parsers;
using JAFDTC.File.ACMI.Models;
using System.Globalization;
using JAFDTC.Models.Units;
using JAFDTC.Core.Extensions;
using JAFDTC.File.Models;
using JAFDTC.File.Extensions;
using JAFDTC.Models.Core;

namespace JAFDTC.File.ACMI
{
    public class Extractor : IExtractor
    {
        public IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria extractCriteria)
        {
            extractCriteria.Required();
            extractCriteria.FilePath.Required();

            if (!System.IO.File.Exists(extractCriteria.FilePath))
                throw new FileNotFoundException("ACMI file not found", extractCriteria.FilePath);

            var rawData = ReadACMI(extractCriteria.FilePath);
            var allLines = GetRawLines(rawData);
            rawData = null;
            if (!allLines.Any())
                throw new InvalidDataException("ACMI file contains no data");

            var timeMarker = GetTimeMarker(allLines, extractCriteria.TimeSnippet);

            var parsedUnits = GetUnits(allLines, timeMarker);
            if (parsedUnits.IsEmpty())
                throw new InvalidDataException("ACMI file contains no units");

            allLines.Clear();

            var groupedUnits = ConvertToGroup(parsedUnits);
            if (groupedUnits.IsEmpty())
                throw new InvalidDataException("ParsedUnits contains no groups");

            parsedUnits = null;

            var result = groupedUnits
                .LimitGroupsWithUnits()
                .LimitCoalitions(extractCriteria.Coalitions)
                .LimitUnitCategories(extractCriteria.UnitCategories)
                .LimitUnitTypes(extractCriteria.UnitTypes)
                .LimitAlive(extractCriteria.IsAlive)
                .ToList();

            return result;
        }

        internal static string ReadACMI(string filePath)
        {
            if (filePath.EndsWith(".zip.acmi", StringComparison.OrdinalIgnoreCase))
                return Core.IO.FileHelper.ReadAllText(filePath, 0);

            return Core.IO.FileHelper.ReadAllText(filePath);
        }

        internal static List<string> GetRawLines(string rawData)
        {
            return rawData
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(l => l?.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList()!;
        }

        internal static double GetTimeMarker(List<string> lines, DateTimeOffset? timeSnippet)
        {
            // parse ReferenceTime (header "0,ReferenceTime=2025-02-21T11:00:01Z")
            DateTimeOffset referenceTime = DateTimeOffset.MinValue;
            var refLine = lines.FirstOrDefault(l => l.StartsWith("0,ReferenceTime=", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(refLine))
            {
                var idx = refLine.IndexOf('=');
                if (idx >= 0)
                {
                    var val = refLine.Substring(idx + 1).Trim();
                    if (!DateTimeOffset.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out referenceTime))
                    {
                        referenceTime = DateTimeOffset.MinValue;
                    }
                }
            }

            // collect all timestamp markers (numeric seconds after ReferenceTime)
            var markers = new List<double>();
            foreach (var l in lines)
            {
                if (l.StartsWith("#", StringComparison.Ordinal))
                {
                    var s = l.Substring(1).Trim();
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        markers.Add(sec);
                }
            }

            if (!markers.Any())
                return 0;

            // choose marker to extract: if TimeSnippet provided pick nearest marker; otherwise pick last marker
            if (timeSnippet.HasValue && referenceTime != DateTimeOffset.MinValue)
            {
                var alignedTime = DateTime.Parse($"{referenceTime.ToString("MM/dd/yyyy")} {timeSnippet.Value.ToString("hh:mm:ss")}");
                var sourceTime = DateTime.Parse($"{referenceTime.ToString("MM/dd/yyyy")} {referenceTime.ToString("hh:mm:ss")}");

                var targetSeconds = (alignedTime - sourceTime).TotalSeconds;
                return markers.OrderBy(m => Math.Abs(m - targetSeconds)).First();
            }

            return markers.Max();
        }

        internal static IEnumerable<ParsedUnit> GetUnits(List<string> lines, double timeMarker)
        {
            var result = new Dictionary<string, ParsedUnit>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines) //i HATE event streams...
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) //linear log of time marker...
                {
                    if (double.TryParse(line.Substring(1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var currentMarker))
                    {
                        if (currentMarker > timeMarker)
                            break; //we are passed the marker we care about.. ignore anything further....
                    }
                    else
                    {
                        //throw error???
                    }
                } 
                else if (line.Contains("Color=")) //primary instance of the unit...
                {
                    var unitItem = UnitParser.Parse(line);
                    if (unitItem != null)
                        result[unitItem.Id] = unitItem; //last one wins...
                }
                else if (line.StartsWith("-", StringComparison.Ordinal)) //unit was deleted/killed
                {
                    if (result.TryGetValue(line.Substring(1).Trim(), out var u))
                        u.IsAlive = false;
                }
                else if (line.Contains("T=")) //subset of data that is just position updates... existing units on the move...
                {
                    var id = line[..line.IndexOf(',')];
                    if (result.TryGetValue(id, out var u))
                        u.Position = PositionParser.Parse(line[(id.Length + 1)..].ToCleanValue()) ?? u.Position; //last update wins... if its null (ie didnt move), use the previous position...
                }
            }

            return result.Values;
        }

        internal static IEnumerable<UnitGroupItem> ConvertToGroup(IEnumerable<ParsedUnit> units)
        {
            var dict = new Dictionary<string, UnitGroupItem>(StringComparer.OrdinalIgnoreCase);

            //var cats = units.Select(p=>p.Category.ToNormalized()).Distinct().ToList();

            foreach (var unit in units)
            {
                if (!dict.TryGetValue(unit.GroupName, out var group))
                    dict[unit.GroupName] = new()
                    {
                        Category = ToCategory(unit.Category),
                        Coalition = ToCoalition(unit.Color),
                        Name = unit.GroupName,
                        UniqueID = $"acmi_u:{unit.Id}",
                        Units = [],
                        Route = [] //no routes from ACMI files
                    };

                dict[unit.GroupName].Units.Add(new()
                {
                    IsAlive = unit.IsAlive,
                    Name = unit.UnitName,
                    UniqueID = $"acmi_g:{unit.Id}",
                    Position = new()
                    {
                        Altitude = unit.Position.Altitude,
                        Latitude = unit.Position.Latitude,
                        Longitude = unit.Position.Longitude
                    },
                    Type = unit.Unit
                });
            }

            return dict.Values;
        }

        private static UnitCategoryType ToCategory(string? value)
        {

            //this is a PITA...
            var norm  = value.ToNormalized();

            if (norm.StartsWith("SEA"))
                return UnitCategoryType.NAVAL;

            if (norm.StartsWith("GROUND"))
                return UnitCategoryType.GROUND;

            if (norm.StartsWith("AIR"))
            {
                if (norm.StartsWith("AIR_ROTORCRAFT"))
                    return UnitCategoryType.HELICOPTER;

                return UnitCategoryType.AIRCRAFT;
            }

            return UnitCategoryType.UNKNOWN; //shrapnel, bullseye... others?
            //throw new NotSupportedException($"Unsupported UnitCategoryType value: {value.ToNormalized()}");
        }

        private static CoalitionType ToCoalition(string? value)
        {
            switch (value.ToNormalized())
            {
                case "BLUE":
                    return CoalitionType.BLUE;
                case "RED":
                    return CoalitionType.RED;
                case "NEUTRAL":
                case "GREY":
                case "GREEN": //what do you want to do with this???
                    return CoalitionType.NEUTRAL;
            }

            throw new NotSupportedException($"Unsupported CoalitionType value: {value.ToNormalized()}");
        }

        public void Dispose()
        {         
        }
    }
}
