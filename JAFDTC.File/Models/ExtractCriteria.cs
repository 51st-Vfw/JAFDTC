// ********************************************************************************************************************
//
// ExtractCriteria.cs -- jafdtc model object extraction criteria for use in IExtractor
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
using JAFDTC.Models.Units;

namespace JAFDTC.File.Models
{
    public class ExtractCriteria
    {
        public required string FilePath { get; set; }
        public string? Theater { get; set; }
        public CoalitionType[]? Coalitions { get; set; }
        public UnitCategoryType[]? UnitCategories { get; set; }
        public string[]? UnitTypes { get; set; } // TODO: move this to an enum?
        public bool? IsAlive { get; set; }
        public DateTimeOffset? TimeSnippet { get; set; }
    }
}
