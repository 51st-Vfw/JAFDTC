// ********************************************************************************************************************
//
// ImportParamsNavptDialog.xaml.cs -- ui c# for dialog to grab navpoint import parameters
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
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// dialog to set import parameters for navigation point imports including flight (via combobox) and other
    /// parameters.
    /// </summary>
    public sealed partial class ImportParamsNavptDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public List<string> Flights { get; private set; }

        // ---- public properties, computed

        public string SelectedFlight { get => (Flights != null) ? (string)uiComboFlights.SelectedItem : null; }

        public bool IsUsingTOS => (bool)uiCkbxTOS.IsChecked;

        public bool IsIncludingFirst => (bool)uiCkbxInitialNavpt.IsChecked;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportParamsNavptDialog(string what, List<string> flights = null)
        {
            InitializeComponent();

            Flights = flights ?? [ ];

            uiTxtPrompt.Text = $"Use the {what.ToLower()}s from the selected flight to replace current" +
                               $" {what.ToLower()}s or append to the end of the current {what.ToLower()} list.";
            uiTxtCkbxInitialNavpt.Text = $"Include first {what.ToLower()} (initial flight position) in the imported route";
            uiTxtCkbxTOS.Text = $"Use time on {what.ToLower()} values from import file when available";
            uiComboFlights.SelectedIndex = (Flights.Count > 0) ? 0 : -1;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interaction
        //
        // ------------------------------------------------------------------------------------------------------------

        private void ComboItems_SelectionChanged(object sender, RoutedEventArgs args)
        {
            IsPrimaryButtonEnabled = ((Flights.Count > 0) && (uiComboFlights.SelectedIndex >= 0));
            IsSecondaryButtonEnabled = ((Flights.Count > 0) && (uiComboFlights.SelectedIndex >= 0));
        }
    }
}
