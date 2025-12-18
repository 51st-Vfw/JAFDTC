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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
    /// backing object for editing a threat. this provides bindings for the text fields at the bottom of the threat
    /// page that set coalition, type information.
    /// </summary>
    internal partial class ThreatDetails : BindableObject
    {
        public int Category { get; set; }                       // unit category (int encoded UnitCategoryType)

        public int Coalition { get; set; }                      // primary unit coalition (int encoded CoalitionType)

        public string TypeDCS { get; set; }                     // dcs .miz unit "type" value for threat

        public string Name { get; set; }                        // display name for threat

        public string RadiusWEZ { get; set; }                   // radius of wez (nm, 0 => "point" threat)

#if NOPE
        public string SourceUID { get; set; }

        public int CurIndexLL { get; set; }

        public List<string> CurTheaters { get; set; }

        // HACK: we will use per-format PoILL instances to avoid binding multiple controls to the same property
        // HACK: (which doesn't seem to work well). should be a way to dynamically bind/unbind properties that we
        // HACK: could use to avoid this, but...
        //
        public PoILL[] LL { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _tags;
        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

#endif

        public bool IsEmpty
            => true;

        public bool IsDirty
        {
            get
            {
#if NOPE
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(SourceUID);
                if (poi != null)
                    return poi.Name != Name ||
                           poi.Tags != Tags ||
                           poi.Elevation != Alt ||
                           poi.Latitude != LL[CurIndexLL].Lat ||
                           poi.Longitude != LL[CurIndexLL].Lon;
                else
                    return !IsEmpty;
#endif
                return true;
            }
        }
#if NOPE
        // NOTE: format order of PoILL must be kept in sync with EditPointsOfInterestPage and xaml.
        //
        public PoIDetails()
            => (CurIndexLL, LL) = (0, [new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF)]);

        public PoIDetails(PointOfInterest poi, LLFormat llDisplayFmt, int llIndex)
        {
            CurIndexLL = 0;
            LL = [new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF)];
            SourceUID = null;
            Name = poi.Name;
            Tags = PointOfInterest.SanitizedTags(poi.Tags);
            LL[llIndex].LatUI = Coord.ConvertFromLatDD(poi.Latitude, llDisplayFmt);
            LL[llIndex].LonUI = Coord.ConvertFromLonDD(poi.Longitude, llDisplayFmt);
            Alt = poi.Elevation;
        }

        public List<string> GetErrorsWithEmpty(bool isEmptyOK)
        {
            List<string> errors = LL[CurIndexLL].GetErrorsWithEmpty(isEmptyOK);
            if (!IsIntegerFieldValid(Alt, -1500, 80000, isEmptyOK))
                errors.Add("Alt");
            return errors;
        }
#endif

        public void Reset()
        {
            //Category = (int)UnitCategoryType.GROUND;
            Coalition = (int)CoalitionType.BLUE;
            TypeDCS = "";
            Name = "";
            RadiusWEZ = "";
        }
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

        private ThreatDetails EditThreat { get; set; }

        private bool IsRebuildPending { get; set; }

        private ThreatFilterSpec FilterThreat { get; set; }

        private HashSet<string> SystemThreatDCSTypes { get; set; }

        // ---- constructed properties

        private bool IsFiltered => ((FilterThreat != null) && !FilterThreat.IsDefault);


        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curThreatFieldValueMap;
        private readonly List<TextBox> _threatFieldValues;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

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

            EditThreat = new ThreatDetails();
            EditThreat.ErrorsChanged += ThreatField_DataValidationError;

// TODO: restore filter from persisted state
            FilterThreat = new();

            _curThreatFieldValueMap = new Dictionary<string, TextBox>()
            {
#if NOPE
                ["LatUI"] = uiPoIValueLatDDM,
                ["LonUI"] = uiPoIValueLonDDM,
                ["Alt"] = uiPoIValueAlt,
#endif
            };
            _threatFieldValues =
            [
                // uiPoIValueLatDD, uiPoIValueLatDDM, uiPoIValueLatDMS, uiPoIValueLonDD, uiPoIValueLonDDM, uiPoIValueLonDMS
            ];
            _defaultBorderBrush = uiTextType.BorderBrush;
            _defaultBkgndBrush = uiTextType.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
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

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = [];
            foreach (string error in errors)
                map[error] = true;
            foreach (KeyValuePair<string, TextBox> kvp in fields)
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key) || EditThreat.IsEmpty);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ThreatField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_curThreatFieldValueMap, EditThreat.GetErrors(null));
            }
            else
            {
                bool isValid = ((List<string>)EditThreat.GetErrors(args.PropertyName)).Count == 0;
                if (_curThreatFieldValueMap.TryGetValue(args.PropertyName, out TextBox value))
                    SetFieldValidState(value, isValid || EditThreat.IsEmpty);
            }
            RebuildInterfaceState();
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
            UnitCategoryType[] categories = [ UnitCategoryType.GROUND, UnitCategoryType.NAVAL ];
            if (FilterThreat.Category == UnitCategoryType.GROUND)
                categories = [ UnitCategoryType.GROUND ];
            else if (FilterThreat.Category == UnitCategoryType.NAVAL)
                categories = [ UnitCategoryType.NAVAL ];

            CoalitionType[] coalitions = [ CoalitionType.BLUE, CoalitionType.RED ];
            if (FilterThreat.Coalition == CoalitionType.BLUE)
                coalitions = [ CoalitionType.BLUE ];
            else if (FilterThreat.Coalition == CoalitionType.RED)
                coalitions = [ CoalitionType.RED ];

            ThreatType[] threatTypes = [ ];
            if (FilterThreat.ShowThreatsDCS)
                threatTypes = [.. threatTypes, ThreatType.DCS_CORE ];
            if (FilterThreat.ShowThreatsUser)
                threatTypes = [.. threatTypes, ThreatType.USER];

            ThreatDbaseQuery query = new(null, name, threatTypes, categories, coalitions, false);
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
            foreach (Threat threat in GetThreatssMatchingFilter(name))
            {
                ThreatListItem item = new(threat);
                if ((threat.Type == ThreatType.USER) && SystemThreatDCSTypes.Contains(threat.TypeDCS))
                    item.IsOverride = true;
                CurThreatItems.Add(item);
            }
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the threat editor. if we have a
        /// source uid, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitles()
        {
#if NOPE
            uiThreatTextBtnAdd.Text = (string.IsNullOrEmpty(EditThreat.SourceUID)) ? "Add" : "Update";
#endif
            uiThreatTextBtnClear.Text = (EditThreat.IsDirty) ? "Reset" : "Clear";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isCoreInSel = false;
            bool isUserInSel = false;
            foreach (ThreatListItem item in uiThreatListView.SelectedItems.Cast<ThreatListItem>())
            {
                isCoreInSel = (isCoreInSel || (item.Threat.Type == ThreatType.DCS_CORE));
                isUserInSel = (isUserInSel || (item.Threat.Type == ThreatType.USER));
            }

#if NOPE
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            List<string> selectionByCampaign = CrackSelectedPoIsByCampaign(selectionByType);
            bool isCoreInSel = selectionByType.ContainsKey(PointOfInterestType.DCS_CORE);
            bool isUserInSel = selectionByType.ContainsKey(PointOfInterestType.USER);
            bool isCampaignInSel = selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN);
            bool isMultCampaignSel = (selectionByCampaign.Count > 1);


            foreach (TextBox elem in _curPoIFieldValueMap.Values)
                Utilities.SetEnableState(elem, (uiPoIListView.SelectedItems.Count <= 1) && !isCoreInSel);

            bool isPoIValid = !string.IsNullOrEmpty(uiPoIValueName.Text) &&
                              !string.IsNullOrEmpty(uiPoIValueAlt.Text) &&
                              ((EditPoI.CurTheaters != null) && (EditPoI.CurTheaters.Count > 0)) &&
                              !EditPoI.HasErrors;
#endif
            bool isThreatValid = true;

            Utilities.SetEnableState(uiThreatBtnAdd, EditThreat.IsDirty && isThreatValid);
            Utilities.SetEnableState(uiThreatBtnClear, !EditThreat.IsEmpty);

            Utilities.SetEnableState(uiBarBtnCopyUser, uiThreatListView.SelectedItems.Count == 1);
            Utilities.SetEnableState(uiBarBtnDelete, !isCoreInSel && isUserInSel);
            Utilities.SetEnableState(uiBarBtnExport, !isCoreInSel && isUserInSel);

#if NOPE
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
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            EditThreatsFilterDialog filterDialog = new(FilterThreat)
            {
                XamlRoot = Content.XamlRoot
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                FilterThreat = filterDialog.Filter;
            else if (result == ContentDialogResult.Secondary)
                FilterThreat = ThreatFilterSpec.Default;
            else
                return;                                         // EXIT: cancelled, no change...

// TODO: persist map filter

            uiThreatListView.SelectedItems.Clear();
            RebuildThreatList();
            RebuildInterfaceState();

            button.IsChecked = IsFiltered;
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
#endif
            RebuildThreatList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// delete command click: remove the selected threats from the threat database. system threats are skipped.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            if (uiThreatListView.SelectedItems.Count > 0)
            {
                string message = (uiThreatListView.SelectedItems.Count > 1) ? "delete these threats?"
                                                                            : "delete this threat?";
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete threat" + ((uiThreatListView.SelectedItems.Count > 1) ? "s?" : "?"),
                    $"Are you sure you want to {message} This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
#if NOPE
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
#endif
                }
            }
        }

        /// <summary>
        /// import command click: prompt the user for a file to import threats from and deserialize the contents of
        /// the file into the points of interest database. the database is saved following the import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            bool? isSuccess = await ExchangePOIUIHelper.ImportFile(Content.XamlRoot, PointOfInterestDbase.Instance);
            if (isSuccess == true)
            {
                RebuildThreatList();
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
                RebuildInterfaceState();
            }

            // set up enables based on items selected. rely on the command handlers to properly handle situations
            // where there are a mix of types and behave correctly (e.g., not delete core items selected).
            //
            bool isCoreInSel = false;
            bool isUserInSel = false;
            foreach (ThreatListItem item in uiThreatListView.SelectedItems.Cast<ThreatListItem>())
            {
                isCoreInSel = (isCoreInSel || (item.Threat.Type == ThreatType.DCS_CORE));
                isUserInSel = (isUserInSel || (item.Threat.Type == ThreatType.USER));
            }

            bool isSelect = (uiThreatListView.SelectedItems.Count > 0);
            uiThreatListCtxMenuFlyout.Items[0].IsEnabled = isSelect;                                // copy to user
            uiThreatListCtxMenuFlyout.Items[2].IsEnabled = !isCoreInSel && isUserInSel;             // export
            uiThreatListCtxMenuFlyout.Items[4].IsEnabled = !isCoreInSel;                            // delete

            uiThreatListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition(listView));
        }

        /// <summary>
        /// threat list view selection changed: rebuild the interface state to reflect newly selected threat(s).
        /// </summary>
        private void ThreatListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (uiThreatListView.SelectedItems.Count != 1)
            {
                EditThreat.Reset();
            }
            else if (uiThreatListView.SelectedItems.Count == 1)
            {
                ThreatListItem item = uiThreatListView.SelectedItem as ThreatListItem;
#if NOPE
                int index = _llFmtToIndexMap[LLDisplayFmt];

                EditPoI.CurTheaters = PointOfInterest.TheatersForCoords(item.PoI.Latitude, item.PoI.Longitude);
                EditPoI.SourceUID = item.PoI.UniqueID;
                EditPoI.Name = item.PoI.Name;
                EditPoI.Tags = PointOfInterest.SanitizedTags(item.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(item.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(item.PoI.Longitude, LLDisplayFmt);
                EditThreat.Alt = item.PoI.Elevation;
#endif
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// threat add/update button click: add a new threat or update an existing threat with the values from the
        /// threat editor.
        /// </summary>
        private async void ThreatBtnAdd_Click(object sender, RoutedEventArgs args)
        {
#if NOPE
            string newUID;

            if (string.IsNullOrEmpty(EditPoI.SourceUID))
            {
                // no source uid on the edit poi implies we are creating a new poi from the data in the editor
                // fields. determine if the new poi should be a user poi or part of a campaign and what theater
                // it should be placed in by asking the user in situations where there is ambiguity. once we have
                // that information, create the poi from the edit poi, ensuring new poi has unique parameters.

                List<string> campaigns = PointOfInterestDbase.Instance.KnownCampaigns;
                campaigns.Insert(0, $"Add “{EditPoI.Name}” as a User POI");

                if ((campaigns.Count > 1) || (EditPoI.CurTheaters.Count > 1))
                {
                    GetPoITagDetails tagDialog = new(campaigns, LastAddCampaign, EditPoI.CurTheaters, LastAddTheater)
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Select POI Parameters",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel"
                    };
                    ContentDialogResult result = await tagDialog.ShowAsync(ContentDialogPlacement.Popup);
                    if (result == ContentDialogResult.None)
                        return;                                         // **** EXITS: user cancel

                    LastAddCampaign = (tagDialog.Campaign.StartsWith("Add “")) ? null : tagDialog.Campaign;
                    LastAddTheater = tagDialog.Theater;
                }
                if (campaigns.Count == 1)
                    LastAddCampaign = null;
                if (EditPoI.CurTheaters.Count == 1)
                    LastAddTheater = EditPoI.CurTheaters[0];

                newUID = CoreCommitEditChanges(EditPoI, LastAddTheater, LastAddCampaign);
            }
            else
            {
                // a source uid on the edit poi implies we are updating an existing poi in the database. this
                // action may change the theater, but cannot change the campaign. prompt for a new theater if
                // potential theaters in edit poi differ from the potential theaters for the poi's current
                // location; otherwise, we'll use the poi's current theater. once we have that information,
                // update the poi from the edit poi, ensuring new poi has unique parameters.

                PointOfInterest poi = PointOfInterestDbase.Instance.Find(EditPoI.SourceUID);
                List<string> poiTheaters = Theater.TheatersForCoords(poi.Latitude, poi.Longitude);

                if (!poiTheaters.SequenceEqual(EditPoI.CurTheaters) || !poiTheaters.Contains(poi.Theater))
                {
                    GetPoITagDetails tagDialog = new(null, null, EditPoI.CurTheaters, LastAddTheater)
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Select POI Theater",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel"
                    };
                    ContentDialogResult result = await tagDialog.ShowAsync(ContentDialogPlacement.Popup);
                    if (result == ContentDialogResult.None)
                        return;                                         // **** EXITS: user cancel

                    LastAddTheater = tagDialog.Theater;
                }
                if (EditPoI.CurTheaters.Count == 1)
                    LastAddTheater = EditPoI.CurTheaters[0];
                else if (poiTheaters.Contains(poi.Theater))
                    LastAddTheater = poi.Theater;

                newUID = CoreCommitEditChanges(EditPoI, LastAddTheater, poi.Campaign);
            }

            if (newUID == null)
            {
                PromptForNameCollision(EditPoI.Name, LastAddTheater, LastAddCampaign, EditPoI.Tags);
            }
            else
            {
                EditPoI.Reset();
                RebuildPoIList();
                RebuildInterfaceState();
            }
#endif
        }

        /// <summary>
        /// threat clear button click: reset (if editor is dirty and there's a selected item) or reset (otherwise)
        /// the threat editor.
        /// </summary>
        private void ThreatBtnClear_Click(object sender, RoutedEventArgs args)
        {
            if (EditThreat.IsDirty && (uiThreatListView.SelectedItem is ThreatListItem item))
            {
#if NOPE
                int index = _llFmtToIndexMap[LLDisplayFmt];
                EditPoI.Name = item.PoI.Name;
                EditPoI.Tags = PointOfInterest.SanitizedTags(item.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(item.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(item.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = item.PoI.Elevation;
#endif
            }
            else
            {
                uiThreatListView.SelectedItems.Clear();
                EditThreat.Reset();
            }
            RebuildInterfaceState();
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
            uiComboCoalition.SelectedIndex = 0;
            uiComboCategory.SelectedIndex = 0;

            base.OnNavigatedTo(args);

            EditThreat.ClearErrors();
            EditThreat.Reset();

            RebuildThreatList();
            RebuildInterfaceState();
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
