// ********************************************************************************************************************
//
// ConfigEditorPageInfo.cs -- information on a configuration editor page
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
using Microsoft.UI.Xaml.Media;
using System;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// holds information on the editor page for a section of an airframe configuration. ConfigurationPage
    /// uses this data to dynamically build the content of the configuration editor page navigation list.
    /// </summary>
    public sealed partial class ConfigEditorPageInfo : BindableObject
    {
        public string Tag { get; }

        public string Label { get; }

        public string ShortName { get; }

        public string Glyph { get; }

        public Type EditorPageType { get; }

        public Type EditorHelperType { get; }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the icon in the nav
        // list used to select the configuration.

        private Brush _editorPageIconFg;
        public Brush EditorPageIconFg
        {
            get => _editorPageIconFg;
            set => SetProperty(ref _editorPageIconFg, value);
        }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the badge in the nav
        // list used to select the configuration.

        private Brush _editorPageBadgeFg;
        public Brush EditorPageBadgeFg
        {
            get => _editorPageBadgeFg;
            set => SetProperty(ref _editorPageBadgeFg, value);
        }

        public ConfigEditorPageInfo(string tag, string label, string name, string glyph, Type pageType, Type helpType = null)
            => (Tag, Label, ShortName, Glyph,
                EditorPageType, EditorHelperType,
                EditorPageIconFg, EditorPageBadgeFg) = (tag, label, name, glyph, pageType, helpType, null, null);
    }
}
