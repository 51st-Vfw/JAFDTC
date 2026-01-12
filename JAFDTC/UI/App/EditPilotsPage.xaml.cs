// ********************************************************************************************************************
//
// EditPilotsPage.xaml.cs : ui c# pilot editor
//
// Copyright(C) 2026 ilominar/raven
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.Pilots;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the pilot ListView. this provides ui-friendly views of properties suitable for
    /// display in the ui via bindings.
    /// </summary>
    internal partial class PilotListItem : BindableObject
    {
        public Pilot Pilot { get; set; }

        public string AirframeUI => Globals.AirframeNames[Pilot.Airframe];
        public string BoardNumberUI => (string.IsNullOrEmpty(Pilot.BoardNumber)) ? "—" : Pilot.BoardNumber;
        public string AvionicsIDUI => (string.IsNullOrEmpty(Pilot.AvionicsID)) ? "—" : Pilot.AvionicsID;

        public string Glyph
            => ((Settings.Callsign.Equals(Pilot.Name, StringComparison.OrdinalIgnoreCase)) ? Glyphs.Pilot : Glyphs.None);

        public PilotListItem(Pilot pilot) => (Pilot) = (pilot);
    }

    // ================================================================================================================

    /// <summary>
    /// backing object for editing a pilot. this provides bindings for the text fields at the bottom of the pilot
    /// page that set name, airframe, and identifier information.
    /// </summary>
    internal partial class PilotDetails : BindableObject
    {
        public string SourceUID { get; set; }

        public AirframeTypes Airframe { get; set; }

        private int _airframeUI;
        public int AirframeUI
        {
            get => _airframeUI;
            set => SetProperty(ref _airframeUI, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _boardNumber;
        public string BoardNumber
        {
            get => _boardNumber;
            set => SetProperty(ref _boardNumber, value);
        }

        private string _avionicsID;
        public string AvionicsID
        {
            get => _avionicsID;
            set => SetProperty(ref _avionicsID, value);
        }

        public bool IsEmpty
            => (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(BoardNumber) && string.IsNullOrEmpty(AvionicsID));

        public bool IsDirty
        {
            get
            {
                Pilot pilot = PilotDbase.Instance.Find(SourceUID);
                if (pilot != null)
                    return pilot.Name != Name ||
                           pilot.Airframe != Airframe ||
                           pilot.BoardNumber != BoardNumber ||
                           pilot.AvionicsID != AvionicsID;
                else
                    return !IsEmpty;
            }
        }

        public PilotDetails() => (Airframe) = (AirframeTypes.UNKNOWN);

        public PilotDetails(Pilot pilot)
        {
            Airframe = pilot.Airframe;
            Name = pilot.Name;
            BoardNumber = pilot.BoardNumber;
            AvionicsID = pilot.AvionicsID;
        }

        public void Reset()
        {
            SourceUID = "";
            AirframeUI = 0;
            Name = "";
            BoardNumber = "";
            AvionicsID = "";
        }
    }

    // ================================================================================================================

    /// <summary>
    /// main page for the pilot editor. this provides the ui view of the pilot database in jafdtc and implements
    /// typical editor actions to create, modify, and delete pilots.
    /// </summary>
    public sealed partial class EditPilotsPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- internal properties

        private ObservableCollection<PilotListItem> CurPilotItems { get; set; }

        private PilotDetails EditPilot { get; set; }

        private PilotFilterSpec PilotFilter { get; set; }

        private bool IsRebuildPending { get; set; }

        // ---- constructed properties

        private bool IsFiltered => ((PilotFilter != null) && !PilotFilter.IsDefault);

        private bool IsEditorStateImpliesAdd
        {
            get
            {
                PilotDbaseQuery query = new()
                {
                    Airframes = [ GetAirframeFromComboItem() ],
                    ExactName = uiTextPilotName.Text.Trim()
                };
                Pilot pilot = PilotDbase.Instance.Find(EditPilot.SourceUID);
                return ((PilotDbase.Instance.Find(query).Count == 0) &&
                        ((pilot == null) ||
                         (pilot.Name != uiTextPilotName.Text.Trim()) ||
                         (pilot.Airframe != GetAirframeFromComboItem())));
            }
        }

        private bool IsEditorStateImpliesClear
        {
            get
            {
                Pilot pilot = PilotDbase.Instance.Find(EditPilot.SourceUID);
                return ((pilot == null) ||
                        ((pilot.Airframe == GetAirframeFromComboItem()) &&
                         (pilot.Name == uiTextPilotName.Text.Trim()) &&
                         (pilot.BoardNumber == uiTextPilotBoardNum.Text) &&
                         (pilot.AvionicsID == uiTextPilotAvionicsID.Text)));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditPilotsPage()
        {
            PilotFilter = new(Settings.LastPilotFilter);

            InitializeComponent();

            CurPilotItems = [ ];

            EditPilot = new PilotDetails();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private AirframeTypes GetAirframeFromComboItem()
        {
            TextBlock block = uiComboAirframe.SelectedItem as TextBlock;
            return (block != null) ? (AirframeTypes)(int.Parse(block.Tag as string)) : AirframeTypes.UNKNOWN;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private TextBlock GetComboItemForAirframe()
        {
            foreach (TextBlock block in uiComboAirframe.Items.Cast<TextBlock>())
                if ((AirframeTypes)(int.Parse(block.Tag as string)) == EditPilot.Airframe)
                    return block;
            return uiComboAirframe.Items[0] as TextBlock;
        }

        /// <summary>
        /// return a list of pilots matching the current filter configuration with a name that containst the provided
        /// name fragment.
        /// </summary>
        private IReadOnlyList<Pilot> GetPilotsMatchingFilter(string name = null)
        {
            AirframeTypes[] airframes = null;
            if (PilotFilter.Airframe != AirframeTypes.UNKNOWN)
            {
                airframes = [ PilotFilter.Airframe ];
            }
            PilotDbaseQuery query = new()
            {
                Airframes = airframes,
                Name = name,
                BoardNumber = null
            };
            return PilotDbase.Instance.Find(query, true);
        }


        /// <summary>
        /// rebuild the content of the pilot list based on the current contents of the pilot database along with
        /// the current filter setup.
        /// </summary>
        private void RebuildPilotList(string name = null)
        {
            CurPilotItems.Clear();
            foreach (Pilot pilot in GetPilotsMatchingFilter(name))
                CurPilotItems.Add(new PilotListItem(pilot));
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the pilot editor. if we have a
        /// source uid, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitles()
        {
            uiPilotTextBtnAdd.Text = (IsEditorStateImpliesAdd) ? "Add" : "Update";
            uiPilotTextBtnClear.Text = (IsEditorStateImpliesClear) ? "Clear" : "Reset";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isPilotValid = !string.IsNullOrEmpty(uiTextPilotName.Text.Trim()) && !EditPilot.HasErrors;

            Utilities.SetEnableState(uiPilotBtnAdd, EditPilot.IsDirty && isPilotValid);
            Utilities.SetEnableState(uiPilotBtnClear, !EditPilot.IsEmpty);

            Utilities.SetEnableState(uiBarBtnDelete, (uiPilotListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarBtnExport, true);
        }

        /// <summary>
        /// rebuild user interface state such as the enable state of the command bars.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    RebuildActionButtonTitles();
                    RebuildEnableState();
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// back button click: navigate back to the configuration list.
        /// </summary>
        private void HdrBtnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ---- name search box ---------------------------------------------------------------------------------------

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PilotNameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = [ ];
                IReadOnlyList<Pilot> pilots = GetPilotsMatchingFilter(sender.Text);
                if (pilots.Count == 0)
                    suitableItems.Add("No Matching Points of Interest Found");
                else
                    foreach (Pilot pilot in pilots)
                        suitableItems.Add(pilot.Name);
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pilots listed in the pilot list.
        /// </summary>
        private void PilotNameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            RebuildPilotList(args.QueryText);
            RebuildInterfaceState();
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// filter command click: change the filter configuration via the filter specification dialog and update the
        /// displayed pois appropriately.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            PilotFilterDialog filterDialog = new(PilotFilter)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Pilot Database Filter"
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                PilotFilter = new(filterDialog.Filter);
            else if (result == ContentDialogResult.Secondary)
                PilotFilter = new();
            else
                return;                                         // EXIT: cancelled, no change...

            button.IsChecked = IsFiltered;

            Settings.LastPilotFilter = PilotFilter;

            uiPilotListView.SelectedItems.Clear();
            RebuildPilotList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// delete command click: remove the selected points of interest from the points of interest database.
        /// dcs core pois are skipped.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            if (uiPilotListView.SelectedItems.Count > 0)
            {
                string message = (uiPilotListView.SelectedItems.Count > 1) ? "delete these pilots?"
                                                                           : "delete this pilot?";
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Pilot" + ((uiPilotListView.SelectedItems.Count > 1) ? "s" : "") + "?",
                    $"Are you sure you want to {message} This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    foreach (PilotListItem item in uiPilotListView.SelectedItems.Cast<PilotListItem>())
                        PilotDbase.Instance.RemovePilot(item.Pilot, false);
                    PilotDbase.Instance.Save();
                    RebuildPilotList();
                    RebuildInterfaceState();
                }
            }
        }

        /// <summary>
        /// import command click: prompt the user for a file to import pilots from and deserialize the contents
        /// of the file into the pilot database. the database is saved following the import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            bool? isSuccess = await ExchangePilotUIHelper.ImportFile(Content.XamlRoot);
            if (isSuccess == true)
            {
                RebuildPilotList();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// export command click: prompt the user for a file to export the selected pilots to and serialize the
        /// selected pilots to a file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            List<Pilot> pilots = [ ];
            if (uiPilotListView.SelectedItems.Count == 0)
                pilots = [.. PilotDbase.Instance.Find() ];
            else
                foreach (PilotListItem item in uiPilotListView.SelectedItems.Cast<PilotListItem>())
                    pilots.Add(item.Pilot);

            bool? isSuccess = await ExchangePilotUIHelper.ExportFile(Content.XamlRoot, pilots);
            if (isSuccess == true)
            {
                RebuildPilotList();
                RebuildInterfaceState();
            }
        }

        // ---- poi list ----------------------------------------------------------------------------------------------

        /// <summary>
        /// pilot list view right click: show context menu.
        /// </summary>
        private void PilotListView_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = sender as ListView;
            PilotListItem poi = ((FrameworkElement)args.OriginalSource).DataContext as PilotListItem;

            // check if the tapped item is selected. if the right tap occurs outside of the selection, change
            // up the selection to just be the tapped item.
            //
            int index = CurPilotItems.IndexOf(poi);
            bool isTappedItemSelected = false;
            foreach (ItemIndexRange range in listView.SelectedRanges)
                if ((index >= range.FirstIndex) && (index <= range.LastIndex))
                {
                    isTappedItemSelected = true;
                    break;
                }
            if (!isTappedItemSelected)
            {
                listView.SelectedIndex = CurPilotItems.IndexOf(poi);
                RebuildInterfaceState();
            }

            // set up enables based on items selected.
            //
            uiPilotListCtxMenuFlyout.Items[0].IsEnabled = true;             // export
            uiPilotListCtxMenuFlyout.Items[2].IsEnabled = true;             // delete

            uiPilotListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition(listView));
        }

        /// <summary>
        /// poi list view selection changed: rebuild the interface state to reflect the newly selected poi(s).
        /// </summary>
        private void PilotListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (uiPilotListView.SelectedItems.Count != 1)
            {
                EditPilot.Reset();
            }
            else if (uiPilotListView.SelectedItems.Count == 1)
            {
                IsRebuildPending = true;
                PilotListItem item = uiPilotListView.SelectedItem as PilotListItem;
                EditPilot.SourceUID = item.Pilot.UniqueID;
                EditPilot.Airframe = item.Pilot.Airframe;
                EditPilot.Name = item.Pilot.Name;
                EditPilot.BoardNumber = item.Pilot.BoardNumber;
                EditPilot.AvionicsID = item.Pilot.AvionicsID;
                uiComboAirframe.SelectedItem = GetComboItemForAirframe();
                IsRebuildPending = false;
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// pilot add/update button click: add a new pilot or update an existing pilot with the values from the
        /// pilot editor.
        /// </summary>
        private async void PilotBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            Pilot newPilot = new()
            {
                Airframe = EditPilot.Airframe,
                Name = EditPilot.Name,
                BoardNumber = EditPilot.BoardNumber,
                AvionicsID = EditPilot.AvionicsID
            };
            Pilot curPilot = PilotDbase.Instance.Find(newPilot.UniqueID);
            if (curPilot != null)
                PilotDbase.Instance.RemovePilot(curPilot, false);
            PilotDbase.Instance.AddPilot(newPilot, true);
            RebuildPilotList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// pilot clear button click: reset (if editor is dirty and there's a selected item) or reset (otherwise)
        /// the pilot editor.
        /// </summary>
        private void PilotBtnClear_Click(object sender, RoutedEventArgs args)
        {
            if (!IsEditorStateImpliesClear && (uiPilotListView.SelectedItem is PilotListItem item))
            {
                EditPilot.Airframe = item.Pilot.Airframe;
                EditPilot.Name = item.Pilot.Name;
                EditPilot.BoardNumber = item.Pilot.BoardNumber;
                EditPilot.AvionicsID = item.Pilot.AvionicsID;
            }
            else
            {
                uiPilotListView.SelectedItems.Clear();
                EditPilot.Reset();
            }
            RebuildInterfaceState();
        }

        // ---- airframe combo box changes ----------------------------------------------------------------------------

        /// <summary>
        /// combo selection changed: push change into pilot edit state.
        /// </summary>
        private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!IsRebuildPending && (uiComboAirframe.SelectedItem != null))
            {
                EditPilot.Airframe = GetAirframeFromComboItem();
                RebuildInterfaceState();
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// pilot editor value changed: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PilotTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// pilot editor value field lost focus: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PilotTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // HACK: 100% uncut cya. as the app is shutting down we can get lost focus events that may try to
            // HACK: operate on ui that has been torn down. in that case, return without doing anything.
            // HACK: this potentially prevents persisting changes made to the control prior to focus loss.
            //
            if ((Application.Current as JAFDTC.App).IsAppShuttingDown)
                return;

            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on file activations, import the pilot database via the exchange helper and update the interface.
        /// </summary>
        private async void Window_FileActivation(object sender, FileActivationEventArgs args)
        {
            bool? isSuccess = await ExchangePOIUIHelper.ImportFile(Content.XamlRoot,
                                                                   PointOfInterestDbase.Instance, args.Path) ?? false;
            if (isSuccess == true)
            {
                RebuildPilotList();
                RebuildInterfaceState();
            }
            args.IsReportSuccess = null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            EditPilot.ClearErrors();
            EditPilot.Reset();

            RebuildPilotList();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);

            (Application.Current as JAFDTC.App).Window.PilotDbFileActivation += Window_FileActivation;
        }

        /// <summary>
        /// on navigating from this page, unsubscribe from file activations.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            (Application.Current as JAFDTC.App).Window.PilotDbFileActivation -= Window_FileActivation;

            base.OnNavigatedFrom(args);
        }
    }
}
