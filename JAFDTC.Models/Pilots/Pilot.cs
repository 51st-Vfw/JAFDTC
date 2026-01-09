// ********************************************************************************************************************
//
// Pilot.cs -- jafdtc pilot objects
//
// Copyright(C) 2025 rage
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

using JAFDTC.Models.Core;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Pilots
{
    /// <summary>
    /// object modeling a pilots. objects with the same name (case-insensitive) and airframe are considered to be
    /// refer to the same pilot.
    /// </summary>
    public class Pilot
    {
        public required string Name { get; set; }
        public required AirframeTypes Airframe { get; set; }
        public string? BoardNumber { get; set; }
        public string? AvionicsID { get; set; }

        // unique ID is always airframe + name, case-insensitive.
        //
        [JsonIgnore]
        public string UniqueID => $"{Airframe}:{Name.ToLower()}".Replace(" ", "_");
    }
}
