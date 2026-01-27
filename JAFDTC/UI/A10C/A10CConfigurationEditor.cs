// ********************************************************************************************************************
//
// A10CConfigurationEditor.cs : supports editors for the a-10c configuration
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
using JAFDTC.Models.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the hawg configuration.
    /// </summary>
    internal class Glyphs
    {
        public const string DSMS =  "\xEBD2";
        public const string HMCS =  "\xEA4A";
        public const string IFFCC = "\xE70A";
        public const string MISC =  UI.Glyphs.Miscellaneous;
        public const string RADIO = UI.Glyphs.Radio;
        public const string TAD =   "\xE8B9";
        public const string TGP =   "\xF272";
        public const string WYPT =  UI.Glyphs.Navigation;
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class A10CConfigurationEditor : ConfigurationEditorBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo
            => [
                // This is the order they appear in the UI. Resist the temptation to alphabetize.
                A10CEditWaypointListHelper.PageInfo,
                A10CEditDSMSPage.PageInfo,
                A10CEditRadioPageHelper.PageInfo,
                A10CEditTADPage.PageInfo,
                A10CEditTGPPage.PageInfo,
                A10CEditHMCSPage.PageInfo,
                A10CEditIFFCCPage.PageInfo,
                A10CEditMiscPage.PageInfo
            ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            A10CConfiguration config = (A10CConfiguration)Config;
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? config.WYPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            A10CConfiguration config = (A10CConfiguration)Config;
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
            A10CConfiguration config = (A10CConfiguration)Config;
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = config.WYPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }
    }
}
