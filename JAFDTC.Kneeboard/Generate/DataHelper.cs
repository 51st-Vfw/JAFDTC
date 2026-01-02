using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using JAFDTC.Models.Planning;

namespace JAFDTC.Kneeboard.Generate
{
    public static class DataHelper
    {
        public static class Keys
        {
            public const string HEADER = "HEADER";
            public const string FOOTER = "FOOTER";
            public const string THEATER = "THEATER";
            public const string NAME = "NAME";
            public const string NIGHTMODE = "NIGHTMODE";
            public const string LOGO = "LOGO";
            
            public const string PACKAGE_NAME = "PACKAGE_*_NAME";

            //in future should be package prefix...based
            public const string FLIGHT_NAME = "FLIGHT_*_NAME"; 
            public const string FLIGHT_AIRCRAFT = "FLIGHT_*_AIRCRAFT";

            //in future should be package/flight prefix...based
            public const string PILOT_NAME = "PILOT_*_NAME";
            public const string PILOT_CALLSIGN = "PILOT_*_CALLSIGN";
            public const string PILOT_STN = "PILOT_*_STN";
            //public const string FLIGHT_PILOT_BOARD = "FLIGHT_*_AIRCRAFT";
            //public const string FLIGHT_PILOT_TACAN = "FLIGHT_*_AIRCRAFT";
            //public const string FLIGHT_PILOT_TACANBAND = "FLIGHT_*_AIRCRAFT";
            //public const string FLIGHT_PILOT_JOKER = "FLIGHT_*_AIRCRAFT";
            //public const string FLIGHT_PILOT_LASE = "FLIGHT_*_AIRCRAFT";

            //since we only are supporting 1 flight right now.. let all pilots, nav points, and comms tied to that first flight...
            public const string COMM_PREFIX = "COMM*_";
            public const string COMM_NUM = "PRESET*_NUM"; //PREFIX + this
            public const string COMM_FREQ = "PRESET*_FREQ"; //PREFIX + this
            public const string COMM_DESC = "PRESET*_DESC"; //PREFIX + this

            public const string NAV_NUM = "NAV_*_NUM";
            public const string NAV_NAME = "NAV_*_NAME";
            public const string NAV_NOTE = "NAV_*_NOTE";
            public const string NAV_ALT = "NAV_*_ALT";
            public const string NAV_TOS = "NAV_*_TOS";
            public const string NAV_TOT = "NAV_*_TOT";
            public const string NAV_SPEED = "NAV_*_SPEED";
            public const string NAV_COORD = "NAV_*_COORD";
            public const string NAV_MGRS = "NAV_*_MGRS";


            public const string THREAT_NAME = "THREAT_*_NAME";
            public const string THREAT_TYPE = "THREAT_*_TYPE";

        }

        public static IReadOnlyDictionary<string, string> ToDataDictionary(this GenerateCriteria criteria)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            BuildMisc(result, criteria);
            BuildPackages(result, criteria.Mission);
            BuildThreats(result, criteria.Mission);
            BuildAirfields(result, criteria);
            BuildMaps(result, criteria);

            return result;
        }

        private static void BuildMisc(Dictionary<string, string> data, GenerateCriteria criteria)
        {
            data.Add(Keys.HEADER, criteria.Name);
            data.Add(Keys.FOOTER, $"{criteria.Name}, by {criteria.Owner.ToString()} @ {DateTime.Now.ToString("MM/dd/yyyy")}");
            data.Add(Keys.THEATER, criteria.Mission.Theater);
            data.Add(Keys.NAME, criteria.Mission.Name);
            data.Add(Keys.NIGHTMODE, criteria.NightMode.GetValueOrDefault(false).ToString());

            if (!string.IsNullOrWhiteSpace(criteria.PathLogo))
            {
                var b64Logo = Convert.ToBase64String(System.IO.File.ReadAllBytes(criteria.PathLogo));
                data.Add(Keys.LOGO, b64Logo);
            }
        }

        private static void BuildPackages(Dictionary<string, string> data, Mission mission)
        {
            if (mission.Packages.IsEmpty())
                return;

            for (var i = 0; i < mission.Packages.Count; i++)
            {
                var package = mission.Packages[i];
                data.Add(ToKey(Keys.PACKAGE_NAME, i), package.Name);

                BuildFlights(data, package);
            }
        }

        private static void BuildFlights(Dictionary<string, string> data, Package package)
        {
            if (package.Flights.IsEmpty())
                return;

            for (var i = 0; i < package.Flights.Count; i++)
            {
                var flight = package.Flights[i];
                data.Add(ToKey(Keys.FLIGHT_NAME, i), flight.Name);
                data.Add(ToKey(Keys.FLIGHT_AIRCRAFT, i), flight.Aircraft);

                if (flight.Pilots.HasData())
                {
                    for (var f = 0; f < package.Flights.Count; f++)
                    {
                        var pilot = flight.Pilots[f];

                        data.Add(ToKey(Keys.PILOT_NAME, f), pilot.Name);
                        data.Add(ToKey(Keys.PILOT_STN, f), Clean(pilot.STN, ""));
                        data.Add(ToKey(Keys.PILOT_CALLSIGN, f), $"{flight.Name}_{f + 1}");

                        //todo: more if we want...
                    }
                }

                if (flight.Comms.HasData())
                {
                    var radios = flight.Comms.Select(p => p.CommId).Distinct().Order().ToList();
                    foreach (var radioId in radios)
                    {
                        var channels = flight.Comms.Where(p => p.CommId == radioId).ToList(); //dont reorder!
                        for(var c = 0; c < channels.Count; c++)
                        {
                            var channel = channels[c];
                            var commPrefix = Keys.COMM_PREFIX.Replace("*", channel.CommId.ToString());

                            data.Add(ToKey(commPrefix, Keys.COMM_NUM, c), (c + 1).ToString());
                            data.Add(ToKey(commPrefix, Keys.COMM_FREQ, c),channel.Frequency.ToString("{d:#.##}"));
                            data.Add(ToKey(commPrefix, Keys.COMM_DESC, c), Clean(channel.Description, "")); //always replace text with Unassigned or blank...?
                        }
                    }
                }

                if (flight.Navs.HasData())
                {
                    for(var n = 0; n < flight.Navs.Count; n++)
                    {
                        var nav = flight.Navs[n];

                        data.Add(ToKey(Keys.NAV_NUM, n), (n + 1).ToString()); //or allow start 0??
                        data.Add(ToKey(Keys.NAV_ALT, n), nav.Altitude.ToString("#"));
                        data.Add(ToKey(Keys.NAV_TOS, n), Clean(nav.TOS, ""));
                        data.Add(ToKey(Keys.NAV_TOT, n), Clean(nav.TOT, ""));
                        data.Add(ToKey(Keys.NAV_SPEED, n), Clean(nav.Speed?.ToString(), ""));
                        data.Add(ToKey(Keys.NAV_COORD, n), "normal".ToDisplay(nav.Latitude, nav.Longitude));
                        data.Add(ToKey(Keys.NAV_MGRS, n), 10.ToMGRS(nav.Latitude, nav.Longitude));

                        var desc = nav.Name;
                        if (string.IsNullOrWhiteSpace(desc)
                            || string.Equals(desc, $"STP{n + 1}", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(desc, $"SP{n + 1}", StringComparison.OrdinalIgnoreCase))
                        {
                            //todo: attempt to match by location to the POI DB
                            //maybe even import from miz/cf/acmi  group/unit/static

                            /*
                             * from Map POI DB and/or Import Miz/CF/ACMI
                             *      if stp is at (or near??) then upgrade any missing data (like name, alt, etc)
                             *      maybe if STP in WEZ or know threats..list them out in Notes ?
                             * 
                             */

                            //name = "better name..."
                        }

                        data.Add(ToKey(Keys.NAV_NAME, n), Clean(nav.Name, $"STP{n + 1}"));

                        data.Add(ToKey(Keys.NAV_NOTE, n), "todo nav note"); //things about the STP.. airfield, threats, WEZ, etc...

                    }
                }
            }
        }

        private static void BuildThreats(Dictionary<string, string> data, Mission mission)
        {
            if (mission.Threats.IsEmpty())
                return;

            /*
            * from POI import of miz/cf/acmi
            * distinct list of air/ground threats
            * 
            * Aircraft
            *      name, amount, rwr symbol, MAR?
            *      Mig-29, 12x
            *      Su-27, 3x
            *      AN-50, 1x
            *      
            * SAM
            *      name, rwr symbol, WEZ, ceiling, IR/EO/RD
            *      EWR, 3x
            *      SA-2 10x
            *      SA-5 2x
            *      SA-10 3x
            *      MIM-111 4x
            * 
            * SHORAD
            *      name, rwr, WEZ, ceiling, IR/EO/RD
            *      SA-8 x23
            *      SA-11 2x
            * 
            * AAA
            *      name, rwr, WEZ, ceiling, EO/RD
            *      Gepard 2x
            *      ZSU-23 80x
            * 
            * 
            * Locations 
            *      just airfields, farps?  cities?
            *      Aleppo: SA-10, SA-11, SA-15, Mig-29
            *      Damascus: EWR, SA-8, Su-27
            * 
            */

            for (var i = 0; i < mission.Threats.Count; i++)
            {
                var threat = mission.Threats[i];
                data.Add(ToKey(Keys.THREAT_NAME, i), threat.Name);
                data.Add(ToKey(Keys.THREAT_TYPE, i), threat.Type);

                //todo: depends on how we want to use this.. ie Ref'd by STP, general notes distinct list/count by types, etc...

                //result.Add($"{prefix}COORD", "normal".ToDisplay(item.Latitude, item.Longitude));
                //result.Add($"{prefix}MGRS", 10.ToMGRS(item.Latitude, item.Longitude));
                //result.Add($"{prefix}DMPI", "DMPItodo");
                //result.Add($"{prefix}Altitude", item.Altitude.ToString("#"));
                //result.Add($"{prefix}WEZ", item.WEZ.HasValue ? item.WEZ.Value.ToString("#") : "");
            }
        }

        private static void BuildAirfields(Dictionary<string, string> data, GenerateCriteria criteria)
        {
            /*
             * for any STPs that are linked or "at/near" airfield (farp tbd)
             *      also if first/last stp...?
             * 
             * from POI DB
             *      all info we have
             *      ?? tower comms, alt, runway(s), icls, etc?
             *      Enemy Threats: Aircraft, AAA, SAM, SHORAD, other?
             * 
             */
        }

        private static void BuildMaps(Dictionary<string, string> data, GenerateCriteria criteria)
        {
            /*
             * long term...
             * gen an image from "map widget"
             * 
             * theater map of total STP + buffer distance
             * 
             * stp map
             *      numbers/doghouses info
             * 
             * ip/target area zoomed in map + buffer distance
             * 
             * include POIs and Threats if imported from miz/cf/acmi
             *      airfield names, farp names
             *      wez rings
             *      enemy aircraft fields (ie mig29 aleppo)
             * 
             */
        }

        private static string ToKey(string key, int index)
        {
            return key.Replace("*", (index + 1).ToString()).ToUpper();
        }

        private static string ToKey(string prefix, string key, int index)
        {
            return prefix + key.Replace("*", (index + 1).ToString()).ToUpper();
        }

        private static string Clean(string? input, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            return input;
        }

    }
}
