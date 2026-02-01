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
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
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
    public class FA18CConfigurationEditor : ConfigurationEditorBase, IMapControlVerbHandler
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private FA18CConfiguration ConfigFA18C => (FA18CConfiguration)Config;

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CConfigurationEditor(IConfiguration config, ConfigurationPage configPage = null)
            => (Config, ConfigPage) = (config, configPage);

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

        public override void SetupMapWindow()
        {
            JAFDTC.App application = Application.Current as JAFDTC.App;
            MapWindow mapWindow = application.CreateMapWindow(true, true);

            // check the theater implied by any threats. default theater is whatever is currently selected.
            //
            string theater = mapWindow.Theater;
// TODO: support threats here
            List<string> theaters = []; // TODO: Models.Planning.Threat.TheatersForThreats(config.Mission.Threats);
            if (theaters.Count > 0)
                theater = theaters[0];

            // check the theater implied by any steerpoints. this theater will override the threats theater in the
            // case both are specified.
            //
            Dictionary<string, List<INavpointInfo>> routes = new()
            {
                [WYPTSystem.SystemInfo.RouteNames[0]] = [.. ConfigFA18C.WYPT.Points ]
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
                PointOfInterestDbaseQuery query = new(PointOfInterestTypeMask.ANY, theater);
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
            mapWindow.SetupMapContent(routes, marks, [], ConfigFA18C.LastMapMarkerImport, ConfigFA18C.LastMapFilter);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? ConfigFA18C.WYPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = ConfigFA18C.WYPT.Points[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"SP{info.TagInt}";
                return name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = ConfigFA18C.WYPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "FA18CConfigurationEditor";

        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerSelected({param}) {info.Type} {info.TagStr}:{info.TagInt}");
        }

        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:MarkerOpen({param}) {info.Type} {info.TagStr}:{info.TagInt}");
        }

        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerMoved({param}) {info.Type} {info.TagStr}:{info.TagInt} / {info.Lat}, {info.Lon}");
            if (info.TagStr == WYPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigFA18C.WYPT.Points[info.TagInt - 1].Lat = info.Lat;
                ConfigFA18C.WYPT.Points[info.TagInt - 1].Lon = info.Lon;
// TODO: what about altitude?
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerAdded({param}) {info.Type} {info.TagStr}:{info.TagInt} / {info.Lat}, {info.Lon}");
            if (info.TagStr == WYPTSystem.SystemInfo.RouteNames[0])
            {
                WaypointInfo wypt = ConfigFA18C.WYPT.Add(null, info.TagInt - 1);
                wypt.Lat = info.Lat;
                wypt.Lon = info.Lon;
// TODO: what about altitude?
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerDeleted({param}) {info.Type} {info.TagStr}:{info.TagInt}");
            if (info.TagStr == WYPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigFA18C.WYPT.Delete(ConfigFA18C.WYPT.Points[info.TagInt - 1]);
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }
    }
}
