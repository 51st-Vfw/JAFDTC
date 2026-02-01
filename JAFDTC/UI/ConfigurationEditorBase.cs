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
using JAFDTC.UI.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.UI.F15E;
using JAFDTC.UI.F16C;
using JAFDTC.UI.FA18C;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI
{
    /// <summary>
    /// abstract base class for a configuration editor that supports editing configurations via the ui. these
    /// objects implement IConfigurationEditor and IMapControlMarkerExplainer. derived classes may also implement
    /// IMapControlVerbHandler to provide baseline verb handling for map controls. the abstract base class provides
    /// a factory method that builds concrete instances to edit a specified IConfiguration instance.
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
        public static IConfigurationEditor Factory(IConfiguration config, ConfigurationPage configPage = null)
            => config.Airframe switch
            {
                AirframeTypes.A10C => new A10CConfigurationEditor(config, configPage),
                AirframeTypes.F16C => new F16CConfigurationEditor(config, configPage),
                AirframeTypes.F15E => new F15EConfigurationEditor(config, configPage),
                AirframeTypes.FA18C => new FA18CConfigurationEditor(config, configPage),
                _ => null,
            };

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public IConfiguration Config { get; set; }

        public ConfigurationPage ConfigPage { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor, virtual methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// base implementation returns an empty list.
        /// 
        /// derived classes must override this to provide the correct list of editor page information for the
        /// systems in the configuration.
        /// </summary>
        public virtual ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo => [ ];

        /// <summary>
        /// base implementation returns an empty list.
        /// 
        /// derived classes must override this to provide the correct list of editor page information for the
        /// systems in the configuration.
        /// </summary>
        public virtual ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo => [ ];

        /// <summary>
        /// base implementation returns an baseline dictionary suitable for common cases.
        /// 
        /// derived classes may override this to provide the information for the update strings suitable for the
        /// airframe and configuration.
        /// </summary>
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
                infoText = $"Sets up {General.JoinList(sysList)} system" + ((sysList.Count > 1) ? "s" : "");

            return new Dictionary<string, string>()
            {
                ["SystemInfoTextUI"] = infoText,
                ["SystemInfoIconsUI"] = icons,
                ["SystemInfoIconBadgesUI"] = iconBadges,
            };
        }

        /// <summary>
        /// base implementation always returns false.
        /// 
        /// derived classes for configurations that support aux commands must override this method to correctly
        /// handle the aux command.
        /// </summary>
        public virtual bool HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd) => false;

        /// <summary>
        /// base implementation is an empty function.
        /// 
        /// derived classes for configurations that support the map window must override this method to correctly
        /// set up the map window.
        /// </summary>
        public virtual void SetupMapWindow() { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer, virtual methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// base implementation calls the base MarkerExplainerHelper method.
        /// 
        /// derived classes may override this method to further customize the return value.
        /// </summary>
        public virtual string MarkerDisplayType(MapMarkerInfo info)
            => MarkerExplainerHelper.MarkerDisplayType(info);

        /// <summary>
        /// base implementation calls the base MarkerExplainerHelper method.
        /// 
        /// derived classes may override this method to further customize the return value.
        /// </summary>
        public virtual string MarkerDisplayName(MapMarkerInfo info)
            => MarkerExplainerHelper.MarkerDisplayName(info);

        /// <summary>
        /// base implementation calls the base MarkerExplainerHelper method.
        /// 
        /// derived classes may override this method to further customize the return value.
        /// </summary>
        public virtual string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
            => MarkerExplainerHelper.MarkerDisplayElevation(info, units);
    }
}
