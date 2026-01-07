// ********************************************************************************************************************
//
// GenerateCriteria.cs -- kneeboard generator generation criteria
//
// Copyright(C) 2026 rage
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

using JAFDTC.Models.Planning;

namespace JAFDTC.Kneeboard.Models
{
    public class GenerateCriteria
    {
        public required string PathTemplates { get; set; } // directory with .svg templates for each kneeboard
        public required string PathOutput { get; set; } // output path specified by user
        public required Mission Mission { get; set; } // description of what we are planning
        public bool? IsNightMode { get; set; } // enables "night mode" tint layer in template
        public bool? IsSVGMode { get; set; } // enables .svg output, else .png
    }
}