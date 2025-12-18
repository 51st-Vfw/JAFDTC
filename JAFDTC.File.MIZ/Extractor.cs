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
using JAFDTC.Core.Extensions;
using JAFDTC.Core.LsonLib;
using JAFDTC.File.Extensions;
using JAFDTC.File.MIZ.Helpers;
using JAFDTC.File.Models;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.File.MIZ
{
    public class Extractor : IExtractor
    {
        private const double M_TO_FT = 3.2808399;

        private static readonly Dictionary<string, UnitCategoryType> _mapKeyToCategory = new()
        {
            ["vehicle"] = UnitCategoryType.GROUND,
            ["plane"] = UnitCategoryType.AIRCRAFT,
            ["helicopter"] = UnitCategoryType.HELICOPTER,
            ["ship"] = UnitCategoryType.NAVAL
        };

        public IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria extractCriteria)
        {
            extractCriteria.Required();
            extractCriteria.FilePath.Required();

            if (!System.IO.File.Exists(extractCriteria.FilePath))
                throw new FileNotFoundException("MIZ file not found", extractCriteria.FilePath);

            extractCriteria.Theater ??= Core.IO.FileHelper.ReadAllText(extractCriteria.FilePath, "theatre");
            extractCriteria.Theater.Required();

            var lua = Core.IO.FileHelper.ReadAllText(extractCriteria.FilePath, "mission");
            var parsed = LsonVars.Parse(lua);
            var startTime = parsed["mission"].GetDict()["start_time"].GetInt();
            var coalitionDict = parsed["mission"].GetDict()["coalition"].GetDict();

            List<UnitGroupItem> groups = [];

            foreach (var coalitionKey in coalitionDict.Keys.Select(v => (string)v))
            {
                var coa = coalitionKey.ToLower() switch
                {
                    "blue" => CoalitionType.BLUE,
                    "red" => CoalitionType.RED,
                    _ => CoalitionType.NEUTRAL
                };

                var countryArray = coalitionDict[coalitionKey].GetDict()["country"].GetDict();
                foreach (var countryIndex in countryArray.Keys.Cast<LsonNumber>())
                {
                    var countryDict = countryArray[countryIndex].GetDict();
                    foreach (KeyValuePair<string, UnitCategoryType> kvp in _mapKeyToCategory)
                        if (countryDict.ContainsKey(kvp.Key))
                            groups.AddRange(ParseGroupContainer(extractCriteria, coa, kvp.Value, startTime,
                                                                countryDict[kvp.Key].GetDict()));
                }
            }

            var result = groups
                .LimitGroupsWithUnits()
                .LimitCoalitions(extractCriteria.Coalitions)
                .LimitUnitCategories(extractCriteria.UnitCategories)
                .ToList();

            return result;
        }

        /// <summary>
        /// returns a list of PositionItem for the route from the positions in groupInfo["route"]["points"]. throws
        /// an exception if there are translation errors.
        /// </summary>
        private static List<UnitPositionItem> ParseRoute(string theater, LsonDict groupDict, int startTime)
        {
            List<UnitPositionItem> route = [];

            if (groupDict.ContainsKey("route") &&
                groupDict["route"].GetDict().ContainsKey("points"))
            {
                var pointsArray = groupDict["route"].GetDict()["points"].GetDict();
                foreach (LsonNumber pointIndex in pointsArray.Keys.Cast<LsonNumber>())
                {
                    var point = pointsArray[pointIndex].GetDict();
                    double x = (double)point["x"].GetDecimal();
                    double z = (double)point["y"].GetDecimal();
                    var ll = CoordInterpolator.Instance.XZtoLL(theater, x, z)
                                 ?? throw new Exception("Group/Point coordiante translation fails");

                    UnitPositionItem posn = new()
                    {
                        Name = (point.ContainsKey("name")) ? point["name"].GetString() : null,
                        Latitude = ll.Lat,
                        Longitude = ll.Lon,
                        Altitude = (double)point["alt"].GetDecimal() * M_TO_FT,
                        TimeOn = -1.0
                    };
                    if (point.ContainsKey("ETA_locked") && point["ETA_locked"].GetBool())
                        posn.TimeOn = startTime + (double)point["ETA"].GetDecimal();

                    route.Add(posn);
                }
            }
            else
            {
                throw new Exception("Group missing route array");
            }

            return route;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static List<UnitItem> ParseUnitsArray(string theater, LsonDict unitsArray)
        {
            List<UnitItem> units = [];

            foreach (var unitIndex in unitsArray.Keys.Cast<LsonNumber>())
            {
                var unitDict = unitsArray[unitIndex].GetDict();
                double x = (double)unitDict["x"].GetDecimal();
                double z = (double)unitDict["y"].GetDecimal();
                var ll = CoordInterpolator.Instance.XZtoLL(theater, x, z)
                             ?? throw new Exception("Unit/Point coordiante translation fails");
                double alt = (unitDict.ContainsKey("alt")) ? (double)unitDict["alt"].GetDecimal() : 0.0;

                units.Add(new()
                {
                    UniqueID = "miz_u:" + unitDict["unitId"].GetDecimal().ToString(),
                    Type = unitDict["type"].GetString(),
                    Name = unitDict["name"].GetString(),
                    Position = new()
                    {
                        Name = (unitDict.ContainsKey("name")) ? unitDict["name"].GetString() : null,
                        Latitude = ll.Lat,
                        Longitude = ll.Lon,
                        Altitude = alt * M_TO_FT,
                        // TODO: TOS import?
                        TimeOn = -1.0
                    },
                    IsAlive = true,
                });
            }

            return units;
        }

        /// <summary>
        /// parse a group container dictionary. group containers have a "group" key with an array of group
        /// dictionaries value. returns a list of units in the group container (list is empty if no units found).
        /// throws an exception on error.
        /// </summary>
        private static List<UnitGroupItem> ParseGroupContainer(ExtractCriteria criteria, CoalitionType coa,
                                                                        UnitCategoryType category, int startTime,
                                                                        LsonDict containerDict)
        {
            List<UnitGroupItem> groups = [];
            if (containerDict.ContainsKey("group"))
            {
                var groupArray = containerDict["group"].GetDict();
                foreach (var groupIndex in groupArray.Keys.Cast<LsonNumber>())
                {
                    var groupDict = groupArray[groupIndex].GetDict();

                    groups.Add(new()
                    {
                        UniqueID = "miz_g:" + groupDict["groupId"].GetDecimal().ToString(),
                        Coalition = coa,
                        Category = category,
                        Name = groupDict["name"].GetString(),
                        Units = [.. ParseUnitsArray(criteria.Theater,
                                                    groupDict["units"].GetDict()).LimitUnitTypes(criteria.UnitTypes)
                                                                                 .LimitAlive(criteria.IsAlive) ],
                        Route = ParseRoute(criteria.Theater, groupDict, startTime)
                    });
                }
            }

            return groups;
        }

        public void Dispose()
        {         
        }
    }
}
