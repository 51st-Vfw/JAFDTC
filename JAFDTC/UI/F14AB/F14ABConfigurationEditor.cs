﻿// ********************************************************************************************************************
//
// F14ABConfigurationEditor.cs : supports editors for the f-14a/b configuration
//
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.Models;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.F14AB
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the tomcat configuration.
    /// </summary>
    public class Glyphs
    {
        public const string WYPT = "\xE707";
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class F14ABConfigurationEditor : ConfigurationEditor
    {
        public F14ABConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo()
            => new()
            {
                F14ABEditWaypointListHelper.PageInfo,
            };
    }
}
