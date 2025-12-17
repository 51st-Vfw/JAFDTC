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
using JAFDTC.File.Extensions;
using JAFDTC.File.Models;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;
using System.Xml;

namespace JAFDTC.File.CF
{
    public class Extractor : IExtractor
    {
        public IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria extractCriteria)
        {
            // TODO: this parser is focused on extracting navpoints and does not currently handle ground units
            // TODO: or other non-aircraft types. not clear it's worth the squeeze to generalize this out given
            // TODO: combatflite is abandonware and is rarely used within the squadron these days.

            extractCriteria.Required();
            extractCriteria.FilePath.Required();

            if (!System.IO.File.Exists(extractCriteria.FilePath))
                throw new FileNotFoundException("CF file not found", extractCriteria.FilePath);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(Core.IO.FileHelper.ReadAllText(extractCriteria.FilePath, "mission.xml"));

            var routes = xmlDoc.DocumentElement.SelectSingleNode("/Mission/Routes");

            var unitTypes = extractCriteria.UnitTypes ?? [];
            var flightGuard = new HashSet<string>();

            List<UnitGroupItem> groups = [];

            foreach (XmlNode route in routes.ChildNodes)
            {
                var unitType = route.SelectSingleNode("Aircraft/Type").InnerText;
                if (unitTypes.HasData() && !unitTypes.Contains(unitType))
                    continue;

                var coalition = route.SelectSingleNode("Side").InnerText.ToLower() switch
                {
                    "blue" => CoalitionType.BLUE,
                    "red" => CoalitionType.RED,
                    _ => CoalitionType.UNKNOWN
                };

                var flightName = route.SelectSingleNode("CallsignName").InnerText + " " +
                                    route.SelectSingleNode("CallsignNumber").InnerText;

                if (flightGuard.Contains(flightName))
                    throw new InvalidDataException($"Duplicate flight name “{flightName}” in file.");

                flightGuard.Add(flightName);

                var navRoute = ParseRoutePath(route.SelectSingleNode("Waypoints"));
                groups.Add(new()
                {
                    UniqueID = $"cf_g:{flightName}",
                    Coalition = coalition,
                    Category = UnitCategoryType.AIRCRAFT,
                    Name = flightName,
                    Units = ParseRouteFlight(route.SelectSingleNode("FlightMembers"), flightName, navRoute[0]),
                    Route = navRoute[1..]
                });
            }

            var result = groups
                .LimitGroupsWithUnits()
                .LimitCoalitions(extractCriteria.Coalitions)
                .LimitUnitCategories(extractCriteria.UnitCategories)
                .ToList();

            return result;
        }

        /// <summary>
        /// walks the waypoints in a "waypoints" element from the xml creating a list of waypoints that make
        /// up the route.
        /// </summary>
        internal static List<UnitPositionItem> ParseRoutePath(XmlNode waypoints)
        {
            List<UnitPositionItem> navpts = [];

            foreach (XmlNode waypoint in waypoints.ChildNodes)
            {
                if (!double.TryParse(waypoint.SelectSingleNode("Altitude").InnerText, out double alt))
                    alt = 0.0;

                var timeOn = -1.0;
                var tot = waypoint.SelectSingleNode("TOT").InnerText;
                if (!string.IsNullOrWhiteSpace(tot))
                {
                    var match = Core.Expressions.CommonExpressions.TimeRegex().Match(tot);
                    if (match.Success)
                    {
                        var h = int.Parse(match.Groups[1].Value);
                        h = match.Groups[4].Value.ToLower() switch
                        {
                            "am" => (h < 12) ? h : 0,
                            "pm" => (h < 12) ? h + 12 : h,
                            _ => h
                        };

                        timeOn = (h * 3600) +
                                       (int.Parse(match.Groups[2].Value) * 60) +
                                       (int.Parse(match.Groups[3].Value) * 1);
                    }
                }

                var navpt = new UnitPositionItem
                {
                    Name = waypoint.SelectSingleNode("Name").InnerText,
                    Latitude = double.Parse(waypoint.SelectSingleNode("Lat").InnerText),
                    Longitude = double.Parse(waypoint.SelectSingleNode("Lon").InnerText),
                    TimeOn = timeOn,
                    Altitude = alt,
                };

                navpts.Add(navpt);
            }

            return navpts;
        }

        internal static List<UnitItem> ParseRouteFlight(XmlNode flightMembers, string flightName, UnitPositionItem position)
        {
            List<UnitItem> units = [];

            int index = 1;
            foreach (XmlNode unit in flightMembers.ChildNodes)
            {
                var aircraft = unit.SelectSingleNode("Aircraft");
                var unitItem = new UnitItem
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

        public void Dispose()
        {         
        }
    }
}
