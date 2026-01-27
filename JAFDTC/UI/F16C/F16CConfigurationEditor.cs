// ********************************************************************************************************************
//
// F16CConfigurationEditor.cs : supports editors for the f16c configuration
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the viper configuration.
    /// </summary>
    internal class Glyphs
    {
        public const string CMDS = UI.Glyphs.Countermeasures;
        public const string DLNK = UI.Glyphs.Pilots;
        public const string HARM = "\xE701";
        public const string HTS = "\xF272";
        public const string MFD = UI.Glyphs.Displays;
        public const string MISC = UI.Glyphs.Miscellaneous;
        public const string RADIO = UI.Glyphs.Radio;
        public const string SMS = UI.Glyphs.Munitions;
        public const string STPT = UI.Glyphs.Navigation;
    }

    /// <summary>
    /// instance of a configuration editor for the f-16c viper. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class F16CConfigurationEditor : ConfigurationEditorBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo
            => [
                F16CEditCoreMissionPageHelper.PageInfo,
                F16CEditSteerpointListPage.PageInfo,
                F16CEditMFDPage.PageInfo,
                F16CEditRadioPageHelper.PageInfo,
                F16CEditSMSPage.PageInfo,
                F16CEditCMDSPage.PageInfo,
                F16CEditHARMPage.PageInfo,
                F16CEditHTSPage.PageInfo,
                F16CEditDLNKPage.PageInfo,
                F16CEditMiscPage.PageInfo,
                F16CEditCoreSimDTCPageHelper.PageInfo,
                F16CEditCoreKboardPageHelper.PageInfo,
            ];

        public F16CConfigurationEditor(IConfiguration config) => (Config) = (config);

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? config.STPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = config.STPT.Points[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"SP{info.TagInt}";
                return name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = config.STPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }
    }
}
