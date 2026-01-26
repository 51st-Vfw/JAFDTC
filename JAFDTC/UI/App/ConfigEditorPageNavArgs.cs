// ********************************************************************************************************************
//
// ConfigEditorPageNavArgs.cs -- information on a configuration editor page provided when navigating
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

using JAFDTC.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// class encapsulating arguments/parameters to pass in to a system editor page.
    /// </summary>
    public sealed partial class ConfigEditorPageNavArgs
    {
        public ConfigurationPage ConfigPage { get; }

        public IConfiguration Config { get; }

        public Dictionary<string, IConfiguration> UIDtoConfigMap { get; }

        public Type EditorHelperType { get; }

        public AppBarButton BackButton { get; }

        public ConfigEditorPageNavArgs(ConfigurationPage cfgPage, IConfiguration cfg, Type type, Dictionary<string,
                                       IConfiguration> map, AppBarButton backButton)
            => (ConfigPage, Config, EditorHelperType, UIDtoConfigMap, BackButton) = (cfgPage, cfg, type, map, backButton);
    }
}
