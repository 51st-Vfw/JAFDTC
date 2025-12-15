// ********************************************************************************************************************
//
// UnitItem.cs -- <one_line_descripti8on>
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
namespace JAFDTC.File.ACMI.Models
{
    public class ParsedUnit
    {
        public required string Id { get; set; } 
        public required ParsedPosition Position { get; set; }
        public required string Coalition { get; set; }
        public required string Category { get; set; }
        public required string Color { get; set; }
        public required string Unit { get; set; }
        public required string GroupName { get; set; }
        public required string UnitName { get; set; }
        public required bool IsAlive { get; set; }
        public Dictionary<string, string>? DebugInfo { get; set; }
    }
}
