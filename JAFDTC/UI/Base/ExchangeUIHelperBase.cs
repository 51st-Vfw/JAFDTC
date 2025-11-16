// ********************************************************************************************************************
//
// ExchangeUIHelperBase.cs : base helper class for exchange (import/export) ui
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

using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// base for helper class to provide a number of static support functions for use in the user interface around
    /// exchanging various databases.
    /// </summary>
    public partial class ExchangeUIHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // support functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// manage a FileSavePicker to select a file to save to. returns the path of the selected file, null if no
        /// selection was made.
        /// </summary>
        public static async Task<string> SavePickerUI(string button, string filename, string type, string extension)
        {
            FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = button,
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = filename
            };
            picker.FileTypeChoices.Add(type, [ extension ]);
            PickFileResult resultPick = await picker.PickSaveFileAsync();
            return resultPick?.Path;
        }

        /// <summary>
        /// manage a FileOpenPicker to select a file to load from. returns the path of the selected file, null if no
        /// selection was made.
        /// </summary>
        public static async Task<string> OpenPickerUI(string button, List<string> extensions)
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = button,
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            foreach (string extension in extensions)
                picker.FileTypeFilter.Add(extension);
            PickFileResult resultPick = await picker.PickSingleFileAsync();
            return resultPick?.Path;
        }
    }
}
