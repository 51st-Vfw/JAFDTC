// ********************************************************************************************************************
//
// ImportHelperMIZ.cs -- helper to import navpoints from a .miz file
//
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using JAFDTC.Utilities.LsonLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// TODO: namespace and class names need to change here for consistency.

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// import helper class to extract navpoints from a flight in a dcs .miz file. flights from the .miz are only
    /// considered if the airframe matches an airframe type provided at consturction.
    /// </summary>
    public partial class ImportHelperMIZ : IExtractor
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private const double M_TO_FT = 3.2808399;

        private readonly Dictionary<string, UnitCategoryType> _mapKeyToCategory = new()
        {
            ["vehicle"] = UnitCategoryType.GROUND,
            ["plane"] = UnitCategoryType.AIRCRAFT,
            ["helicopter"] = UnitCategoryType.HELICOPTER,
            ["ship"] = UnitCategoryType.NAVAL
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperMIZ() { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IExtractor
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// returns a list of PositionItem for the route from the positions in groupInfo["route"]["points"]. throws
        /// an exception if there are translation errors.
        /// </summary>
        private static List<UnitPositionItem> ParseRoute(string theater, LsonDict groupDict, int startTime)
        {
            List<UnitPositionItem> route = [ ];
            if (groupDict.ContainsKey("route") &&
                groupDict["route"].GetDict().ContainsKey("points"))
            {
                LsonDict pointsArray = groupDict["route"].GetDict()["points"].GetDict();
                foreach (LsonNumber pointIndex in pointsArray.Keys.Cast<LsonNumber>())
                {
                    LsonDict point = pointsArray[pointIndex].GetDict();
                    double x = (double)point["x"].GetDecimal();
                    double z = (double)point["y"].GetDecimal();
                    CoordLL ll = CoordInterpolator.Instance.XZtoLL(theater, x, z)
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
        private static IReadOnlyList<UnitItem> ParseUnitsArray(string theater, LsonDict unitsArray)
        {
            List<UnitItem> units = [ ];
            foreach (LsonNumber unitIndex in unitsArray.Keys.Cast<LsonNumber>())
            {
                LsonDict unitDict = unitsArray[unitIndex].GetDict();
                double x = (double)unitDict["x"].GetDecimal();
                double z = (double)unitDict["y"].GetDecimal();
                CoordLL ll = CoordInterpolator.Instance.XZtoLL(theater, x, z)
                             ?? throw new Exception("Unit/Point coordiante translation fails");
                double alt = (unitDict.ContainsKey("alt")) ? (double)unitDict["alt"].GetDecimal() : 0.0;
                units.Add(new UnitItem()
                {
                    UniqueID = "miz_uid_" + unitDict["unitId"].GetDecimal().ToString(),
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
        private static IReadOnlyList<UnitGroupItem> ParseGroupContainer(ExtractCriteria criteria, CoalitionType coa,
                                                                        UnitCategoryType category, int startTime,
                                                                        LsonDict containerDict)
        {
            List<UnitGroupItem> groups = [ ];
            if (containerDict.ContainsKey("group"))
            {
                LsonDict groupArray = containerDict["group"].GetDict();
                foreach (LsonNumber groupIndex in groupArray.Keys.Cast<LsonNumber>())
                {
                    LsonDict groupDict = groupArray[groupIndex].GetDict();
                    groups.Add(new UnitGroupItem()
                    {
                        UniqueID = "miz_gid_" + groupDict["groupId"].GetDecimal().ToString(),
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

        /// <summary>
        /// extract the list of groups from the .miz file identified in the extraction criteria. returns list of
        /// groups matching the export criteria, null on failure.
        /// </summary>
        public IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria criteria)
        {
            List<UnitGroupItem> groups = [ ];

            try
            {
                criteria.Required();
                criteria.FilePath.Required();
                criteria.Theater ??= FileManager.ReadFileFromZip(criteria.FilePath, "theatre");
                criteria.Theater.Required();

                string lua = FileManager.ReadFileFromZip(criteria.FilePath, "mission");
                Dictionary<string, LsonValue> parsed = LsonVars.Parse(lua);

                int startTime = parsed["mission"].GetDict()["start_time"].GetInt();

                LsonDict coalitionDict = parsed["mission"].GetDict()["coalition"].GetDict();
                foreach (string coalitionKey in coalitionDict.Keys.Select(v => (string)v))
                {
                    CoalitionType coa = coalitionKey.ToLower() switch
                    {
                        "blue" => CoalitionType.BLUE,
                        "red" => CoalitionType.RED,
                        _ => CoalitionType.NEUTRAL
                    };

                    LsonDict countryArray = coalitionDict[coalitionKey].GetDict()["country"].GetDict();
                    foreach (LsonNumber countryIndex in countryArray.Keys.Cast<LsonNumber>())
                    {
                        LsonDict countryDict = countryArray[countryIndex].GetDict();
                        foreach (KeyValuePair<string, UnitCategoryType> kvp in _mapKeyToCategory)
                            if (countryDict.ContainsKey(kvp.Key))
                                groups.AddRange(ParseGroupContainer(criteria, coa, kvp.Value, startTime,
                                                                    countryDict[kvp.Key].GetDict()));
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"MIZ:Extract fails with exception {ex}");
                return null;
            }

            return [.. groups.LimitGroupsWithUnits()
                             .LimitCoalitions(criteria.Coalitions)
                             .LimitUnitCategories(criteria.UnitCategories) ];
        }
    }
}
