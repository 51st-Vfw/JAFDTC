// ********************************************************************************************************************
//
// FormatExtension.cs -- core format extension
//
// Copyright(C) 2025-2026 rage
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

using JAFDTC.Core.Expressions;
using System.Text.RegularExpressions;

namespace JAFDTC.Core.Extensions
{
    public static class FormatExtension
    {
        public static string ToDisplay(this string mode, double latitude, double longitude) //uhh copilot ide made it.......
        {
            string latHemisphere = latitude >= 0 ? "N" : "S";
            string lonHemisphere = longitude >= 0 ? "E" : "W";
            latitude = Math.Abs(latitude);
            longitude = Math.Abs(longitude);
            int latDegrees = (int)latitude;
            int lonDegrees = (int)longitude;
            double latMinutesFull = (latitude - latDegrees) * 60;
            double lonMinutesFull = (longitude - lonDegrees) * 60;
            int latMinutes = (int)latMinutesFull;
            int lonMinutes = (int)lonMinutesFull;
            double latSeconds = (latMinutesFull - latMinutes) * 60;
            double lonSeconds = (lonMinutesFull - lonMinutes) * 60;

            //todo: diff modes... 

            return $"{latHemisphere} {latDegrees}°{latMinutes:00}'{latSeconds:00.00}\" " +
                   $"{lonHemisphere} {lonDegrees}°{lonMinutes:00}'{lonSeconds:00.00}\"";
        }

        public static string ToMGRS(this int precision, double latitude, double longitude)
        {
            // Placeholder for MGRS conversion logic
            // Implementing a full MGRS conversion is complex and typically requires a dedicated library.
            // Here, we will return a dummy string for demonstration purposes.
            return "33TWN1234567890"; // Dummy MGRS coordinate
        }

        public static string ToShortCallsign(this string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                MatchCollection m = CommonExpressions.CallsignRegex().Matches(value.ToUpper());
                if ((m.Count == 1) && string.IsNullOrEmpty(m[0].Groups[3].Value.ToString()))
                    //
                    // "VENOM1", "VENOM 1", "VENOM-1", etc. --> "VM1"
                    //
                    return $"{m[0].Groups[1].ToString().First()}{m[0].Groups[1].ToString().Last()}{m[0].Groups[2]}";
                else if (m.Count == 1)
                    //
                    // "VENOM1-1", "VENOM 1 1", "VENOM 1-1", etc. --> "VM1-1"
                    //
                    return $"{m[0].Groups[1].ToString().First()}{m[0].Groups[1].ToString().Last()}{m[0].Groups[2]}-{m[0].Groups[3]}";
            }
            return string.Empty;
        }

    }
}
