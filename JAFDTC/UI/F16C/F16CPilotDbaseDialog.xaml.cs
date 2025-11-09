// ********************************************************************************************************************
//
// F16CPilotDbaseDialog.xaml.cs -- ui c# for f-16c pilot database dialog
//
// Copyright(C) 2023 ilominar/raven
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

using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CPilotDbaseDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- regular expressions

        [GeneratedRegex(@"^[0-7]{5}$")]
        private static partial Regex TNDLRegex();

        // ---- properties

        private ObservableCollection<ViperDriver> _pilots;
        public ObservableCollection<ViperDriver> Pilots
        {
            get => _pilots;
            set
            {
                if (_pilots != value)
                {
                    _pilots = value;
                    SortPilots();
                }
            }
        }

        public string ErrorOperation { get; set; }

        public string ErrorMessage { get; set; }

        // ---- TODO

        public List<ViperDriver> SelectedDrivers => [.. uiPDbListView.SelectedItems.Cast<ViperDriver>() ];

        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CPilotDbaseDialog(XamlRoot root, List<ViperDriver> pilots)
        {
            XamlRoot = root;

            InitializeComponent();

            Pilots = new ObservableCollection<ViperDriver>(pilots);

            Opened += (s, e) => { ErrorOperation = null; ErrorMessage = null; };

            _defaultBorderBrush = uiPDbValueCallsign.BorderBrush;
            _defaultBkgndBrush = uiPDbValueCallsign.Background;

            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// sorts the list of pilots by name. sort occurs in-place to avoid messing up bindings.
        /// </summary>
        private void SortPilots()
        {
            if ((Pilots != null) && (Pilots.Count > 0))
            {
                List<ViperDriver> sortableList = [.. Pilots ];
                sortableList.Sort((a, b) => a.Name.CompareTo(b.Name));
                for (int i = 0; i < sortableList.Count; i++)
                    Pilots.Move(Pilots.IndexOf(sortableList[i]), i);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set the border brush and background for a TextBox based on validity. valid fields use the defaults, invalid
        /// fields use ErrorFieldBorderBrush from the resources.
        /// </summary>
        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        /// <summary>
        /// rebuild the pilot list interface state based on the current configuration. highlights the callsign and
        /// tndl fields based on their validity.
        /// </summary>
        private void RebuildInterfaceState()
        {
            Utilities.SetEnableState(uiPDbBtnExport, true);
            Utilities.SetEnableState(uiPDbBtnImport, true);
            Utilities.SetEnableState(uiPDbBtnDelete, uiPDbListView.SelectedItems.Count > 0);

            bool isCallsignValid = uiPDbValueCallsign.Text.Length > 0;
            foreach (ViperDriver driver in Pilots)
            {
                if (uiPDbValueCallsign.Text.Equals(driver.Name, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    isCallsignValid = false;
                    break;
                }
            }
            SetFieldValidState(uiPDbValueCallsign, isCallsignValid || string.IsNullOrEmpty(uiPDbValueCallsign.Text));

            bool isTNDLValid = TNDLRegex().IsMatch(uiPDbValueTNDL.Text);
            SetFieldValidState(uiPDbValueTNDL, isTNDLValid || string.IsNullOrEmpty(uiPDbValueTNDL.Text));

            Utilities.SetEnableState(uiPDbBtnAdd, isCallsignValid && isTNDLValid);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- new pilot controls ------------------------------------------------------------------------------------

        /// <summary>
        /// on changes to the callsign/tndl fields, rebuild the interface state to reflect the current state based on
        /// user input.
        /// </summary>
        private void PDbValue_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on add button clicks, add a new entry to the list with the callsign and tndl from the dialog. re-sort
        /// the list.
        /// </summary>
        private void PDbBtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ViperDriver newDriver = new()
            {
                Name = uiPDbValueCallsign.Text,
                TNDL = uiPDbValueTNDL.Text
            };
            Pilots.Add(newDriver);
            SortPilots();

            uiPDbValueCallsign.Text = "";
            uiPDbValueTNDL.Text = "";
        }

        // ---- pilot list --------------------------------------------------------------------------------------------

        /// <summary>
        /// on changes to the selection, rebuild the interface state to reflect the current state based on user input.
        /// </summary>
        private void PDbListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // ---- pilot list commands -----------------------------------------------------------------------------------

        /// <summary>
        /// on confirm flyout for delete button clicks, delete the selected pilots from the list.
        /// </summary>
        private void PDbBtnDeleteFlyout_Click(object sender, RoutedEventArgs e)
        {
            foreach (ViperDriver driver in uiPDbListView.SelectedItems.Cast<ViperDriver>())
                Pilots.Remove(driver);
        }

        /// <summary>
        /// on confirm flyong for import button clicks, note an import was requested and dismiss the dialog. if the
        /// import fails, hide the dialog and set the error information.
        /// </summary>
        private async void PDbBtnFlyoutImport_Click(object sender, RoutedEventArgs e)
        {
            // ---- pick file

            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Import Viper Pilots",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".jafdtc_db");

            PickFileResult resultPick = await picker.PickSingleFileAsync();
            try
            {
                // ---- do the import

                if ((resultPick != null) && (Path.GetExtension(resultPick.Path.ToLower()) == ".jafdtc_db"))
                {
                    List<ViperDriver> fileDrivers = FileManager.LoadSharableDbase<ViperDriver>(resultPick.Path);
                    if (fileDrivers != null)
                    {
                        Pilots.Clear();
                        foreach (ViperDriver driver in fileDrivers)
                            Pilots.Add(driver);
                        SortPilots();
                    }
                    else
                    {
                        throw new Exception("LoadSharableDbase fails");
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CPilotDbaseDialog:PDbBtnFlyoutImport_Click exception {ex}");
                ErrorOperation = "Import";
                ErrorMessage = $"Unable to import the pilots from the database file at:\n\n{resultPick.Path}";
                Hide();
            }
        }

        /// <summary>
        /// on export button clicks, open a file picker and export pilots to the specified file. if the export
        /// fails, hide the dialog and set the error information.
        /// </summary>
        private async void PDbBtnExport_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Export Viper Pilots",
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = "Viper Drivers"
            };
            picker.FileTypeChoices.Add("JAFDTC Db", [".jafdtc_db"]);

            PickFileResult resultPick = await picker.PickSaveFileAsync();
            try
            {
                if (resultPick != null)
                    FileManager.SaveSharableDatabase(resultPick.Path, [.. Pilots ]);
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CPilotDbaseDialog:PDbBtnExport_Click exception {ex}");
                ErrorOperation = "Export";
                ErrorMessage = $"Unable to export the pilots to the database file at:\n\n{resultPick.Path}";
                Hide();
            }
        }
    }
}
