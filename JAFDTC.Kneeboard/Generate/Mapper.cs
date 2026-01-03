using JAFDTC.Kneeboard.Models;
using JAFDTC.Core.Extensions;
using JAFDTC.Models.Planning;

namespace JAFDTC.Kneeboard.Generate
{
    internal class Mapper : IDisposable
    {
        private readonly Dictionary<string, string> _data = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> Map(GenerateCriteria criteria)
        {
            _data.Clear(); //just in case...

            BuildMisc(criteria);
            BuildPackages(criteria.Mission);
            BuildOwnship(criteria.Mission);
            BuildThreats(criteria.Mission);
            BuildAirfields(criteria);
            BuildMaps(criteria);

            return _data;
        }

        private void BuildMisc(GenerateCriteria criteria)
        {
            var name = Clean(criteria.Mission?.Name, criteria.Name);

            _data.Add(Keys.HEADER, name);
            _data.Add(Keys.FOOTER, $"{name}, by {criteria.Mission?.Owner?.Name} @ {DateTime.Now.ToString("MM/dd/yyyy")}");
            _data.Add(Keys.THEATER, criteria.Mission?.Theater);
            _data.Add(Keys.NAME, name);
            _data.Add(Keys.NIGHTMODE, criteria.NightMode.GetValueOrDefault(false).ToString());

            //if (!string.IsNullOrWhiteSpace(criteria.PathLogo))
            //{
            //    var b64Logo = Convert.ToBase64String(System.IO.File.ReadAllBytes(criteria.PathLogo));
            //    _data.Add(Keys.LOGO, b64Logo);
            //}
        }

        private void BuildPackages(Mission mission)
        {
            if (mission.Packages.IsEmpty())
                return;

            for (var i = 0; i < mission.Packages.Count; i++)
            {
                var package = mission.Packages[i];
                _data.Add(ToKey(Keys.PACKAGE_NAME, i), package.Name);

                BuildFlights(package);
            }
        }        

        private void BuildFlights(Package package)
        {
            if (package.Flights.IsEmpty())
                return;

            for (var i = 0; i < package.Flights.Count; i++)
            {
                var flight = package.Flights[i];
                _data.Add(ToKey(Keys.FLIGHT_NAME, i), flight.Name);
                _data.Add(ToKey(Keys.FLIGHT_NAME_SHORT, i), flight.Name.ToShortCallsign());
                _data.Add(ToKey(Keys.FLIGHT_AIRCRAFT, i), flight.Aircraft);

                if (flight.Pilots.HasData())
                {
                    for (var p = 0; p < flight.Pilots.Count; p++)
                    {
                        var pilot = flight.Pilots[p];

                        _data.Add(ToKey(Keys.PILOT_NAME, p), pilot.Name);
                        _data.Add(ToKey(Keys.PILOT_DATAID, p), Clean(pilot.DataId, ""));
                        _data.Add(ToKey(Keys.PILOT_SCL, p), Clean(pilot.SCL, ""));

                        var callsign = $"{flight.Name}-{p + 1}"; //from flight + position
                        _data.Add(ToKey(Keys.PILOT_CALLSIGN, p), callsign);
                        _data.Add(ToKey(Keys.PILOT_CALLSIGN_SHORT, p), callsign.ToShortCallsign());

                        //todo: more if we want...
                    }
                }

                BuildComms(flight);
                BuildRoutes(flight);
            }
        }

        private void BuildComms(Flight flight)
        {
            if (flight.Radios.IsEmpty())
                return;

            foreach (var radio in flight.Radios)
            {
                _data.Add(ToKey(Keys.RADIO_NUM, radio.RadioId, false), radio.RadioId.ToString());
                _data.Add(ToKey(Keys.RADIO_NAME, radio.RadioId, false), radio.Name);

                var radioPrefix = ToKey(Keys.RADIO_PREFIX, radio.RadioId, false);

                if (radio.Presets.IsEmpty())
                    continue;

                for (var c = 0; c < radio.Presets.Count; c++)
                {
                    var channel = radio.Presets[c];

                    _data.Add(ToKey(radioPrefix, Keys.RADIO_PRESET_NUM, c), (c + 1).ToString());
                    _data.Add(ToKey(radioPrefix, Keys.RADIO_PRESET_FREQ, c), channel.Frequency.ToString("{d:#.##}"));
                    _data.Add(ToKey(radioPrefix, Keys.RADIO_PRESET_DESC, c), Clean(channel.Description, ""));
                    _data.Add(ToKey(radioPrefix, Keys.RADIO_PRESET_MOD, c), Clean(channel.Modulation, ""));
                }
            }
        }

        private void BuildRoutes(Flight flight)
        {
            if (flight.Routes.IsEmpty())
                return;

            for (var r = 0; r < flight.Routes.Count; r++)
            {
                var routePrefix = ToKey(Keys.ROUTE_PREFIX, r);
                var route = flight.Routes[r];

                _data.Add(ToKey(Keys.ROUTE_NUM, r), (r + 1).ToString());
                _data.Add(ToKey(Keys.ROUTE_NAME, r), route.Name);

                for (var np = 0; np < route.NavPoints.Count; np++)
                {
                    var nav = route.NavPoints[np];

                    _data.Add(ToKey(routePrefix, Keys.NAV_NUM, np), (np + 1).ToString()); //or allow start 0??
                    _data.Add(ToKey(routePrefix, Keys.NAV_ALT, np), nav.Location.Altitude);
                    _data.Add(ToKey(routePrefix, Keys.NAV_TOS, np), Clean(nav.TOS, ""));
                    _data.Add(ToKey(routePrefix, Keys.NAV_TOT, np), Clean(nav.TOT, ""));
                    _data.Add(ToKey(routePrefix, Keys.NAV_SPEED, np), Clean(nav.Speed?.ToString(), ""));

                    _data.Add(ToKey(routePrefix, Keys.NAV_COORD, np), $"{nav.Location.Latitude} / {nav.Location.Longitude}");
                    //_data.Add(ToKey(routePrefix, Keys.NAV_COORD, np), "normal".ToDisplay(nav.Location.Latitude, nav.Location.Longitude));
                    //_data.Add(ToKey(routePrefix, Keys.NAV_MGRS, np), 10.ToMGRS(nav.Latitude, nav.Longitude));

                    var desc = nav.Name;
                    if (string.IsNullOrWhiteSpace(desc)
                        || string.Equals(desc, $"STP{np + 1}", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(desc, $"SP{np + 1}", StringComparison.OrdinalIgnoreCase))
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

                    _data.Add(ToKey(routePrefix, Keys.NAV_NAME, np), Clean(nav.Name, $"STP{np + 1}"));

                    _data.Add(ToKey(routePrefix, Keys.NAV_NOTE, np), "todo nav note"); //things about the STP.. airfield, threats, WEZ, etc...
                }
            }
        }

        private void BuildOwnship(Mission mission)
        {
            var owner = mission.Owner;

            _data.Add(Keys.OWNSHIP_BOARD, Clean(owner.Board, ""));
            _data.Add(Keys.OWNSHIP_JOKER, owner.Joker.HasValue ? owner.Joker.ToString() : "");
            _data.Add(Keys.OWNSHIP_LASE, owner.Lase.HasValue ? owner.Lase.ToString() : "");
            _data.Add(Keys.OWNSHIP_NAME, Clean(owner.Name, ""));
            _data.Add(Keys.OWNSHIP_STN, Clean(owner.STN, ""));
            _data.Add(Keys.OWNSHIP_TACAN, owner.Tacan.HasValue ? owner.Tacan.ToString() : "");
            _data.Add(Keys.OWNSHIP_TACANBAND, Clean(owner.TacanBand?.ToString(), ""));

            if (owner.CommPresets.HasData())
                foreach (var key in owner.CommPresets.Keys.Order())
                    _data.Add($"OWNSHIP_COMM{key}", owner.CommPresets[key].ToString());
        }

        private void BuildThreats(Mission mission)
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
                _data.Add(ToKey(Keys.THREAT_NAME, i), threat.Name);
                _data.Add(ToKey(Keys.THREAT_TYPE, i), threat.Type);
                _data.Add(ToKey(Keys.THREAT_COORD, i), $"{threat.Location.Latitude} / {threat.Location.Longitude}");

                //todo: depends on how we want to use this.. ie Ref'd by STP, general notes distinct list/count by types, etc...

                //result.Add($"{prefix}COORD", "normal".ToDisplay(item.Latitude, item.Longitude));
                //result.Add($"{prefix}MGRS", 10.ToMGRS(item.Latitude, item.Longitude));
                //result.Add($"{prefix}DMPI", "DMPItodo");
                //result.Add($"{prefix}Altitude", item.Altitude.ToString("#"));
                //result.Add($"{prefix}WEZ", item.WEZ.HasValue ? item.WEZ.Value.ToString("#") : "");
            }
        }

        private void BuildAirfields(GenerateCriteria criteria)
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

        private void BuildMaps(GenerateCriteria criteria)
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

        private static string ToKey(string key, int position)
        {
            return ToKey(key, position, true);
        }

        private static string ToKey(string prefix, string key, int position)
        {
            return prefix + key.Replace("*", (position + 1).ToString()).ToUpper();
        }

        private static string ToKey(string key, int position, bool isIndex)
        {
            return key.Replace("*", (position + (isIndex ? 1: 0)).ToString()).ToUpper();
        }

        private static string Clean(string? input, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            return input;
        }

        public void Dispose()
        {
            _data?.Clear();
        }

    }
}
