// ********************************************************************************************************************
//
// FA18CConfigurationEditor.cs : supports editors for the fa18c configuration
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
using JAFDTC.Models.FA18C;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the viper configuration.
    /// </summary>
    internal class Glyphs
    {
        public const string CMS = UI.Glyphs.Countermeasures;
        public const string MISC = UI.Glyphs.Miscellaneous;
        public const string PP = "\xE8FD";
        public const string RADIO = UI.Glyphs.Radio;
        public const string WYPT = UI.Glyphs.Navigation;
    }

    /// <summary>
    /// instance of a configuration editor for the fa-18c hornet. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class FA18CConfigurationEditor : ConfigurationEditorBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo
            => [
                FA18CEditWaypointListHelper.PageInfo,
                FA18CEditRadioPageHelper.PageInfo,
                FA18CEditPreplanPage.PageInfo,
                FA18CEditCMSPage.PageInfo,
#if TODO_IMPLEMENT
                FA18CEditCoreSimDTCPageHelper.PageInfo
#endif
            ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? config.WYPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = config.WYPT.Points[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"SP{info.TagInt}";
                return name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = config.WYPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }
    }
}
