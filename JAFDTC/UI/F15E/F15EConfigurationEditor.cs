﻿// ********************************************************************************************************************
//
// F15EConfigurationEditor.cs : supports editors for the f16c configuration
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
using JAFDTC.Models.F15E;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the viper configuration.
    /// </summary>
    public class Glyphs
    {
        public const string MISC = "\xE8B7";
        public const string MPD = "\xE950";
        public const string RADIO = "\xE704";
        public const string STPT = "\xE707";
        public const string UFC = "\xF261";
        public const string PILOT = "\xE806";
        public const string WSO = "\xF272";
    }

    /// <summary>
    /// instance of a configuration editor for the f-16c viper. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class F15EConfigurationEditor : ConfigurationEditor
    {
        private static readonly ObservableCollection<ConfigEditorPageInfo> _configEditorPageInfo = new()
        {
            F15EEditSteerpointListPage.PageInfo,
            F15EEditMPDPage.PageInfo,
            F15EEditRadioPageHelper.PageInfo,
            F15EEditUFCPage.PageInfo,
            F15EEditMiscPage.PageInfo,
        };

        private static readonly ObservableCollection<ConfigAuxCommandInfo> _configAuxCmdPilot = new()
        {
            new("Pilot", "Pilot Seat", Glyphs.PILOT)
        };

        private static readonly ObservableCollection<ConfigAuxCommandInfo> _configAuxCmdWSO = new()
        {
            new("WSO", "WSO Seat", Glyphs.WSO)
        };

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo() => _configEditorPageInfo;

        public override ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo()
            => (((F15EConfiguration)Config).CrewMember == F15EConfiguration.CrewPositions.PILOT) ? _configAuxCmdPilot
                                                                                                 : _configAuxCmdWSO;

        public F15EConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override bool HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd)
        {
            F15EConfiguration cfgEagle = (F15EConfiguration)Config;
            cfgEagle.CrewMember = (cfgEagle.CrewMember == F15EConfiguration.CrewPositions.PILOT)
                ? F15EConfiguration.CrewPositions.WSO : F15EConfiguration.CrewPositions.PILOT;

            // inform system editors that pilot/wso seat has changed so they can update their state appropriately.
            //
            configPage.RaiseAuxCommandInvoked(cmd);
            return true;
        }
    }
}
