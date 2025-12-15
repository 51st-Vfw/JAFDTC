// ********************************************************************************************************************
//
// UnitGroupItem.cs : jafdtc unit group item type
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

using JAFDTC.Models.Core;

namespace JAFDTC.Models.Units
{
    public class UnitGroupItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for group
        public required CoalitionType Coalition { get; set; }               // coalition of group
        public required UnitCategoryType Category { get; set; }             // category of units in group
        public required string Name { get; set; }                           // group name
        public required List<UnitItem> Units { get; set; }                  // list of units in group
        public required List<UnitPositionItem> Route { get; set; }          // list of positions along route
    }
}
