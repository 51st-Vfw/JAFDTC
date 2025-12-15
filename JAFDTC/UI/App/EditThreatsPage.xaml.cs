// ********************************************************************************************************************
//
// EditThreatsPage.xaml.cs : ui c# threats editor
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.Units;
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
using System.Threading;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the threat ListView. this provides ui-friendly views of properties suitable for
    /// display in the ui via bindings.
    /// </summary>
    internal partial class ThreatListItem : BindableObject
    {
        public Threat Threat { get; set; }

        public bool IsOverride { get; set; }

        private string _radiusWEZUI;
        public string RadiusWEZUI
        {
            get => $"{Threat.RadiusWEZ:F2}";
            set
            {
                Threat.RadiusWEZ = double.Parse(value);
                SetProperty(ref _radiusWEZUI, value);
            }
        }

        public string CoalitionLabel => Threat.Coalition switch
        {
            CoalitionType.BLUE => "Blue",
            CoalitionType.RED => "Red",
            CoalitionType.NEUTRAL => "Neutral",
            _ => "Unknown"
        };

        public string CategoryLabel => Threat.Category switch
        {
            UnitCategoryType.GROUND => "Ground Unit",
            UnitCategoryType.NAVAL => "Naval Unit",
            _ => "Unknown"
        };

        public string TypeGlyph
            => Threat.Type switch
            {
                ThreatType.USER => "\xE718",
                _ => ""
            };

        public string ReplaceGlyph => (IsOverride) ? "\xE710" : "";

        public ThreatListItem(Threat threat) => (Threat) = (threat);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document.
    /// </summary>
    public sealed partial class EditThreatsPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ObservableCollection<ThreatListItem> CurThreatItems { get; set; }

        private bool IsRebuildPending { get; set; }

        private HashSet<string> SystemThreatDCSTypes { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditThreatsPage()
        {
            InitializeComponent();

            CurThreatItems = [ ];
            SystemThreatDCSTypes = [ ];
            foreach (Threat threat in ThreatDbase.Instance.Find(null, true))
            {
                ThreatListItem item = new(threat);
                if (threat.Type == ThreatType.DCS_CORE)
                    SystemThreatDCSTypes.Add(threat.TypeDCS);
                else if ((threat.Type == ThreatType.USER) && SystemThreatDCSTypes.Contains(threat.TypeDCS))
                    item.IsOverride = true;
                CurThreatItems.Add(item);
            }

            Threat tx = new();
            tx.Coalition = CoalitionType.BLUE;
            tx.Category = UnitCategoryType.GROUND;
            tx.Type = ThreatType.USER;
            tx.TypeDCS = "foo";
            tx.Name = "Test";
            tx.RadiusWEZ = 67.0;
            CurThreatItems.Add(new ThreatListItem(tx));

            Threat ty = new();
            ty.Coalition = CoalitionType.BLUE;
            ty.Category = UnitCategoryType.GROUND;
            ty.Type = ThreatType.USER;
            ty.TypeDCS = "Hawk ln";
            ty.Name = "Test Hawk";
            ty.RadiusWEZ = 67.0;
            ThreatListItem tli = new(ty);
            tli.IsOverride = true;
            CurThreatItems.Add(tli);

        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a list of threats matching the current filter configuration with a name that  containst the
        /// provided name fragment.
        /// </summary>
        private IReadOnlyList<Threat> GetThreatssMatchingFilter(string name = null)
        {
// TODO: pick up other criteria...
            ThreatDbaseQuery query = new(null, name);
            return ThreatDbase.Instance.Find(query, true);
        }

        /// <summary>
        /// rebuild the content of the threat list based on the current contents of the threat database along with
        /// the currently included types from the filter specification. name specifies the partial name to match,
        /// null if no match on name.
        /// </summary>
        private void RebuildThreatList(string name = null)
        {
// TODO: preserve selection, or nah?
            CurThreatItems.Clear();
            Dictionary<string, PointOfInterest> marks = [];
            foreach (Threat threat in GetThreatssMatchingFilter(name))
            {
                ThreatListItem item = new(threat);
                if ((threat.Type == ThreatType.USER) && SystemThreatDCSTypes.Contains(threat.TypeDCS))
                    item.IsOverride = true;
                CurThreatItems.Add(item);
            }
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the point of interest editor.
        /// if we have a source uid, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitles()
        {
#if NOPE
            uiPoITextBtnAdd.Text = (string.IsNullOrEmpty(EditPoI.SourceUID)) ? "Add" : "Update";
            uiPoITextBtnClear.Text = (EditPoI.IsDirty) ? "Reset" : "Clear";
#endif
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
#if NOPE
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            List<string> selectionByCampaign = CrackSelectedPoIsByCampaign(selectionByType);
            bool isCoreInSel = selectionByType.ContainsKey(PointOfInterestType.DCS_CORE);
            bool isUserInSel = selectionByType.ContainsKey(PointOfInterestType.USER);
            bool isCampaignInSel = selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN);
            bool isMultCampaignSel = (selectionByCampaign.Count > 1);

            bool isExportable = !isCoreInSel && ((isUserInSel && !isCampaignInSel) ||
                                                 (!isUserInSel && isCampaignInSel && !isMultCampaignSel));

            foreach (TextBox elem in _curPoIFieldValueMap.Values)
                Utilities.SetEnableState(elem, (uiPoIListView.SelectedItems.Count <= 1) && !isCoreInSel);

            bool isPoIValid = !string.IsNullOrEmpty(uiPoIValueName.Text) &&
                              !string.IsNullOrEmpty(uiPoIValueAlt.Text) &&
                              ((EditPoI.CurTheaters != null) && (EditPoI.CurTheaters.Count > 0)) &&
                              !EditPoI.HasErrors;

            Utilities.SetEnableState(uiPoIBtnAdd, EditPoI.IsDirty && isPoIValid);
            Utilities.SetEnableState(uiPoIBtnClear, !EditPoI.IsEmpty);
            Utilities.SetEnableState(uiPoIBtnCapture, curApp.IsDCSAvailable);

            bool isCampaigns = (PointOfInterestDbase.Instance.KnownCampaigns.Count > 0);

            Utilities.SetEnableState(uiBarBtnCopyUser, uiPoIListView.SelectedItems.Count == 1);
            Utilities.SetEnableState(uiBarBtnCopyCampaign, isCampaigns && (uiPoIListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarBtnDelete, !isCoreInSel);
            Utilities.SetEnableState(uiBarBtnExport, isExportable);

            Utilities.SetEnableState(uiBarBtnDeleteCamp, isCampaigns);

            List<string> errors = EditPoI.GetErrorsWithEmpty(EditPoI.IsEmpty);
            ValidateAllFields(_curPoIFieldValueMap, errors);
#endif
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
#if NOPE
                    RebuildActionButtonTitles();
                    RebuildEditorTheater();
#endif
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
        private void ThreatNameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = [];
                IReadOnlyList<Threat> threats = GetThreatssMatchingFilter(sender.Text);
                if (threats.Count == 0)
                    suitableItems.Add("No Matching Threats Found");
                else
                    foreach (Threat threat in threats)
                        suitableItems.Add(threat.Name);
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the threats listed in the threat list.
        /// </summary>
        private void ThreatNameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            RebuildThreatList(args.QueryText);
            RebuildInterfaceState();
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// filter command click: change the filter configuration via the filter specification dialog and update the
        /// displayed threats appropriately.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            GetPoIFilterDialog filterDialog = new(FilterTheater, FilterCampaign, FilterTags, FilterIncludeTypes)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Set a Filter for Points of Interest",
                PrimaryButtonText = "Set",
                SecondaryButtonText = "Clear Filters",
                CloseButtonText = "Cancel",
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                FilterTheater = filterDialog.Theater;
                FilterCampaign = filterDialog.Campaign;
                FilterTags = PointOfInterest.SanitizedTags(filterDialog.Tags);
                FilterIncludeTypes = filterDialog.IncludeTypes;
            }
            else if (result == ContentDialogResult.Secondary)
            {
                FilterTheater = "";
                FilterCampaign = "";
                FilterTags = "";
                FilterIncludeTypes = PointOfInterestTypeMask.ANY;
            }
            else
            {
                return;                                         // EXIT: cancelled, no change...
            }

            button.IsChecked = IsFiltered;

            Settings.LastPoIFilterTheater = FilterTheater;
            Settings.LastPoIFilterCampaign = FilterCampaign;
            Settings.LastPoIFilterTags = FilterTags;
            Settings.LastPoIFilterIncludeTypes = FilterIncludeTypes;

            uiPoIListView.SelectedItems.Clear();
            RebuildPoIList();
            RebuildInterfaceState();
#endif
        }

        /// <summary>
        /// edit command click: copy the details of the currnetly selected threat into the threat editor fields and
        /// rebuild the interface state to reflect the change. this should only be called on read-only system
        /// threats.
        /// </summary>
        private void CmdCopyUser_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            int index = _llFmtToIndexMap[LLDisplayFmt];
            foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                PoIDetails newPoI = new(item.PoI, LLDisplayFmt, index)
                {
                    Name = $"{item.PoI.Name} - User Copy",
                    SourceUID = null                                        // null creates new poi
                };
                if (CoreCommitEditChanges(newPoI, item.PoI.Theater, null, true) == null)
                {
                    PromptForNameCollision(newPoI.Name, item.PoI.Theater, null, newPoI.Tags);
                    break;
                }
            }
            RebuildPoIList();
            RebuildInterfaceState();
#endif
        }

        /// <summary>
        /// delete command click: remove the selected threats from the threat database. system threats are skipped.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                string message = (uiPoIListView.SelectedItems.Count > 1) ? "delete these points of interest?"
                                                                         : "delete this point of interest?";
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Point" + ((uiPoIListView.SelectedItems.Count > 1) ? "s" : "") + " of Interest?",
                    $"Are you sure you want to {message} This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    Dictionary<string, bool> campaignsModified = [];
                    Dictionary<string, bool> campaignsDeleted = [];
                    foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
                        if (item.PoI.Type != PointOfInterestType.DCS_CORE)
                        {
                            campaignsModified[(string.IsNullOrEmpty(item.PoI.Campaign)) ? "<u>" : item.PoI.Campaign] = true;
                            if (PointOfInterestDbase.Instance.CountPoIInCampaign(item.PoI.Campaign) == 1)
                            {
                                campaignsDeleted[item.PoI.Campaign] = true;
                                PointOfInterestDbase.Instance.DeleteCampaign(item.PoI.Campaign);
                            }
                            else
                            {
                                PointOfInterestDbase.Instance.RemovePointOfInterest(item.PoI, false);

                                VerbMirror?.MirrorVerbMarkerDeleted(this, new((MapMarkerInfo.MarkerType)item.PoI.Type,
                                                                              item.PoI.UniqueID));
                            }
                        }
                    foreach (string campaign in campaignsModified.Keys)
                        PointOfInterestDbase.Instance.Save((campaign != "<u>") ? campaign : null);

                    RebuildPoIList();
                    RebuildInterfaceState();

                    if (campaignsDeleted.Count > 0)
                    {
                        string msg;
                        List<string> campaigns = [.. campaignsDeleted.Keys];
                        if (campaigns.Count == 1)
                            msg = $"campaign {campaigns[0]}. This campaign has";
                        else if (campaigns.Count == 2)
                            msg = $"campaigns {campaigns[0]} and {campaigns[1]}. These campaigns have";
                        else
                            msg = $"campaigns " + string.Join(", ", campaigns.GetRange(0, campaigns.Count - 1)) +
                                  $", and {campaigns[^1]}. These campaigns have";

                        await Utilities.Message1BDialog(
                            Content.XamlRoot,
                            $"Deleted Empty Campaign" + ((campaignsDeleted.Count > 1) ? "s" : ""),
                            $"The delete removed all points of interest from the {msg} been deleted as well.");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// import command click: prompt the user for a file to import threats from and deserialize the contents of
        /// the file into the points of interest database. the database is saved following the import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            // TODO: could probably handle this without closing map window...
            MapWindow?.Close();

            bool? isSuccess = await ExchangePOIUIHelper.ImportFile(Content.XamlRoot, PointOfInterestDbase.Instance);
            if (isSuccess == true)
            {
                RebuildPoIList();
                RebuildInterfaceState();
            }
#endif
        }

        /// <summary>
        /// export command click: prompt the user for a file to export the selected threats to and serialize the
        /// selected threats to a file.
        /// 
        /// we assume contorls triggering CmdExport_Click are disabled when exports are not allowed to ensure
        /// that export files contain only user threats.
        /// </summary>
        private void CmdExport_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();

            if (selectionByType.TryGetValue(PointOfInterestType.USER, out List<PointOfInterest> userPoIs))
                ExchangePOIUIHelper.ExportFileForUser(Content.XamlRoot, userPoIs);
            else if (selectionByType.TryGetValue(PointOfInterestType.CAMPAIGN, out List<PointOfInterest> campaignPoIs))
                ExchangePOIUIHelper.ExportFileForCampaign(Content.XamlRoot, campaignPoIs);
#endif
        }

        // ---- poi list ----------------------------------------------------------------------------------------------

        /// <summary>
        /// threat list view right click: show context menu
        /// </summary>
        private void ThreatListView_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = sender as ListView;
            ThreatListItem threat = ((FrameworkElement)args.OriginalSource).DataContext as ThreatListItem;

            // check if the tapped item is selected. if the right tap occurs outside of the selection, change
            // up the selection to just be the tapped item.
            //
            int index = CurThreatItems.IndexOf(threat);
            bool isTappedItemSelected = false;
            foreach (ItemIndexRange range in listView.SelectedRanges)
                if ((index >= range.FirstIndex) && (index <= range.LastIndex))
                {
                    isTappedItemSelected = true;
                    break;
                }
            if (!isTappedItemSelected)
            {
                listView.SelectedIndex = CurThreatItems.IndexOf(threat);
#if NOPE
                RebuildInterfaceState();
#endif
            }

            // set up enables based on items selected. rely on the command handlers to properly handle situations
            // where there are a mix of types and behave correctly (e.g., not delete core items selected).
            //
#if NOPE
            Dictionary<ThreatType, List<Threat>> selectionByType = CrackSelectedPoIsByType();
            List<string> selectionByCampaign = CrackSelectedPoIsByCampaign(selectionByType);
            bool isCoreInSel = selectionByType.ContainsKey(Threat.DCS_CORE);
            bool isUserInSel = selectionByType.ContainsKey(Threat.USER);

            bool isExportable = !isCoreInSel && isUserInSel;

            bool isSelect = (uiThreatListView.SelectedItems.Count > 0);
            uiThreatListCtxMenuFlyout.Items[0].IsEnabled = isSelect;                                // copy to user
            uiThreatListCtxMenuFlyout.Items[2].IsEnabled = isExportable;                            // export
            uiThreatListCtxMenuFlyout.Items[4].IsEnabled = !isCoreInSel;                            // delete
#endif

            uiThreatListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition(listView));
        }

        /// <summary>
        /// threat list view selection changed: rebuild the interface state to reflect newly selected threat(s).
        /// </summary>
        private void ThreatListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
#if NOPE
            if (uiPoIListView.SelectedItems.Count != 1)
            {
                EditPoI.Reset();
                // TODO: this will clear map window selection when we have multiple things selected in the poi list.
                // TODO: this likely needs to change if multi-selection is ever supported in the map window.
                if (!IsVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new());
            }
            else if (uiPoIListView.SelectedItems.Count == 1)
            {
                PoIListItem item = uiPoIListView.SelectedItem as PoIListItem;
                int index = _llFmtToIndexMap[LLDisplayFmt];

                EditPoI.CurTheaters = PointOfInterest.TheatersForCoords(item.PoI.Latitude, item.PoI.Longitude);
                EditPoI.SourceUID = item.PoI.UniqueID;
                EditPoI.Name = item.PoI.Name;
                EditPoI.Tags = PointOfInterest.SanitizedTags(item.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(item.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(item.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = item.PoI.Elevation;

                if (!IsVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new((MapMarkerInfo.MarkerType)item.PoI.Type,
                                                                   item.PoI.UniqueID, -1));
            }
            RebuildInterfaceState();
#endif
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
            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, close any open map window.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            base.OnNavigatedFrom(args);
        }
    }
}
