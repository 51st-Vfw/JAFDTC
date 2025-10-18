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
                // SettingsIdentifier = "JAFDTC_ExportCfg",
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
                await FileIO.WriteTextAsync(file, config.Serialize());

                if (root != null)
                    await Utilities.Message1BDialog(root, "Success", $"{config.Name} exported to {path}");
            }
        }
    }
}
