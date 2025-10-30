// ********************************************************************************************************************
//
// MapSettingsDialog.xaml.cs -- ui c# for map settings dialog
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
using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapSettingsDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public bool IsAutoOpen => uiSetCkbxAutoOpen.IsChecked ?? false;

        public bool IsTileCacheEnabled => uiSetCkbxEnableCache.IsChecked ?? false;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapSettingsDialog(string dbasePath, string dbaseSize, bool isAutoOpen, bool isTileCacheEnabled)
        {
            InitializeComponent();

            uiTxtCacheInfo.Text = $"The map tile cache currently uses {dbaseSize} in the directory:";
            uiTxtCachePath.Text = dbasePath;

            uiSetCkbxAutoOpen.IsChecked = isAutoOpen;
            uiSetCkbxEnableCache.IsChecked = isTileCacheEnabled;
        }
    }
}
