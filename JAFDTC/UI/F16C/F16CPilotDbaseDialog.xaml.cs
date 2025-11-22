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

using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// ContentDialog for the viper pilot database editor. presents a ui to edit the pilot database in a dialog. as
    /// we cannot nest ContentDialog, the dialog can request the caller to display a status dialog on completion
    /// of this dialog through the StatusTitle and StatusMessage properties.
    /// </summary>
    public sealed partial class F16CPilotDbaseDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

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

        public string StatusTitle { get; set; }

        public string StatusMessage { get; set; }

        public List<ViperDriver> SelectedDrivers => [.. uiPDbListView.SelectedItems.Cast<ViperDriver>() ];

        // ---- private properties

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

            Opened += F16CPilotDbaseDialog_Opened;
            Closed += F16CPilotDbaseDialog_Closed;

            Pilots = new ObservableCollection<ViperDriver>(pilots);

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
        /// TODO: document
        /// </summary>
        private void RebuildPilotDbase()
        {
            Pilots.Clear();
            foreach (ViperDriver driver in F16CPilotsDbase.LoadDbase())
                Pilots.Add(driver);
            SortPilots();
            RebuildInterfaceState();
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

            bool isTNDLValid = F16CConfiguration.RegexTNDL().IsMatch(uiPDbValueTNDL.Text);
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
        /// on confirm flyover for import button clicks, attempt to import the database from a user-selected dbase
        /// file. if the import fails, hide the dialog and set the error information in the status properties.
        /// </summary>
        private async void PDbBtnFlyoutImport_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: the interaction model is a bit wonky here as we can't nest dialogs

            bool? isSuccess = await ExchangeViperPilotUIHelper.ImportFile(null, null);
            if (isSuccess == true)
            {
                RebuildPilotDbase();
                string count = $"{Pilots.Count}" + ((Pilots.Count == 1) ? $" pilot" : $" pilots");
                StatusTitle = "Import Successful";
                StatusMessage = $"Imported {count} from the specified database file.";
                Hide();
            }
            else if (isSuccess == false)
            {
                StatusTitle = "Import Failed";
                StatusMessage = "Unable to import the pilots from the specified database file.";
                Hide();
            }
        }

        /// <summary>
        /// on export button clicks, open a file picker and export pilots to the specified file. if the export
        /// fails, hide the dialog and set the error information in the status properties.
        /// </summary>
        private async void PDbBtnExport_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: the interaction model is a bit wonky here as we can't nest dialogs

            bool? isSuccess = await ExchangeViperPilotUIHelper.ExportFile(null, [.. Pilots ], null);
            if (isSuccess == true)
            {
                string count = $"{Pilots.Count}" + ((Pilots.Count == 1) ? $" pilot" : $" pilots");
                StatusTitle = "Export Successful";
                StatusMessage = $"Exported {count} to the specified database file.";
                Hide();
            }
            else if (isSuccess == false)
            {
                StatusTitle = "Export Failed";
                StatusMessage = "Unable to export the pilots to the specified database file.";
                Hide();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on dialog window opened, add the activation handler to the main window's list of activation handlers and
        /// clear the status properties.
        /// </summary>
        private void F16CPilotDbaseDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            (Application.Current as JAFDTC.App).Window.ViperDbFileActivation += Window_FileActivation;
            StatusTitle = null;
            StatusMessage = null;
        }

        /// <summary>
        /// on dialog window closed, remove the activation handler to the main window's list of activation handlers.
        /// </summary>
        private void F16CPilotDbaseDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            (Application.Current as JAFDTC.App).Window.ViperDbFileActivation -= Window_FileActivation;
        }

        /// <summary>
        /// on file activations, import the pilot database via the exchange helper and update the interface.
        /// we will hide the dialog to give a chance for a status dialog to be presented.
        /// </summary>
        private async void Window_FileActivation(object sender, FileActivationEventArgs args)
        {
            bool? isSuccess = await ExchangeViperPilotUIHelper.ImportFile(null, args.Path) ?? false;
            if (isSuccess == true)
            {
                RebuildPilotDbase();
                string count = $"{Pilots.Count}" + ((Pilots.Count == 1) ? $" pilot" : $" pilots");
                StatusTitle = "Import Successful";
                StatusMessage = $"Imported {count} from a file activation for the" +
                                $" database file:\n\n{args.Path}";
                Hide();
            }
            else if (isSuccess == false)
            {
                StatusTitle = "Import Failed";
                StatusMessage = $"Unable to import pilots for a file activation from the" +
                                $" database file:\n\n{args.Path}";
                Hide();
            }
            args.IsReportSuccess = null;
        }
    }
}
