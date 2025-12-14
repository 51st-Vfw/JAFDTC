// ********************************************************************************************************************
//
// TheaterInfo.cs -- theater information model
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

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// defines information on a theater including the boundary of a theater on the map in terms of a min/max latitude
    /// and longitude along with time zone information.
    /// </summary>
    public sealed class TheaterInfo
    {
        public double LatMin { get; }
        public double LatMax { get; }
        public double LonMin { get; }
        public double LonMax { get; }

        public int Zulu { get; }

        public TheaterInfo(double latMin, double latMax, double lonMin, double lonMax, int zulu)
            => (LatMin, LatMax, LonMin, LonMax, Zulu) = (latMin, latMax, lonMin, lonMax, zulu);

        public bool Contains(double lat, double lon)
            => ((LatMin <= lat) && (lat <= LatMax) && (LonMin <= lon) && (lon <= LonMax));
    }
}
