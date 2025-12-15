// ********************************************************************************************************************
//
// UnitPositionItem.cs : jafdtc unit item type
//
// Copyright(C) 2025 ilominar/raven, rage
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

using System.Text.Json.Serialization;

namespace JAFDTC.Models.Units
{
    public class UnitPositionItem
    {
        public string? Name { get; set; }                                   // position name (optional)
        public required double Latitude { get; set; }                       // decimal degrees
        public required double Longitude { get; set; }                      // decimal degrees
        public double Altitude { get; set; }                                // feet
        public double TimeOn { get; set; }                                  // s in day (local), < 0 => no time on

        [JsonIgnore]
        public string TimeOnAsHMS
            => (TimeOn >= 0.0) ? string.Format("{0:00}:{1:00}:{2:00}",
                                               (((int)TimeOn % 86400) / 3600),
                                               (((int)TimeOn % 86400) / 60) % 60,
                                               (((int)TimeOn % 86400) % 60))
                               : string.Empty;
    }
}
