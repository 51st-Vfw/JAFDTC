// ********************************************************************************************************************
//
// ImportParamsThreatDialog.xaml.cs -- ui c# for dialog to grab import parameters for threats
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

using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// dialog to set import parameters for threat imports.
    /// </summary>
    public sealed partial class ImportParamsThreatDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapImportSpec Spec => new(Path,
                                         uiComboCoalition.SelectedIndex switch { 1 => CoalitionType.RED,
                                                                                 _ => CoalitionType.BLUE },
                                         (uiCkbxEnemyOnly.IsChecked == true),
                                         (uiCkbxIsSummaryOnly.IsChecked == true),
                                         (uiCkbxIsAliveOnly.IsChecked == true));

        private string Path { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportParamsThreatDialog(MapImportSpec spec = null)
        {
            spec ??= new();

            InitializeComponent();

            Path = spec.Path;
            uiComboCoalition.SelectedIndex = spec.FriendlyCoalition switch
            {
                CoalitionType.RED => 1,
                _ => 0
            };
            uiCkbxEnemyOnly.IsChecked = spec.IsEnemyOnly;
            uiCkbxIsSummaryOnly.IsChecked = spec.IsSummaryOnly;
            uiCkbxIsAliveOnly.IsChecked = spec.IsAliveOnly;
        }
    }
}