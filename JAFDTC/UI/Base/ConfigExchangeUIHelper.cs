// ********************************************************************************************************************
//
// ConfigExchangeUIHelper.cs : helper classes for configuration exchange (import/export) ui
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
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// configuration files.
    /// </summary>
    public partial class ConfigExchangeUIHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // config export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// manage a FileSavePicker to select a .jafdtc file to save to. returns the path of the selected file,
        /// null if no selection was made.
        /// </summary>
        private static async Task<string> SavePickerUI()
        {
            FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Export Configuration",
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = "Configuration"
            };
            picker.FileTypeChoices.Add("JAFDTC", [".jafdtc"]);
            PickFileResult resultPick = await picker.PickSaveFileAsync();
            return resultPick?.Path;
        }

        /// <summary>
        /// export a configuration to a .jafdtc file at the given path (null path causes prompt via picker). notes
        /// success via a message dialog if root is non-null. throws an exception on issues, caller should catch.
        /// </summary>
        public static async void ConfigExportJAFDTC(XamlRoot root, IConfiguration config, string path = null)
        {
            path ??= await SavePickerUI();
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
        /// manage a FileSavePicker to select a .jafdtc file to load from. returns the path of the selected file,
        /// null if no selection was made.
        /// </summary>
        private static async Task<string> OpenPickerUI()
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Import Configuration",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".jafdtc");
            PickFileResult resultPick = await picker.PickSingleFileAsync();
            return resultPick?.Path;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static async Task<ContentDialogResult> ImportDissimilarAirframeUI(XamlRoot root,
                                                                                  ConfigurationList configList,
                                                                                  IConfiguration config)
        {
// TODO: on disimilar imports, may want to prompt for config to merge with or allow direct import?
            await Utilities.Message1BDialog(root, "Mismatched Airframe", $"Cannot import that file here.");
            return ContentDialogResult.None;
        }

        /// <summary>
        /// handle the ui for importing a configuration for an airframe that matches that of the configuration
        /// list. updates the input configuration with the new name and adjusts it based on any role adjustements
        /// specified. returns the result of the dialog.
        /// </summary>
        private static async Task<ContentDialogResult> ImportSimilarAirframeUI(XamlRoot root,
                                                                               ConfigurationList configList,
                                                                               IConfiguration config)
        {
            string importName = configList.UniquifyName(config.Name);
            ImportBaseDialog importDialog = new(configList, config, importName)
            {
                XamlRoot = root,
                Title = $"Creating New Configuration From File",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await importDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                config.Name = importDialog.ConfigName;
                config.AdjustForRole(importDialog.ConfigRole);
            }
            return result;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static async Task<IConfiguration> ConfigImportJAFDTC(XamlRoot root, ConfigurationList configList,
                                                                    string path = null)
        {
            path ??= await OpenPickerUI();
            if (path != null)
            {
                IConfiguration config = FileManager.ReadUnmanagedConfigurationFile(path);

                ContentDialogResult result;
                if (config.Airframe != configList.Airframe)
                    result = await ImportDissimilarAirframeUI(root, configList, config);
                else
                    result = await ImportSimilarAirframeUI(root, configList, config);

                if (result == ContentDialogResult.Primary)
                {
                    IConfiguration newConfig = configList.Inject(config);
                    if (root != null)
                    {
                        await Utilities.Message1BDialog(root, "Success",
                                                        $"Created a new configuration named “{config.Name}” " +
                                                        $"from the file at:\n\n{path}");
                        return newConfig;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// TODO: document
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
