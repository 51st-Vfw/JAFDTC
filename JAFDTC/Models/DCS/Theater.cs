// ********************************************************************************************************************
//
// Theater.cs -- theater model
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using System.Collections.Generic;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// class to encapsulate a number of useful functions around theaters.
    /// </summary>
    public sealed class Theater
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        public static Dictionary<string, TheaterInfo> TheaterInfo => new()
        {
            //                         lat-    lat+     lon-     lon+    z
            ["Afghanistan"]     = new( 23.00,  38.75,   60.25,   73.25,  5),    // UTC +5
            ["Caucasus"]        = new( 40.00,  46.00,   33.00,   46.00,  4),    // UTC +4
            ["Germany"]         = new( 49.50,  54.50,    6.50,   16.00,  2),    // UTC +2
            ["Iraq"]            = new( 26.25,  37.00,   38.50,   52.00,  4),    // UTC +4
            ["Kola"]            = new( 64.00,  71.25,   12.00,   39.00,  3),    // UTC +3
            ["Marianas"]        = new( 11.75,  22.00,  141.00,  149.00, 10),    // UTC +10
            ["Nevada"]          = new( 34.50,  38.75, -118.50, -113.00, -8),    // UTC -8
            ["Persian Gulf"]    = new( 22.00,  30.75,   50.00,   59.00,  4),    // UTC +4
            ["Sinai"]           = new( 26.50,  34.00,   29.50,   35.75,  3),    // UTC +3
            ["South Atlantic"]  = new(-57.00, -48.00,  -77.00,  -55.00, -3),    // UTC -3
            ["Syria"]           = new( 31.25,  37.75,   30.75,   41.00,  3)     // UTC +3
        };
        public static List<string> Theaters => [.. TheaterInfo.Keys ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a list of the names of the dcs theaters that contains the given coordinate, empty list if
        /// no theater matches the coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// note that a coordinate may appear in multiple theaters.
        /// </summary>
        public static List<string> TheatersForCoords(double lat, double lon)
        {
            List<string> theaters = [];
            foreach (KeyValuePair<string, TheaterInfo> kvp in TheaterInfo)
                if (kvp.Value.Contains(lat, lon))
                    theaters.Add(kvp.Key);
            return theaters;
        }

        /// <summary>
        /// returns a list of the names of the dcs theaters that contains the given coordinate, empty list if
        /// no theater matches the coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// note that a coordinate may appear in multiple theaters. null is returned if unable to parse.
        /// </summary>
        public static List<string> TheatersForCoords(string lat, string lon)
        {
            if (double.TryParse(lat, out double latNum) && double.TryParse(lon, out double lonNum))
                return TheatersForCoords(latNum, lonNum);
            return null;
        }
    }
}
