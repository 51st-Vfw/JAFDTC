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
    public class UnitItem
    {
        public required string Id { get; set; } 
        public required PositionItem Position { get; set; }
        public required CoalitionType Coalition { get; set; }
        public required CategoryType Category { get; set; }
        public required ColorType Color { get; set; }
        public required UnitType Unit { get; set; }
        public required string GroupName { get; set; }
        public required string UnitName { get; set; }
        public required bool IsAlive { get; set; }
        public string? DebugInfo { get; set; }
        public string? DebugInfo2 { get; set; }
        public Dictionary<string, string>? DebugInfoDict { get; set; }
    }
}
