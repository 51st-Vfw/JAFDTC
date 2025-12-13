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

        // ---- public properties, computed

        public CoalitionType FriendlyCoalition
        {
            get => uiComboCoalition.SelectedIndex switch { 0 => CoalitionType.BLUE, _ => CoalitionType.RED };
        }

        public bool IsEnemyOnly { get => (uiCkbxEnemyOnly.IsChecked == true); }

        public bool IsSummaryOnly { get => (uiCkbxIsSummaryOnly.IsChecked == true); }

        public bool IsAliveOnly { get => (uiCkbxIsAliveOnly.IsChecked == true); }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportParamsThreatDialog()
        {
            InitializeComponent();

            uiComboCoalition.SelectedIndex = 0;
            uiCkbxEnemyOnly.IsChecked = true;
            uiCkbxIsSummaryOnly.IsChecked = false;
            uiCkbxIsAliveOnly.IsChecked = true;
        }
    }
}