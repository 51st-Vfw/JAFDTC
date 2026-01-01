using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using JAFDTC.Models.Planning;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.WebRequestMethods;

namespace JAFDTC.Kneeboard.Generate
{
    public static class DataHelper
    {
        public static IReadOnlyDictionary<string, string> ToDataDictionary(this GenerateCriteria criteria)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            //todo: do we want to "generate" a KB with looping and templatizing in future vs just string match replacement...
            //that will change the "gen X num of records per type vs raw looping..

            #region Mission

            result.Add("HEADER", criteria.Name);
            result.Add("FOOTER", $"{criteria.Name}, by {criteria.Owner.ToString()} @ {DateTime.Now.ToString("MM/dd/yyyy")}");
            result.Add("Theater", criteria.Mission.Theater);
            result.Add("Name", criteria.Mission.Name);
            result.Add("NightMode", criteria.NightMode.GetValueOrDefault(false).ToString());

            if (!string.IsNullOrWhiteSpace(criteria.PathLogo))
            {
                var b64Logo = Convert.ToBase64String(System.IO.File.ReadAllBytes(criteria.PathLogo));
                result.Add("LOGO", b64Logo);
            }

            #endregion

            #region Packages

            for (var i = 0; i < 8; i++)
            {
                var prefix = $"PACKAGE_{i + 1}_";
                var item = i < criteria.Mission.Packages.Count ? criteria.Mission.Packages[i] : null;
                if (item == null)
                {
                    result.Add($"{prefix}Name", "");
                }
                else
                {
                    result.Add($"{prefix}Name", item.Name);
                }
            }

            #endregion

            //we really just care about our flight for now... dont really care about its relation to packages or other flights as we just support "1" flight 0-8 ship
            #region Flights

            for (var i = 0; i < 16; i++)
            {
                var prefix = $"FLIGHT_{i + 1}_";
                var item = i < (criteria.Mission.Packages[0].Flights?.Count ?? -1) ? criteria.Mission.Packages[0].Flights[i] : null;
                if (item == null)
                {
                    result.Add($"{prefix}Name", "");
                }
                else
                {
                    result.Add($"{prefix}Name", item.Name);
                    result.Add($"{prefix}Aircraft", item.Aircraft);
                }

                //pilots
                //comms
                //nav points                
            }

            #endregion

            #region Threats
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

            for (var i = 0; i < 1000; i++)
            {
                var prefix = $"THREAT_{i + 1}_";
                var item = i < (criteria.Mission.Threats?.Count ?? -1) ? criteria.Mission.Threats[i] : null;
                if (item == null)
                {
                    result.Add($"{prefix}Name", "");
                    result.Add($"{prefix}Type", "");
                    result.Add($"{prefix}COORD", "");
                    result.Add($"{prefix}MGRS", "");
                    result.Add($"{prefix}DMPI", "");
                    result.Add($"{prefix}Altitude", "");
                    result.Add($"{prefix}WEZ", "");
                }
                else
                {
                    result.Add($"{prefix}Name", item.Name);
                    result.Add($"{prefix}Type", item.Type);
                    result.Add($"{prefix}COORD", "normal".ToDisplay(item.Latitude, item.Longitude));
                    result.Add($"{prefix}MGRS", 10.ToMGRS(item.Latitude, item.Longitude));
                    result.Add($"{prefix}DMPI", "DMPItodo");
                    result.Add($"{prefix}Altitude", item.Altitude.ToString("#"));
                    result.Add($"{prefix}WEZ", item.WEZ.HasValue ? item.WEZ.Value.ToString("#") : "");
                }
            }
            #endregion

            #region Airfields
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
            #endregion

            #region Maps
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
            #endregion

            return result;
        }

        private static string Clean(string? input, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            return input;
        }

    }
}
