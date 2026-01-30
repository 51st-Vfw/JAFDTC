// ********************************************************************************************************************
//
// MapSettingsDialog.xaml.cs -- ui c# for map settings dialog
//
// Copyright(C) 2025-2026 ilominar/raven
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

using JAFDTC.Models.CoreApp;
using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// ContentDialog to allow the user to specify the settings for a map window. these settings control things like
    /// auto open of the window, tile cache usage, etc.
    /// </summary>
    public sealed partial class MapSettingsDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapSettingsData Settings => new(uiSetCkbxEnableCache.IsChecked ?? false);

        public bool IsTileCacheEnabled => uiSetCkbxEnableCache.IsChecked ?? false;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapSettingsDialog(MapSettingsData settings, string dbasePath, string dbaseSize)
        {
            InitializeComponent();

            uiTxtCacheInfo.Text = $"The map tile cache currently uses {dbaseSize} in the directory:";
            uiTxtCachePath.Text = dbasePath;

            uiSetCkbxEnableCache.IsChecked = settings.IsTileCacheEnabled;
        }
    }
}
