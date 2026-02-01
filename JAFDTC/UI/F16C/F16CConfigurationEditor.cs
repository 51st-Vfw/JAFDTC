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
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

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
    /// along with abstracting some access to internal system configuration state for use by ui editors.
    /// </summary>
    public class F16CConfigurationEditor : ConfigurationEditorBase, IMapControlVerbHandler
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private F16CConfiguration ConfigF16C => (F16CConfiguration)Config;

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

        public F16CConfigurationEditor(IConfiguration config, ConfigurationPage configPage = null)
            => (Config, ConfigPage) = (config, configPage);

        public override void SetupMapWindow()
        {
            JAFDTC.App application = Application.Current as JAFDTC.App;
            MapWindow mapWindow = application.CreateMapWindow(true, true);

            // check the theater implied by any threats. default theater is whatever is currently selected.
            //
            string theater = mapWindow.Theater;
            List<string> theaters = Models.Planning.Threat.TheatersForThreats(ConfigF16C.Mission.Threats);
            if (theaters.Count > 0)
                theater = theaters[0];

            // check the theater implied by any steerpoints. this theater will override the threats theater in the
            // case both are specified.
            //
            Dictionary<string, List<INavpointInfo>> routes = new()
            {
                [STPTSystem.SystemInfo.RouteNames[0]] = [.. ConfigF16C.STPT.Points ]
            };
            List<INavpointInfo> allRoutes = [ ];
            foreach (string route in routes.Keys)
                allRoutes.AddRange(routes[route]);
            theaters = NavpointUIHelper.TheatersForNavpoints(allRoutes);
            if (theaters.Count > 0)
                theater = theaters[0];

            // collect the pois that match the identified theater.
            //
            Dictionary<string, PointOfInterest> marks = [ ];
            if (theater != null)
            {
                PointOfInterestDbaseQuery query = new(PointOfInterestTypeMask.ANY, theater);
                foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                    marks[poi.UniqueID] = poi;
            }

            // configure the map window with the appropriate content.
            //
            bool isLinked = !string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));

            mapWindow.Theater = theater;
            mapWindow.OpenMask = MapMarkerInfo.MarkerTypeMask.NAV_PT;
            mapWindow.EditMask = ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.NAV_PT) |
                                 ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.PATH_EDIT_HANDLE);
            mapWindow.CoordFormat = STPTSystem.SystemInfo.NavptCoordFmt;
            mapWindow.MaxRouteLength = STPTSystem.SystemInfo.NavptMaxCount;

            mapWindow.SetupMapContent(routes, marks, ConfigF16C.Mission.Threats,
                                      ConfigF16C.LastMapMarkerImport, ConfigF16C.LastMapFilter);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string MarkerDisplayType(MapMarkerInfo info)
        {
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? ConfigF16C.STPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);
        }

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = ConfigF16C.STPT.Points[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"SP{info.TagInt}";
                string tos = ConfigF16C.STPT.Points[info.TagInt - 1].TOS;
                if (!string.IsNullOrEmpty(tos))
                    name = $"{name} / TOS {tos}";
                return name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = ConfigF16C.STPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "F16CConfigurationEditor";

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
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigF16C.STPT.Points[info.TagInt - 1].Lat = info.Lat;
                ConfigF16C.STPT.Points[info.TagInt - 1].Lon = info.Lon;
// TODO: what about altitude?
                Config.Save(this, STPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(STPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerAdded({param}) {info.Type} {info.TagStr}:{info.TagInt} / {info.Lat}, {info.Lon}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                SteerpointInfo stpt = ConfigF16C.STPT.Add(null, info.TagInt - 1);
                stpt.Lat = info.Lat;
                stpt.Lon = info.Lon;
// TODO: what about altitude?
                Config.Save(this, STPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(STPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerDeleted({param}) {info.Type} {info.TagStr}:{info.TagInt}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigF16C.STPT.Delete(ConfigF16C.STPT.Points[info.TagInt - 1]);
                Config.Save(this, STPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(STPTSystem.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }
    }
}
