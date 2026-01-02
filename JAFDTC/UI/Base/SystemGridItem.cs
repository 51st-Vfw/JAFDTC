// ********************************************************************************************************************
//
// SystemGridItem.cs : backing object for a grid view button that represents a system
//
// Copyright(C) 2026 ilominar/raven
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

using JAFDTC.Utilities;
using System;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// object representing the user interface view of a system that can appear in a GridView. this includes a name
    /// and glyph to display in a button that represents the system.
    /// </summary>
    public sealed partial class SystemGridItem : BindableObject
    {
        public string Tag { get; set; }

        public string Glyph { get; set; }

        public string Name { get; set; }

        public Boolean IsChecked { get; set; }

        public SystemGridItem(string tag, string glyph, string name, Boolean isChecked)
            => (Tag, Glyph, Name, IsChecked) = (tag, glyph, name, isChecked);
    }
}
