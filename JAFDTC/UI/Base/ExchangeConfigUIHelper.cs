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
using System.Diagnostics;
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
        /// export a configuration to a .jafdtc file at the given path. if path is null, prompts the user for a file
        /// location via a FileSavePicker. reports successful status to user via ui. returns null on cancel, true on
        /// success, and false on failure.
        /// 
        /// user interaction is disabled if xaml root is null.
        /// </summary>
        public static async Task<bool?> ExportFile(XamlRoot root, IConfiguration config, string path = null)
        {
            Debug.Assert((root != null) || (path != null));

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleanConfigName = new([.. config.Name.Where(m => !invalidChars.Contains(m)) ]);

            path ??= await SavePickerUI("Export Configuration", cleanConfigName, "JAFDTC", ".jafdtc");
            if (path != null)
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                    IConfiguration cleanConfig = config.Clone();
                    cleanConfig.Sanitize();
                    await FileIO.WriteTextAsync(file, cleanConfig.Serialize());

                    if (root != null)
                        await Utilities.Message1BDialog(root, "Success",
                                                        $"Exported configuration “{config.Name}” to the file:\n\n{path}");

                    return true;
                }
                catch (Exception ex)
                {
                    FileManager.Log($"ExchangeConfigUIHelper:ExportFile fails {ex}");
                    return false;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // config import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// import the .jafdtc file with user interaction. user is prompted for a name for the new configuration
        /// along with a pilot role (if roles are supported). returns imported configuration, null on error or user
        /// cancellation.
        /// 
        /// user interaction is disabled if xaml root is null.
        /// </summary>
        public static async Task<IConfiguration> ImportFile(XamlRoot root, ConfigurationList configList, string path = null)
        {
            Debug.Assert((root != null) || (path != null));

            IConfiguration config = null;
            bool isNoList = (configList == null);

            path ??= await OpenPickerUI("Import Configuration", [ ".jafdtc" ]);
            if (path != null)
            {
                try
                {
                    config = FileManager.ReadUnmanagedConfigurationFile(path);
                    if (isNoList || (config.Airframe != configList.Airframe))
                        configList = new(config.Airframe);
                    string importName = configList.UniquifyName(config.Name);
                    config.Name = importName;

                    if (root != null)
                    {
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

                            await Utilities.Message1BDialog(root, "Success",
                                                            $"Created a new configuration named “{config.Name}”" +
                                                            $" for the {Globals.AirframeNames[config.Airframe]}" +
                                                            $" from the file:\n\n{path}");
                        }
                        else if (result == ContentDialogResult.None)
                        {
                            return null;                                                // **** EXIT: user cancelled
                        }
                    }
                }
                catch (Exception ex)
                {
                    FileManager.Log($"ExchangeConfigUIHelper:ImportFile fails {ex}");
                    return null;                                                        // **** EXIT: config read error
                }

                if (isNoList)
                {
                    config.Sanitize(true);
                    config.Save();
                }
                else
                {
                    configList.Inject(config);
                }
            }

            return config;
        }

        /// <summary>
        /// import the .jafdtc file silently without any user interaction. the name will be uniquified, but role
        /// changes are not handled. returns imported configuration, null on error.
        /// </summary>
        public static IConfiguration SilentImportFile(string path)
        {
            IConfiguration config = FileManager.ReadUnmanagedConfigurationFile(path);
            ConfigurationList configList = new(config.Airframe);
            config.Name = configList.UniquifyName(config.Name);
            config.Sanitize(true);
            config.Save();
            return config;
        }
    }
}
