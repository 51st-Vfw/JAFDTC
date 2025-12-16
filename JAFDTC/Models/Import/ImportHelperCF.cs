// ********************************************************************************************************************
//
// ImportHelperCF.cs -- helper to import navpoints from a .cf file
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

using JAFDTC.File;
using JAFDTC.File.Extensions;
using JAFDTC.File.Models;
using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.Units;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

// TODO: namespace and class names need to change here for consistency.

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// import helper class to extract navpoints from a flight in a combatflite .cf file. flights from the .cf are only
    /// considered if the airframe matches an airframe type provided at consturction.
    /// </summary>
    public partial class ImportHelperCF : IExtractor
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        [GeneratedRegex(@"(?i)^.+\s+(\d\d|\d):(\d\d):(\d\d)\s+(am|pm)")]
        private static partial Regex TimeRegex();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperCF() { }

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
        /// walks the waypoints in a "waypoints" element from the xml creating a list of waypoints that make
        /// up the route.
        /// </summary>
        private static List<UnitPositionItem> ParseRoutePath(XmlNode waypoints)
        {
            List<UnitPositionItem> navpts = [ ];

            foreach (XmlNode waypoint in waypoints.ChildNodes)
            {
                UnitPositionItem navpt = new()
                {
                    Name = waypoint.SelectSingleNode("Name").InnerText,
                    Latitude = double.Parse(waypoint.SelectSingleNode("Lat").InnerText),
                    Longitude = double.Parse(waypoint.SelectSingleNode("Lon").InnerText),
                    TimeOn = -1.0
                };
                if (!double.TryParse(waypoint.SelectSingleNode("Altitude").InnerText, out double alt))
                    alt = 0.0;
                navpt.Altitude = alt;

                string tot = waypoint.SelectSingleNode("TOT").InnerText;
                if (tot != null)
                {
                    Match match = TimeRegex().Match(tot);
                    if (match.Success)
                    {
                        int h = int.Parse(match.Groups[1].Value);
                        h = match.Groups[4].Value.ToLower() switch
                        {
                            "am" => (h < 12) ? h : 0,
                            "pm" => (h < 12) ? h + 12 : h,
                            _ => h
                        };
                        navpt.TimeOn = (h * 3600) +
                                       (int.Parse(match.Groups[2].Value) * 60) +
                                       (int.Parse(match.Groups[3].Value) * 1);
                    }
                }

                navpts.Add(navpt);
            }

            return navpts;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static List<UnitItem> ParseRouteFlight(XmlNode flightMembers, string flightName,
                                                       UnitPositionItem position)
        {
            List<UnitItem> units = [ ];

            int index = 1;
            foreach (XmlNode unit in flightMembers.ChildNodes)
            {
                XmlNode aircraft = unit.SelectSingleNode("Aircraft");
                UnitItem unitItem = new()
                {
                    UniqueID = $"cf_u:{flightName}-{index++}",
                    Type = aircraft.SelectSingleNode("Type").InnerText,
                    Name = $"{flightName}-{index++}",
                    Position = position,
                    IsAlive = true
                };
                units.Add(unitItem);
            }

            return units;
        }

        /// <summary>
        /// extract the list of groups from the .miz file identified in the extraction criteria. returns list of
        /// groups matching the export criteria, null on failure.
        /// </summary>
        public IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria criteria)
        {
            List<UnitGroupItem> groups = [ ];
            List<string> flights = [ ];

            try
            {
                criteria.Required();
                criteria.FilePath.Required();

                string[] unitTypes = criteria.UnitTypes ?? [ ];

                XmlDocument xmlDoc = new();
                string xml = FileManager.ReadFileFromZip(criteria.FilePath, "mission.xml");
                xmlDoc.LoadXml(xml);

                // TODO: this parser is focused on extracting navpoints and does not currently handle ground units
                // TODO: or other non-aircraft types. not clear it's worth the squeeze to generalize this out given
                // TODO: combatflite is abandonware and is rarely used within the squadron these days.

                XmlNode routes = xmlDoc.DocumentElement.SelectSingleNode("/Mission/Routes");
                foreach (XmlNode route in routes.ChildNodes)
                {
                    string unitType = route.SelectSingleNode("Aircraft/Type").InnerText;
                    if ((unitTypes.Length > 0) && !unitTypes.Contains(unitType))
                        continue;

                    string foo = route.SelectSingleNode("Side").InnerText;
                    CoalitionType coalition = route.SelectSingleNode("Side").InnerText.ToLower() switch
                    {
                        "blue" => CoalitionType.BLUE,
                        "red" => CoalitionType.RED,
                        _ => CoalitionType.UNKNOWN
                    };
                    string flightName = route.SelectSingleNode("CallsignName").InnerText + " " +
                                        route.SelectSingleNode("CallsignNumber").InnerText;
                    if (flights.Contains(flightName))
                        throw new InvalidOperationException($"Duplicate flight name “{flightName}” in file.");
                    flights.Add(flightName);

                    List<UnitPositionItem> navRoute = ParseRoutePath(route.SelectSingleNode("Waypoints"));
                    groups.Add(new()
                    {
                        UniqueID = $"cf_g:{flightName}",
                        Coalition = coalition,
                        Category = UnitCategoryType.AIRCRAFT,
                        Name = flightName,
                        Units = ParseRouteFlight(route.SelectSingleNode("FlightMembers"), flightName, navRoute[0]),
                        Route = navRoute.Slice(1, navRoute.Count - 1)
                    });
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"CF:Extract fails with exception {ex}");
                return null;
            }

            return [.. groups.LimitGroupsWithUnits()
                             .LimitCoalitions(criteria.Coalitions)
                             .LimitUnitCategories(criteria.UnitCategories) ];
        }
    }
}
