// ********************************************************************************************************************
//
// ConfigurationEditorBase.cs : abstract base class for a configuration editor
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
using JAFDTC.Models.Core;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using JAFDTC.UI.F15E;
using JAFDTC.UI.F16C;
using JAFDTC.UI.FA18C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI
{
    /// <summary>
    /// abstract base class for a configuration editor that implements IConfigurationEditor. the abstract base
    /// class provides a factory method to build concrete instances based on airframe.
    /// </summary>
    public abstract class ConfigurationEditorBase : IConfigurationEditor, IMapControlMarkerExplainer
    {

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor instance factory
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns an instance of the configuration editor to use for a particular configuration. null if the
        /// configuration is invalid or for an unsupported airframe.
        /// </summary>
        public static IConfigurationEditor Factory(IConfiguration config)
            => config.Airframe switch
            {
                AirframeTypes.A10C => new A10CConfigurationEditor(config),
                AirframeTypes.F16C => new F16CConfigurationEditor(config),
                AirframeTypes.F15E => new F15EConfigurationEditor(config),
                AirframeTypes.FA18C => new FA18CConfigurationEditor(config),
                _ => null,
            };

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public IConfiguration Config { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor, virtual methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // derived classes must override this to provide the correct list of editor page information for the
        // systems in the configuration.
        //
        public virtual ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo => [ ];

        // derived classes must override this to provide the correct list of auxilliary command information for
        // the configuration.
        //
        public virtual ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo => [ ];

        // derived classes may override this to provide the information for the update strings suitable for the
        // configuration.
        //
        public virtual Dictionary<string, string> BuildUpdatesStrings(IConfiguration config)
        {
            List<string> sysList = [ ];
            string icons = "";
            string iconBadges = "";
            foreach (ConfigEditorPageInfo info in ConfigEditorPageInfo)
            {
                if (!config.IsDefault(info.Tag))
                {
                    sysList.Add(info.ShortName);
                    icons += $" {info.Glyph}";
                    if (config.SystemLinkedTo(info.Tag) != null)
                        iconBadges += $" {Glyphs.CfgLinkBadge}";
                    else
                        iconBadges += $" {info.Glyph}";
                }
            }

            string infoText = "Default setup, no changes to avionics";
            if (sysList.Count > 0)
            {
                infoText = $"Sets up {General.JoinList(sysList)} system" + ((sysList.Count > 1) ? "s" : "");
            }

            return new Dictionary<string, string>()
            {
                ["SystemInfoTextUI"] = infoText,
                ["SystemInfoIconsUI"] = icons,
                ["SystemInfoIconBadgesUI"] = iconBadges,
            };
        }

        // derived classes must override this to handle auxilliary commands.
        //
        public virtual bool HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd) => false;

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer, virtual methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public virtual string MarkerDisplayType(MapMarkerInfo info)
            => info.Type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM => $"Core POI",
                MapMarkerInfo.MarkerType.POI_USER => $"User POI",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => $"Campaign POI",
                _ => ""
            };

        public virtual string MarkerDisplayName(MapMarkerInfo info)
        {
            string name = "";
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    name = poi.Type switch
                    {
                        PointOfInterestType.SYSTEM => $"POI: {poi.Name}",
                        PointOfInterestType.USER => $"User: {poi.Name}",
                        PointOfInterestType.CAMPAIGN => $"{poi.Campaign}: {poi.Name}",
                        _ => throw new NotImplementedException(),
                    };
            }
            return name;
        }

        public virtual string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            string elev = "";
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    elev = $"{poi.Elevation}{units}";
            }
            return elev;
        }
    }
}
