// ********************************************************************************************************************
//
// AddNavpointsFromPOIsPage.xaml.cs : ui c# point of navpoint addition
//
// Copyright(C) 2025 ilominar/raven, fizzle
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
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class AddNavpointsFromPOIsPage : Page
    {
        /// <summary>
        /// return an object to use as the argument to the add POI navpoints editor. this object is passed in through the
        /// Parameter of a navigation operation.
        /// </summary>
        public class NavigationArg
        {
            public Page ParentEditor { get; private set; }
            public IConfiguration Config { get; private set; }
            public IEditNavpointListPageHelper PageHelper { get; private set; }
            public NavigationArg(Page parentEditor, IConfiguration config, IEditNavpointListPageHelper helper) => 
                (ParentEditor, Config, PageHelper) = (parentEditor, config, helper);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ObservableCollection<App.PoIListItem> CurPoIItems { get; set; }

        private List<PointOfInterest> CurPoI { get; set; }

        private LLFormat LLDisplayFmt { get; set; }

        private POIFilterSpec POIFilter { get; set; }

        private bool IsFiltered => !POIFilter.IsDefault;

        private NavigationArg NavArgs;

        // read-only properties

        private readonly Dictionary<LLFormat, int> _llFmtToIndexMap;
        private readonly Dictionary<string, LLFormat> _llFmtTextToFmtMap;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AddNavpointsFromPOIsPage()
        {
            InitializeComponent();

            POIFilter = new(Settings.LastPOIFilter);

            LLDisplayFmt = LLFormat.DDM_P3ZF;
            CurPoIItems = [ ];

            // NOTE: these need to be kept in sync with PoIDetails and the xaml.
            //
            _llFmtToIndexMap = new Dictionary<LLFormat, int>()
            {
                [LLFormat.DDU] = 0,
                [LLFormat.DMS] = 1,
                [LLFormat.DDM_P3ZF] = 2
            };
            _llFmtTextToFmtMap = new Dictionary<string, LLFormat>()
            {
                ["Decimal Degrees"] = LLFormat.DDU,
                ["Degrees, Minutes, Seconds"] = LLFormat.DMS,
                ["Degrees, Decimal Minutes"] = LLFormat.DDM_P3ZF,
            };

            uiBarBtnFilter.IsChecked = IsFiltered;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a list of points of interest matching the current filter configuration with a name that
        /// containst the provided name fragment.
        /// </summary>
        private List<PointOfInterest> GetPoIsMatchingFilter(string name = null)
        {
            PointOfInterestDbQuery query = new(POIFilter.IncludeTypes, POIFilter.Theater, POIFilter.Campaign,
                                               name, POIFilter.Tags, PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            return PointOfInterestDbase.Instance.Find(query, true);
        }

        /// <summary>
        /// update the coordinate format used in the poi lat/lon specification. there are per-format fields to
        /// deal with the apparent inability to change masks dynamically. show the field for the current format,
        /// hide all others. updates LLDisplayFmt.
        /// </summary>
        private void ChangeCoordFormat(LLFormat fmt)
        {
            LLDisplayFmt = fmt;
        }

        /// <summary>
        /// rebuild the content of the point of interest list based on the current contents of the poi database
        /// along with the currently selected theater, tags, and included types from the filter specification.
        /// name specifies the partial name to match, null if no match on name.
        /// </summary>
        private void RebuildPoIList(string name = null)
        {
            CurPoI = GetPoIsMatchingFilter(name);
            CurPoIItems.Clear();
            foreach (PointOfInterest poi in CurPoI)
                CurPoIItems.Add(new App.PoIListItem(poi, LLDisplayFmt));
            UpdateAcceptButtons();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        private void PoIListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAcceptButtons();
        }

        private void UpdateAcceptButtons()
        {
            ToolTip tip = new();
            int remainingCount = NavArgs.PageHelper.NavptSystem(NavArgs.Config).NavptAvailableCount();
            string navptName = NavArgs.PageHelper.SystemInfo.NavptName.ToLower();
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                uiAcceptBtnOK.Content = "Append Selected";
                if (uiPoIListView.SelectedItems.Count > remainingCount)
                {
                    uiAcceptBtnOK.IsEnabled = false;
                    tip.Content = $"The airframe has space for only {remainingCount} more {navptName}s";
                }
                else
                {
                    uiAcceptBtnOK.IsEnabled = true;
                    tip.Content = $"Append {uiPoIListView.SelectedItems.Count} selected POIs as {navptName}s";
                }
            }
            else
            {
                uiAcceptBtnOK.Content = "Append All";
                if (CurPoIItems.Count > remainingCount)
                {
                    uiAcceptBtnOK.IsEnabled = false;
                    tip.Content = $"The airframe has space for only {remainingCount} more {navptName}s";
                }
                else
                {
                    uiAcceptBtnOK.IsEnabled = true;
                    tip.Content = $"Append {CurPoIItems.Count} listed POIs as {navptName}s";
                }
            }
            ToolTipService.SetToolTip(uiAcceptBtnOK, tip);
        }

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// accept click: if page has errors and we aren't linked, update the configuration state before returning to
        /// the navpoint list.
        /// </summary>
        private async void AcceptBtnOk_Click(object sender, RoutedEventArgs args)
        {
            string navptName = NavArgs.PageHelper.SystemInfo.NavptName.ToLower();
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Append Selected?",
                    $"Are you sure you want to append {uiPoIListView.SelectedItems.Count} selected POIs as {navptName}s?",
                    $"Append Selected"
                );
                if (result == ContentDialogResult.Primary)
                {
                    List<PointOfInterest> pois = new(uiPoIListView.SelectedItems.Count);
                    foreach (App.PoIListItem item in uiPoIListView.SelectedItems.Cast<App.PoIListItem>())
                        pois.Add(item.PoI);
                    NavArgs.PageHelper.AddNavpointsFromPOIs(pois, NavArgs.Config);
                    Frame.GoBack();
                }
            }
            else
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Append All?",
                    $"Are you sure you want to append all {CurPoIItems.Count} listed POIs as {navptName}s?",
                    $"Append All"
                );
                if (result == ContentDialogResult.Primary)
                {
                    List<PointOfInterest> pois = new(CurPoIItems.Count);
                    foreach (App.PoIListItem item in CurPoIItems)
                        pois.Add(item.PoI);
                    NavArgs.PageHelper.AddNavpointsFromPOIs(pois, NavArgs.Config);
                    Frame.GoBack();
                }
            }
        }

        /// <summary>
        /// accept cancel click: return to the navpoint list without making any changes to the navpoint.
        /// </summary>
        private void AcceptBtnCancel_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ---- name search box ---------------------------------------------------------------------------------------

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PoINameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = [ ];
                List<PointOfInterest> pois = GetPoIsMatchingFilter(sender.Text);
                if (pois.Count == 0)
                    suitableItems.Add("No Matching Points of Interest Found");
                else
                    foreach (PointOfInterest poi in pois)
                        suitableItems.Add(poi.Name);
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pois listed in the poi list.
        /// </summary>
        private void PoINameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            RebuildPoIList(args.QueryText);
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// filter command click: setup the filter setup.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            GetPoIFilterDialog filterDialog = new(POIFilter)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Set a Filter for Points of Interest"
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                POIFilter = new(filterDialog.Filter);
            else if (result == ContentDialogResult.Secondary)
                POIFilter = new();
            else
                return;                                         // EXIT: cancelled, no change...

            button.IsChecked = IsFiltered;

            Settings.LastPOIFilter = POIFilter;

            uiPoIListView.SelectedItems.Clear();
            RebuildPoIList();
        }

        // ---- coordinate setup --------------------------------------------------------------------------------------

        /// <summary>
        /// select coordinate format click: present the user with a list dialog to select the display format for poi
        /// coordinates and update the ui to reflect the new choice.
        /// </summary>
        private async void CmdCoords_Click(object sender, RoutedEventArgs args)
        {
            List<string> items = [.. _llFmtTextToFmtMap.Keys ];
            GetListDialog coordList = new(items, null, 0, _llFmtToIndexMap[LLDisplayFmt])
            {
                XamlRoot = Content.XamlRoot,
                Title = "Select a Coordinate Format",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await coordList.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                Settings.LastPOICoordFmtSelection = _llFmtTextToFmtMap[coordList.SelectedItem];
                ChangeCoordFormat(_llFmtTextToFmtMap[coordList.SelectedItem]);
                RebuildPoIList();
            }
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
            NavArgs = (NavigationArg)args.Parameter;

            ChangeCoordFormat(Settings.LastPOICoordFmtSelection);
            RebuildPoIList();

            base.OnNavigatedTo(args);
        }
    }
}
