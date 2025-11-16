// ********************************************************************************************************************
//
// ExchangeConfigUIHelper.cs : helper classes for configuration exchange (import/export) ui
//
// Copyright(C) 2025 ilominar/raven
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
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// configuration files.
    /// </summary>
    public partial class ExchangeConfigUIHelper : ExchangeUIHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // config export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// export a configuration to a .jafdtc file at the given path (null path causes prompt via picker). notes
        /// success via a message dialog if root is non-null. throws an exception on issues, caller should catch.
        /// </summary>
        public static async void ConfigExportJAFDTC(XamlRoot root, IConfiguration config, string path = null)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleanConfigName = new([.. config.Name.Where(m => !invalidChars.Contains(m)) ]);

            path ??= await SavePickerUI("Export Configuration", cleanConfigName, "JAFDTC", ".jafdtc");
            if (path != null)
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                IConfiguration cleanConfig = config.Clone();
                cleanConfig.Sanitize();
                await FileIO.WriteTextAsync(file, cleanConfig.Serialize());

                if (root != null)
                    await Utilities.Message1BDialog(root, "Success",
                                                    $"Exported configuration “{config.Name}” to the file at:\n\n“{path}”.");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // config import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// import the .jafdtc file with user interaction. user is prompted for a name for the new configuration
        /// along with a pilot role (if roles are supported). returns imported configuration, null on error.
        /// </summary>
        public static async Task<IConfiguration> ConfigImportJAFDTC(XamlRoot root, ConfigurationList configList,
                                                                    string path = null)
        {
            path ??= await OpenPickerUI("Import Configuration", [ ".jafdtc" ]);
            if (path != null)
            {
                IConfiguration config = FileManager.ReadUnmanagedConfigurationFile(path);

                if (config.Airframe != configList.Airframe)
                    configList = new(config.Airframe);
                string importName = configList.UniquifyName(config.Name);

                ImportBaseDialog importDialog = new(configList, config, importName)
                {
                    XamlRoot = root,
                    Title = $"Create a New Configuration From File",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await importDialog.ShowAsync(ContentDialogPlacement.Popup);
                if (result == ContentDialogResult.Primary)
                {
                    config.Name = importDialog.ConfigName;
                    config.AdjustForRole(importDialog.ConfigRole);

                    IConfiguration newConfig = configList.Inject(config);

                    await Utilities.Message1BDialog(root, "Success",
                                                    $"Created a new configuration named “{config.Name}”" +
                                                    $" for the {Globals.AirframeNames[config.Airframe]}" +
                                                    $" from the file at:\n\n{path}");
                    return newConfig;
                }
            }
            return null;
        }

        /// <summary>
        /// import the .jafdtc file silently without any user interaction. the name will be uniquified, but role
        /// changes are not handled. returns imported configuration, null on error.
        /// </summary>
        public static IConfiguration ConfigSilentImportJAFDTC(string path, ConfigurationList configList = null)
        {
            IConfiguration config = FileManager.ReadUnmanagedConfigurationFile(path);
            if (configList == null)
            {
                configList = new(config.Airframe);
                config.Name = configList.UniquifyName(config.Name);
                config.Sanitize(true);
                config.Save();
            }
            else
            {
                config.Name = configList.UniquifyName(config.Name);
                configList.Inject(config);
            }
            return config;
        }
    }
}
