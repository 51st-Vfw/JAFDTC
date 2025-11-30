using JAFDTC.TacView.Extensions;
using JAFDTC.TacView.Models;

namespace JAFDTC.TacView.Parsers
{
    public static class PositionParser
    {
        public static PositionItem Parse(string value)
        {
            //T=4.4070743|5.3344556|1839.87|-1.8|1.1|xxx|111826.99|-383591.38|318.9
            /*
             * https://www.tacview.net/documentation/acmi/en/
             * T = Longitude | Latitude | Altitude
             * T = Longitude | Latitude | Altitude | U | V
             * T = Longitude | Latitude | Altitude | Roll | Pitch | Yaw
             * T = Longitude | Latitude | Altitude | Roll | Pitch | Yaw | U | V | Heading
            */
            var parts = value.Split('|', StringSplitOptions.None);
            if (string.IsNullOrWhiteSpace(parts[0]))
                return default; //didnt move since the last time... throw it away...

            if (parts.Length < 3) //cases where there is only 1 value?  T=4.7 ???
                return default;

            var pos = new PositionItem
            {
                Longitude = -118 + parts[0].ToCleaDouble(), //lon offset from -118 deg
                Latitude = 31 + parts[1].ToCleaDouble(), //lat offset from 31 deg
                Altitude = 3.281 * parts[2].ToCleaDouble() //meters to feet

                //others are optional...
                //Heading = parts.Length > 5 ? (int)parts[5].ToCleaDouble() : 0,
                //X = parts.Length > 6 ? parts[6].ToCleaDouble() : 0,
                //Y = parts.Length > 7 ? parts[7].ToCleaDouble() : 0,
                //Knots = parts.Length > 8 ? 1.94384 * parts[8].ToCleaDouble() : 0 //m/s to knots
            };

            return pos;
        }
    }
}