// ********************************************************************************************************************
//
// UnitItem.cs : jafdtc unit item type
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

namespace JAFDTC.Models.Units
{
    public class UnitItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for unit
// TODO: make Type an enum? need some mapping from string to enum for ui, possibly
        public required string Type { get; set; }                           // unit type
        public required string Name { get; set; }                           // unit name
        public required UnitPositionItem Position { get; set; }             // unit position
        public required bool IsAlive { get; set; }                          // t => alive
    }
}
