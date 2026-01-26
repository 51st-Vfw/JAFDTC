// ********************************************************************************************************************
//
// ConfigEditorPageInfo.cs -- information on a configuration editor page auxillary page
//
// Copyright(C) 2023-2026 ilominar/raven
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

namespace JAFDTC.UI.App
{
    /// <summary>
    /// holds information on an auxiliary command from the aux list of an airframe configuration. ConfigurationPage
    /// uses this data to dynamically build the content of the configuration editor page.
    /// </summary>
    public sealed partial class ConfigAuxCommandInfo : BindableObject
    {
        public string Tag { get; }

        public string Label { get; }

        public string Glyph { get; }

        public ConfigAuxCommandInfo(string tag, string label, string glyph)
            => (Tag, Label, Glyph) = (tag, label, glyph);
    }
}
