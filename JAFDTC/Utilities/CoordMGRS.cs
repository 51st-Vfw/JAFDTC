// ********************************************************************************************************************
//
// CoordMGRS.cs : coordinate transformation and conversion functions for mgrs
//
// Copyright(C) 2025 ilominar/raven
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
//
// adapted from code from https://github.com/Special-K-s-Flightsim-Bots/DCSServerBot
//
// MIT License
//
// Copyright (c) 2021-2023 DCSServerBot
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without
// limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
// ********************************************************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JAFDTC.Utilities
{
    public class CoordLL(double lat, double lon)
    {
        public double Lat = lat;
        public double Lon = lon;
    }

    public class CoordUTM
    {
        public double Easting;
        public double Northing;
        public int ZoneNumber;
        public string ZoneLetter;
        public double Accuracy;
    }

    /// <summary>
    /// class to encapsulate static functions that provide translation between lat/lon (via CoordLL and raw
    /// latitude/longitude representations) and mgrs representations.
    /// 
    /// TODO: UTM code here may be sharable with the XZ <--> LL translation done in support of .miz parsing.
    /// </summary>
    public partial class CoordMGRS
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        [GeneratedRegex(@"(?i)^([0-9]+)[A-Z]")]
        public static partial Regex MGRSZoneRegex();

        const string MGRS_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string MGRS_COL_ORIGIN = "AJSAJS";
        const string MGRS_ROW_ORIGIN = "AFAFAF";
        const string MGRS_BAD_ZONES = "BYIO";

        // UTM zones are grouped, and assigned to one of a group of 6 sets.
        //
        private const int MGRS_100K_SETS = 6;

        private static readonly Dictionary<char, double> _minNorthingMap = new()
        {
            ['C'] = 1100000.0,
            ['D'] = 2000000.0,
            ['E'] = 2800000.0,
            ['F'] = 3700000.0,
            ['G'] = 4600000.0,
            ['H'] = 5500000.0,
            ['J'] = 6400000.0,
            ['K'] = 7300000.0,
            ['L'] = 8200000.0,
            ['M'] = 9100000.0,
            ['N'] = 0.0,
            ['P'] = 800000.0,
            ['Q'] = 1700000.0,
            ['R'] = 2600000.0,
            ['S'] = 3500000.0,
            ['T'] = 4400000.0,
            ['U'] = 5300000.0,
            ['V'] = 6200000.0,
            ['W'] = 7000000.0,
            ['X'] = 7900000.0
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // lat/lon <--> mgrs conversion
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the mgrs encoding of a lat/lon value, null on error. the accuracy specifies the number of digits
        /// to use in the mgrs encoding.
        /// </summary>
        public static string LLtoMGRS(string lat, string lon, int accuracy = 5)
            => LLtoMGRS(new CoordLL(double.Parse(lat), double.Parse(lon)), accuracy);

        public static string LLtoMGRS(double lat, double lon, int accuracy = 5)
            => LLtoMGRS(new CoordLL(lat, lon), accuracy);

        public static string LLtoMGRS(CoordLL ll, int accuracy = 5)
        {
            try
            {
                return MGRSEncode(LLtoUTM(ll), accuracy);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LLtoMGRS fails: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// returns the lat/lon value for an mgrs encoding, null on error.
        /// </summary>
        public static CoordLL MGRStoLL(string mgrs)
        {
            try
            {
                return UTMtoLL(MGRSDecode(mgrs.ToUpper()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MGRStoLL fails: {ex.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities & support
        //
        // ------------------------------------------------------------------------------------------------------------

        private static double DegToRad(double deg) => deg * (Math.PI / 180.0);

        private static double RadToDeg(double rad) => 180.0 * (rad / Math.PI);

        /// <summary>
        /// returns a UTM coordiante encoded as an MGRS string.
        /// </summary>
        private static string MGRSEncode(CoordUTM utm, int accuracy = 5)
        {
            string e = $"00000{(int)utm.Easting}";
            string n = $"00000{(int)utm.Northing}";
            return $"{utm.ZoneNumber}{utm.ZoneLetter}{Get100kID(utm)}{e[^5..][0..accuracy]}{n[^5..][0..accuracy]}";
        }

        /// <summary>
        /// returns the utm coordinate for the given mgrs string (assumed upper case). throws exception on error.
        /// </summary>
        private static CoordUTM MGRSDecode(string mgrsString)
        {
            if (string.IsNullOrEmpty(mgrsString))
                throw new Exception("MGRSDecode emtpy MGRS string");

            int length = mgrsString.Length;

            MatchCollection match = MGRSZoneRegex().Matches(mgrsString);
            if (match.Count == 0)
                throw new Exception($"MGRSDecode bogus MGRS string '{mgrsString}'");
            int index = match[0].Groups[1].Length;
            if ((index > 2) || ((index + 3) > length))
                throw new Exception($"MGRSDecode bogus MGRS string '{mgrsString}'");

            int zoneNumber = int.Parse(match[0].Groups[1].ToString());
            char zoneLetter = mgrsString[index++];

            if (MGRS_BAD_ZONES.Contains(zoneLetter))
                throw new Exception($"MGRSDecode bogus zone '{zoneLetter}' in MGRS string '{mgrsString}'");

            string hunK = mgrsString[index..(index + 2)];
            index += 2;

            int set = Get100kSetForZone(zoneNumber);
            double east100k = GetEastingFromChar(hunK[0], set);
            double north100k = GetNorthingFromChar(hunK[1], set);
            while (north100k < GetMinNorthing(zoneLetter))
                north100k += 2000000;

            // calculate the char index for easting/northing separator and make sure it's valid.
            //
            int remainder = length - index;
            if ((remainder % 2) != 0)
                throw new Exception($"MGRSDecode odd easting/northing count in MGRS string '{mgrsString}'");
            int accuracy = remainder / 2;

            double sepEasting = 0.0;
            double sepNorthing = 0.0;
            if (accuracy > 0)
            {
                double accuracyPoT = 100000.0 / Math.Pow(10.0, accuracy);
                sepEasting = double.Parse(mgrsString[index..(index + accuracy)]) * accuracyPoT;
                sepNorthing = double.Parse(mgrsString[(index + accuracy)..]) * accuracyPoT;
            }

            return new CoordUTM()
            {
                Northing = sepNorthing + north100k,
                Easting = sepEasting + east100k,
                ZoneNumber = zoneNumber,
                ZoneLetter = $"{zoneLetter}",
                Accuracy = accuracy
            };
        }

        /// <summary>
        /// returns the utm encoding for a lat/lon using the WGS84 ellipsoid for the conversion. throws exception
        /// on error.
        /// </summary>
        private static CoordUTM LLtoUTM(CoordLL ll)
        {
            double a = 6378137.0;                   // ellipse radius
            double ecc2 = 0.00669438;               // ellipse eccentricity, squared
            double k0 = 0.9996;

            double latRad = DegToRad(ll.Lat);
            double lonRad = DegToRad(ll.Lon);

            CoordUTM utm = new()
            {
                ZoneNumber = (int)Math.Floor((ll.Lon + 180.0) / 6.0) + 1,
                ZoneLetter = GetLetterDesignator(ll.Lat)
            };

            // make sure the longitude 180.00 is in zone 60 and update for special zones for norway, svalbard
            //
            if (ll.Lon == 180.0)
                utm.ZoneNumber = 60;
            else if (((56.0 <= ll.Lat) && (ll.Lat < 64.0)) && ((3.0 <= ll.Lon) && (ll.Lon < 12.0)))
                utm.ZoneNumber = 32;
            else if ((72.0 <= ll.Lat) && (ll.Lat < 84.0))
                if ((0.0 <= ll.Lon) && (ll.Lon < 9.0))
                    utm.ZoneNumber = 31;
                else if ((9.0 <= ll.Lon) && (ll.Lon < 21.0))
                    utm.ZoneNumber = 33;
                else if ((21.0 <= ll.Lon) && (ll.Lon < 33.0))
                    utm.ZoneNumber = 35;
                else if ((33.0 <= ll.Lon) && (ll.Lon < 42.0))
                    utm.ZoneNumber = 37;

            double lonOrigin = (utm.ZoneNumber - 1) * 6 - 180 + 3;  // +3 puts origin in middle of zone
            double lonOriginRad = DegToRad(lonOrigin);

            double eccPrime2 = ecc2 / (1.0 - ecc2);
            double ecc4 = ecc2 * ecc2;
            double ecc6 = ecc2 * ecc2 * ecc2;

            double N = a / Math.Sqrt(1.0 - ecc2 * Math.Sin(latRad) * Math.Sin(latRad));
            double T = Math.Tan(latRad) * Math.Tan(latRad);
            double C = eccPrime2 * Math.Cos(latRad) * Math.Cos(latRad);
            double A = Math.Cos(latRad) * (lonRad - lonOriginRad);

            double T2 = T * T;
            double C2 = C * C;
            double A2 = A * A;
            double A3 = A * A2;
            double A4 = A * A3;
            double A5 = A * A4;
            double A6 = A * A5;

            double M = a * ((1 - ecc2 / 4 - 3 * ecc4 / 64 - 5 * ecc6 / 256) * latRad -
                            (3 * ecc2 / 8 + 3 * ecc4 / 32 + 45 * ecc6 / 1024) * Math.Sin(2 * latRad) +
                            (15 * ecc4 / 256 + 45 * ecc6 / 1024) * Math.Sin(4 * latRad) -
                            (35 * ecc6 / 3072) * Math.Sin(6 * latRad));

            utm.Easting = (k0 * N * (A +
                                     (((1.0 - T + C) * A3) / 6.0) +
                                     (5.0 - (18.0 * T) + T2 + (72.0 * C) - (58.0 * eccPrime2)) * A5 / 120.0)) + 500000.0;
            utm.Northing = (k0 * (M +
                                  N * Math.Tan(latRad) * ((A2 / 2.0) +
                                                          ((5 - T + (9.0 * C) + (4.0 * C2)) * A4) / 24.0 +
                                                          ((61.0 - (58.0 * T) + T2 + (600.0 * C) -
                                                          (330.0 * eccPrime2)) * A6) / 720.0)));

            if (ll.Lat < 0.0)
                utm.Northing += 10000000.0;                     // 10000000 meter offset for southern hemisphere

            utm.Northing = Math.Floor(utm.Northing + 0.5);
            utm.Easting = Math.Floor(utm.Easting + 0.5);
            return utm;
        }

        /// <summary>
        /// returns lat/lon coordinates cooresponding to the given UTM coords using the WGS84 ellipsoid, null if
        /// the UTM coordinates are invalid.
        /// </summary>
        private static CoordLL UTMtoLL(CoordUTM utm)
        {
            if ((utm == null) || (utm.ZoneNumber < 0) || (utm.ZoneNumber > 60))
                return null;

            double k0 = 0.9996;
            double a = 6378137.0;                   // ellipse radius
            double ecc2 = 0.00669438;               // ellipse eccentricity, squared

            double e1 = (1.0 - Math.Sqrt(1.0 - ecc2)) / (1.0 + Math.Sqrt(1 - ecc2));

            double e1_2 = e1 * e1;
            double e1_3 = e1 * e1_2;
            double e1_4 = e1 * e1_3;

            double x = utm.Easting - 500000.0;      // remove 500000 meter offset for longitude
            double y = utm.Northing;

            // we must know somehow if we are in the northern or southern hemisphere, this is the only time we use
            // the letter so even if the zone letter isn't exactly correct it should indicate hemisphere correctly.
            //
            if (MGRS_LETTERS.IndexOf(utm.ZoneLetter) < MGRS_LETTERS.IndexOf('N'))
                y -= 10000000.0;                    // remove 10,000,000 meter offset used for southern hemisphere

            // there are 60 zones with zone 1 being at West -180 to -174, adjust origin by +3 to put it in the
            // middle of the zone.
            //
            int lonOrigin = (utm.ZoneNumber - 1) * 6 - 180 + 3;

            double eccPrime2 = ecc2 / (1.0 - ecc2);
            double ecc4 = ecc2 * ecc2;
            double ecc6 = ecc2 * ecc2 * ecc2;

            double M = y / k0;
            double mu = M / (a * (1.0 - (ecc2 / 4.0) - ((3.0 * ecc4) / 64.0) - ((5.0 * ecc6) / 256.0)));

            double phi1Rad = mu + (((3.0 * e1) / 2.0) - ((27.0 * e1_3) / 32.0)) * Math.Sin(2.0 * mu) +
                                  (((21.0 * e1_2) / 16.0) - ((55.0 * e1_4) / 32.0)) * Math.Sin(4.0 * mu) +
                                  ((151.0 * e1_3) / 96.0) * Math.Sin(6.0 * mu);

            double N1 = a / Math.Sqrt(1.0 - ecc2 * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
            double T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
            double C1 = eccPrime2 * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
            double R1 = a * (1.0 - ecc2) / Math.Pow(1.0 - ecc2 * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
            double D = x / (N1 * k0);

            double C1_2 = C1 * C1;

            double T1_2 = T1 * T1;

            double D2 = D * D;
            double D3 = D * D2;
            double D4 = D * D3;
            double D5 = D * D4;
            double D6 = D * D5;

            double lat = phi1Rad - ((N1 * Math.Tan(phi1Rad)) / R1) * ((D2 / 2.0) -
                         (5.0 + (3.0 * T1) + (10.0 * C1) - (4.0 * C1_2) - (9.0 * eccPrime2)) * D4 / 24.0 +
                         (61.0 + (90.0 * T1) + (298.0 * C1) + (45.0 * T1_2) - (252.0 * eccPrime2) -
                          (3.0 * C1_2)) * D6 / 720.0);
            double lon = (D - (((1.0 + (2.0 * T1) + C1) * D3) / 6.0) +
                              ((5.0 - (2.0 * C1) + (28.0 * T1) - (3.0 * C1_2) +
                                (8.0 * eccPrime2) + (24.0 * T1_2)) * D5) / 120.0) / Math.Cos(phi1Rad);

            return new CoordLL(RadToDeg(lat), RadToDeg(lon) + lonOrigin);
        }

        /// <summary>
        /// returns the MGRS letter designator for the given latitude. throws exception on error
        /// </summary>
        private static string GetLetterDesignator(double lat)
        {
            if ((84.0 >= lat) && (lat >= 72.0))
                return "X";
            else if ((72.0 > lat) && (lat >= 64.0))
                return "W";
            else if ((64.0 > lat) && (lat >= 56.0))
                return "V";
            else if ((56.0 > lat) && (lat >= 48.0))
                return "U";
            else if ((48.0 > lat) && (lat >= 40.0))
                return "T";
            else if ((40.0 > lat) && (lat >= 32.0))
                return "S";
            else if ((32.0 > lat) && (lat >= 24.0))
                return "R";
            else if ((24.0 > lat) && (lat >= 16.0))
                return "Q";
            else if ((16.0 > lat) && (lat >= 8.0))
                return "P";
            else if ((8.0 > lat) && (lat >= 0.0))
                return "N";
            else if ((0.0 > lat) && (lat >= -8.0))
                return "M";
            else if ((-8.0 > lat) && (lat >= -16.0))
                return "L";
            else if ((-16.0 > lat) && (lat >= -24.0))
                return "K";
            else if ((-24.0 > lat) && (lat >= -32.0))
                return "J";
            else if ((-32.0 > lat) && (lat >= -40.0))
                return "H";
            else if ((-40.0 > lat) && (lat >= -48.0))
                return "G";
            else if ((-48.0 > lat) && (lat >= -56.0))
                return "F";
            else if ((-56.0 > lat) && (lat >= -64.0))
                return "E";
            else if ((-64.0 > lat) && (lat >= -72.0))
                return "D";
            else if ((-72.0 > lat) && (lat >= -80.0))
                return "C";

            throw new Exception($"GetLetterDesignator invalid latitude {lat}");
        }

        /// <summary>
        /// return the two-letter 100k designator for a utm easting/northing and zone number.
        /// </summary>
        private static string Get100kID(CoordUTM utm)
            => GetLetter100kID((int)Math.Floor(utm.Easting / 100000.0),
                               (int)Math.Floor(utm.Northing / 100000.0) % 20,
                               Get100kSetForZone(utm.ZoneNumber));

        /// <summary>
        /// return the mgrs 100k set the given utm zone number is in.
        /// </summary>
        private static int Get100kSetForZone(int zoneNumber)
            => ((zoneNumber % MGRS_100K_SETS) == 0) ? MGRS_100K_SETS : zoneNumber % MGRS_100K_SETS;

        /// <summary>
        /// return the two-letter MGRS 100k designator for the given UTM northing, easting, and zone number.
        /// col is on [1, 8], row is on [0, 19], and setBlock is on [1, 6].
        /// </summary>
        private static string GetLetter100kID(int col, int row, int setBlock)
        {
            int I = MGRS_LETTERS.IndexOf('I');
            int O = MGRS_LETTERS.IndexOf('O');
            int V = MGRS_LETTERS.IndexOf('V');
            int Z = MGRS_LETTERS.IndexOf('Z');

            int colOrigin = MGRS_LETTERS.IndexOf(MGRS_COL_ORIGIN[setBlock - 1]);
            int colInt = colOrigin + col - 1;
            bool rollover = false;
            if (colInt > Z)
            {
                colInt = colInt - Z - 1;
                rollover = true;
            }
            if ((colInt == I) || ((colOrigin < I) && (I < colInt)) || (((colInt > I) || (colOrigin < I)) && rollover))
                colInt += 1;
            if ((colInt == O) || ((colOrigin < O) && (O < colInt)) || (((colInt > O) || (colOrigin < O)) && rollover))
                colInt += 1;
            if (colInt == I)
                colInt += 1;
            else if (colInt > Z)
                colInt = colInt - Z - 1;

            int rowOrigin = MGRS_LETTERS.IndexOf(MGRS_ROW_ORIGIN[setBlock - 1]);
            int rowInt = rowOrigin + row;
            rollover = false;
            if (rowInt > V)
            {
                rowInt = rowInt - V - 1;
                rollover = true;
            }
            if (((rowInt == I) || ((rowOrigin < I) && (rowInt > I))) || (((rowInt > I) || (rowOrigin < I)) && rollover))
                rowInt += 1;
            if (((rowInt == O) || ((rowOrigin < O) && (rowInt > O))) || (((rowInt > O) || (rowOrigin < O)) && rollover))
                rowInt += 1;
            if (rowInt == I)
                rowInt += 1;
            else if (rowInt > V)
                rowInt = rowInt - V - 1;

            return $"{MGRS_LETTERS[colInt]}{MGRS_LETTERS[rowInt]}";
        }

        /// <summary>
        /// returns the easting value that should be added to the secondary easting value based on the first letter
        /// of a two-letter mgrs 100k zone and mgrs table set for the zone number. throws an exception on error.
        /// </summary>
        private static double GetEastingFromChar(char e, int set)
        {
            int I = MGRS_LETTERS.IndexOf('I');
            int O = MGRS_LETTERS.IndexOf('O');
            int Z = MGRS_LETTERS.IndexOf('Z');

            int curCol = MGRS_LETTERS.IndexOf(MGRS_COL_ORIGIN[set - 1]);
            double eastingValue = 100000.0;
            bool rewindMarker = false;
            while (curCol != MGRS_LETTERS.IndexOf(e))
            {
                curCol += 1;
                if (curCol == I)
                {
                    curCol += 1;
                }
                else if (curCol == O)
                {
                    curCol += 1;
                }
                else if (curCol > Z)
                {
                    if (rewindMarker)
                        throw new Exception($"GetEastingFromChar found bad character '{e}'");

                    curCol = 0;
                    rewindMarker = true;
                }
                eastingValue += 100000.0;
            }
            return eastingValue;
        }

        /// <summary>
        /// returns the northing value that should be added to the secondary northing value based on the second
        /// letter of a two-letter mgrs 100k zone and mgrs table set for the zone number. throws an exception
        /// on error.
        /// 
        /// remember that northings are determined from the equator, and the vertical cycle of letters imply
        /// 2000000 additional northing meters. this happens approx.every 18 degrees of latitude. this method
        /// does *NOT* count any additional northings and assumes callers figure out how many 2000000 meters
        /// need to be added for the zone letter of the mgrs coordinate.
        /// </summary>
        private static double GetNorthingFromChar(char n, int set)
        {
            if (MGRS_LETTERS.IndexOf(n) > MGRS_LETTERS.IndexOf('V'))
                throw new Exception($"GetNorthingFromChar has invalid northing '{n}'");

            int I = MGRS_LETTERS.IndexOf('I');
            int O = MGRS_LETTERS.IndexOf('O');
            int V = MGRS_LETTERS.IndexOf('V');

            int curRow = MGRS_LETTERS.IndexOf(MGRS_ROW_ORIGIN[set - 1]);
            double northingValue = 0.0;
            bool rewindMarker = false;
            while (curRow != MGRS_LETTERS.IndexOf(n))
            {
                curRow += 1;
                if (curRow == I)
                {
                    curRow += 1;
                }
                else if (curRow == O)
                {
                    curRow += 1;
                }
                else if (curRow > V)
                {
                    if (rewindMarker)
                        throw new Exception($"GetNorthingFromChar found bad character '{n}'");
                    curRow = 0;
                    rewindMarker = true;
                }
                northingValue += 100000.0;
            }
            return northingValue;
        }

        /// <summary>
        /// return minimum northing value for an mgrs zone. throws an exception if the zone is invalid.
        /// </summary>
        private static double GetMinNorthing(char zoneLetter)
        {
            if (_minNorthingMap.TryGetValue(zoneLetter, out double northing))
                return northing;
            else
                throw new Exception($"GetMinNorthing found invalid zone letter '{zoneLetter}'");
        }
    }
}
