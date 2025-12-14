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

using JAFDTC.Models.CoreApp;
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
                        navpt.TimeOn = (int.Parse(match.Groups[1].Value) * 3600) +
                                       (int.Parse(match.Groups[2].Value) * 60) +
                                       (int.Parse(match.Groups[3].Value) * 1);
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
                    UniqueID = $"cf_extract:{flightName}-{index++}",
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
                        UniqueID = $"cf_extract:{flightName}",
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

// TODO: deprecate

#if NOPE
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties

        private AirframeTypes Airframe {  get; set; }

        private string Path { get; set; }

        private XmlDocument XmlDoc { get; set; }
        
        private Dictionary<string, XmlNode> XmlNavpointNodes { get; set; }

        private bool IsImportTakeOff { get; set; }

        private bool IsImportTOS { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperCF(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
            XmlDoc = new XmlDocument();
            XmlNavpointNodes = [ ];

            IsImportTakeOff = false;
            IsImportTOS = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        private bool IsMatchingAirframe(string airframe)
        {
            return Settings.IsNavPtImportIgnoreAirframe || Airframe switch
            {
                AirframeTypes.A10C => (airframe == "A-10C_2"),
                AirframeTypes.AH64D => (airframe == "AH-64D_BLK_II"),
                AirframeTypes.AV8B => (airframe == "AV8BNA"),
                AirframeTypes.F14AB => (airframe == "F-14A-135-GR") || (airframe == "F-14B"),
                AirframeTypes.F15E => (airframe == "F-15ESE"),
                AirframeTypes.F16C => (airframe == "F-16C_50"),
                AirframeTypes.FA18C => (airframe == "FA-18C_hornet"),
                AirframeTypes.M2000C => (airframe == "M-2000C"),
                _ => false,
            };
        }

        /// <summary>
        /// return a list of navpoints from the import data source for the flight with the given name. for sources
        /// where HasFlights is false, the flight name is ignored. for sources where HasFlights is true, the flight
        /// name must match one of the flights from Flights(). navpoints are represented by a string/string
        /// dictionary with the following key/value pairs:
        /// 
        ///   ["name"]      (string) name of navpoint
        ///   ["lat"]       (string) latitude of navpoint, decimal degrees with no units
        ///   ["lon"]       (string) longitude of navpoint, decimal degrees with no units
        ///   ["alt"]       (string) elevation of navpoint, feet
        ///   ["ton"]       (string) time on navpoint, hh:mm:ss local
        /// </summary>
        private List<Dictionary<string, string>> Navpoints(string flightName)
        {
            List<Dictionary<string, string>> navpoints = null;
            if (XmlNavpointNodes.TryGetValue(flightName, out XmlNode value))
            {
                navpoints = [ ];
                foreach (XmlNode node in value)
                {
                    string type = node.SelectSingleNode("Type").InnerText;
                    bool isTakeOffType = ((type != null) && Regex.Match(type.ToLower(), @"^take off").Success);

                    if (IsImportTakeOff || !isTakeOffType)
                    {
                        Dictionary<string, string> navpoint = new()
                        {
                            ["name"] = node.SelectSingleNode("Name").InnerText,
                            ["lat"] = node.SelectSingleNode("Lat").InnerText,
                            ["lon"] = node.SelectSingleNode("Lon").InnerText,
                        };

                        if (!double.TryParse(node.SelectSingleNode("Altitude").InnerText, out double alt))
                        {
                            alt = 0.0;
                        }
                        navpoint["alt"] = $"{(int)alt:D}";

                        string ton = node.SelectSingleNode("TOT").InnerText;
                        if ((ton != null) && IsImportTOS)
                        {
                            string[] parts = ton.Split(' ');
                            if (parts.Length == 3)
                            {
                                string[] hms = parts[1].Split(':');
                                if ((hms.Length == 3) &&
                                    (int.TryParse(hms[0], out int h) && (h >= 1) && (h < 13)) &&
                                    (int.TryParse(hms[1], out int m) && (m >= 0) && (m < 60)) &&
                                    (int.TryParse(hms[2], out int s) && (s >= 0) && (s < 60)))
                                {
                                    if ((parts[2].Equals("pm", StringComparison.CurrentCultureIgnoreCase)) && (h < 12))
                                    {
                                        h += 12;
                                    }
                                    navpoint["ton"] = $"{h:D2}:{m:D2}:{s:D2}";
                                }
                            }
                        }

                        // TODO: put "ERROR" in dictionary if there were errors in the stpt?
                        navpoints.Add(navpoint);
                    }
                }
            }
            return navpoints;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IImportHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public override bool HasFlights => true;

        public override List<string> Flights()
        {
            XmlNavpointNodes.Clear();

            List<string> flights = [ ];
            try
            {
                string xml = FileManager.ReadFileFromZip(Path, "mission.xml");
                XmlDoc.LoadXml(xml);

                XmlNode routes = XmlDoc.DocumentElement.SelectSingleNode("/Mission/Routes");
                foreach (XmlNode route in routes.ChildNodes)
                {
                    XmlNode aircraft = route.SelectSingleNode("FlightMembers/FlightMember/Aircraft");
                    if (IsMatchingAirframe(aircraft.SelectSingleNode("Type").InnerText))
                    {
                        string callsignName = route.SelectSingleNode("CallsignName").InnerText;
                        string callsignNumber = route.SelectSingleNode("CallsignNumber").InnerText;
                        string flightName = callsignName + " " + callsignNumber;

                        if (XmlNavpointNodes.ContainsKey(flightName))
                        {
                            throw new InvalidOperationException("Duplicate flight name in file.");
                        }
                        flights.Add(flightName);
                        XmlNavpointNodes[flightName] = route.SelectSingleNode("Waypoints");
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ImportHelperCF:Flights exception {ex}");
                return null;
            }
            return flights;
        }

        public override Dictionary<string, string> NavptOptionTitles(string what = "Steerpoint")
            => new()
            {
                ["A"] = $"Import Take Off {what}s",
                ["B"] = $"Import Time on {what}",
            };

        public override Dictionary<string, object> NavptOptionDefaults
            => new()
            {
                ["A"] = false,
                ["B"] = false,
            };

        public override bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true,
                                    Dictionary<string, object> options = null)
        {
            IsImportTakeOff = (bool)options["A"];
            IsImportTOS = (bool)options["B"];

            List<Dictionary<string, string>> navptInfoList = Navpoints(flightName);
            if (navptInfoList != null)
            {
                return navptSys.ImportNavpointInfoList(navptInfoList, isReplace);
            }
            return false;
        }
#endif
    }
}
