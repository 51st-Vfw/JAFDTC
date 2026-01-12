// ********************************************************************************************************************
//
// Preset.cs -- planning model radio preset setting
//
// Copyright(C) 2026 rage, ilominar/raven
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

namespace JAFDTC.Models.Planning
{
    public class Preset
    {
        public required int PresetId { get; set; }              // preset number
        public required string Frequency { get; set; }          // frequency in avionics format (eg, "123.000")

        public string? Modulation { get; set; }                 // modulation (eg, "AM")
        public string? Description { get; set; }                // description of preset
    }
}
