// ********************************************************************************************************************
//
// Pilot.cs -- planning model pilot information
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
    public class Pilot
    {
        public required string Name { get; set; }
        public required bool IsLead { get; set; }
        public string? DataId { get; set; }
        public string? SCL { get; set; }
        //public string? Board { get; set; }
        //public int? Tacan { get; set; }
        //public char? TacanBand { get; set; }
        //public int? Joker { get; set; }
        //public int? LaseCode { get; set; }
    }
}
