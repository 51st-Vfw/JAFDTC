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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
    public class A10CConfigurationEditor : ConfigurationEditorBase, IMapControlVerbHandler
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private A10CConfiguration ConfigA10C => (A10CConfiguration)Config;

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CConfigurationEditor(IConfiguration config, ConfigurationPage configPage = null)
            => (Config, ConfigPage) = (config, configPage);

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
            MapWindow mapWindow = application.CreateMapWindow(true, true);

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
                [WYPTSystem.SystemInfo.RouteNames[0]] = [.. ConfigA10C.WYPT.Points ]
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
            mapWindow.SetupMapContent(routes, marks, [ ], ConfigA10C.LastMapSetup);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            return (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT) ? ConfigA10C.WYPT.SysInfo.NavptName
                                                                      : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = ConfigA10C.WYPT.Points[info.Tag.Int - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"SP{info.Tag.Int}";
                return name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = ConfigA10C.WYPT.Points[info.Tag.Int - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "A10CConfigurationEditor";

        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerSelected({param}) {info.Tag}");
        }

        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:MarkerOpen({param}) {info.Tag}");
            JAFDTC.App application = Application.Current as JAFDTC.App;
            application.Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ConfigPage.SwitchToEditorForSystem(WYPTSystem.SystemTag);
                application.Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    application.MapWindow?.MirrorVerbMarkerOpened(sender, info, param);
                });
            });
        }

        public void VerbMarkerUpdated(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerUpdated({param}) {info.Tag}");
        }

        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerMoved({param}) {info.Tag} {info.TagAux} / {info.Lat}, {info.Lon}");
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                string alt = "";
                if (!info.TagAux.IsUnknown)
                {
                    PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagAux.Str);
                    if (poi != null)
                    {
                        alt = poi.Elevation;
                    }
                    else
                    {
// TODO: handle other snap targets (threats?)
                    }
                }

                ConfigA10C.WYPT.Points[info.Tag.Int - 1].Lat = info.Lat;
                ConfigA10C.WYPT.Points[info.Tag.Int - 1].Lon = info.Lon;
                ConfigA10C.WYPT.Points[info.Tag.Int - 1].Alt = alt;
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerAdded({param}) {info.Tag} / {info.Lat}, {info.Lon}");
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                WaypointInfo wypt = ConfigA10C.WYPT.Add(null, info.Tag.Int - 1);
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
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerDeleted({param}) {info.Tag}");
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigA10C.WYPT.Delete(ConfigA10C.WYPT.Points[info.Tag.Int - 1]);
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }
    }
}
