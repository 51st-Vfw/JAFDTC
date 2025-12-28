using System;
using System.Collections.Generic;
using System.Text;

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


    }
}
