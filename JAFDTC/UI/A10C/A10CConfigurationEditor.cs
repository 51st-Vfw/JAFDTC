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
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
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

        public override void SetupMapWindow()
        {
            JAFDTC.App application = Application.Current as JAFDTC.App;
            MapWindow mapWindow = application.CreateMapWindow();
            A10CConfiguration config = (A10CConfiguration)Config;

            // check the theater implied by any threats. default theater is whatever is currently selected.
            //
            string theater = mapWindow.Theater;
// TODO: support threats here
            List<string> theaters = [ ]; // TODO: Models.Planning.Threat.TheatersForThreats(config.Mission.Threats);
            if (theaters.Count > 0)
                theater = theaters[0];

            // check the theater implied by any steerpoints. this theater will override the threats theater in the
            // case both are specified.
            //
            Dictionary<string, List<INavpointInfo>> routes = new()
            {
                [WYPTSystem.SystemInfo.RouteNames[0]] = [.. config.WYPT.Points ]
            };
            List<INavpointInfo> allRoutes = [];
            foreach (string route in routes.Keys)
                allRoutes.AddRange(routes[route]);
            theaters = NavpointUIHelper.TheatersForNavpoints(allRoutes);
            if (theaters.Count > 0)
                theater = theaters[0];

            // collect the pois that match the identified theater.
            //
            Dictionary<string, PointOfInterest> marks = [];
            if (theater != null)
            {
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, theater);
                foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                    marks[poi.UniqueID] = poi;
            }

            // configure the map window with the appropriate content.
            //
            bool isLinked = !string.IsNullOrEmpty(Config.SystemLinkedTo(WYPTSystem.SystemTag));

            mapWindow.Theater = theater;
            mapWindow.OpenMask = MapMarkerInfo.MarkerTypeMask.NAV_PT;
            mapWindow.EditMask = ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.NAV_PT) |
                                 ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.PATH_EDIT_HANDLE);
            mapWindow.CoordFormat = WYPTSystem.SystemInfo.NavptCoordFmt;
            mapWindow.MaxRouteLength = WYPTSystem.SystemInfo.NavptMaxCount;

// TODO: support threats here
            mapWindow.SetupMapContent(routes, marks, [ ], config.LastMapMarkerImport, config.LastMapFilter);
        }

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
