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
using JAFDTC.Models.Threats;
using JAFDTC.Models.Units;
using JAFDTC.UI.Base;
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the threat ListView. this provides ui-friendly views of properties suitable for
    /// display in the ui via bindings.
    /// </summary>
    internal partial class ThreatListItem(Threat threat) : BindableObject
    {
        public Threat Threat { get; private set; } = threat;

        public bool IsOverride { get; set; }

        public string RadiusWEZUI => $"{Threat.RadiusWEZ:F2}";

        public string CoalitionUI => Threat.Coalition switch
        {
            CoalitionType.BLUE => "Blue",
            CoalitionType.RED => "Red",
            CoalitionType.NEUTRAL => "Neutral",
            _ => "—"
        };

        public string CategoryUI => Threat.Category switch
        {
            UnitCategoryType.GROUND => "Ground Unit",
            UnitCategoryType.NAVAL => "Naval Unit",
            _ => "Unknown"
        };

        public string TypeGlyph => Threat.Type switch
        {
            ThreatType.USER => "\xE718",
            _ => ""
        };

        public string ReplaceGlyph => (IsOverride) ? ((Threat.Type == ThreatType.USER) ? "\xE710" : "\xE733") : "";
    }

    // ================================================================================================================

    /// <summary>
    /// backing object for editing a threat. this provides bindings for the text fields at the bottom of the threat
    /// page that set coalition, type information.
    /// </summary>
    internal partial class ThreatDetails : BindableObject
    {
        // ---- properties

        public string SourceUID { get; set; }

        private int _categoryUI;                                // unit category (combobox item index)
        public int CategoryUI
        {
            get => _categoryUI;
            set => SetProperty(ref _categoryUI, value);
        }

        private int _coalitionUI;                               // primary unit coalition (combobox item index)
        public int CoalitionUI
        {
            get => _coalitionUI;
            set => SetProperty(ref _coalitionUI, value);
        }

        private string _typeDCS;                                // dcs .miz unit "type" value for threat
        public string TypeDCS
        {
            get => _typeDCS;
            set => SetProperty(ref _typeDCS, value);
        }

        private string _name;                                   // display name for threat
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _radiusWEZ;                              // radius of wez (nm, 0 => "point" threat)
        public string RadiusWEZ   
        {
            get => _radiusWEZ;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.0, 999.99))
                {
                    value = FixupDecimalField(value, "F2");
                    error = null;
                }
                SetProperty(ref _radiusWEZ, value, error);
            }
        }

        public ThreatDetails()
            => (CoalitionUI, CategoryUI, TypeDCS, Name, RadiusWEZ) =
               (EditThreatsPage.CoalitionTypeToUI(CoalitionType.BLUE),
                EditThreatsPage.CategoryTypeToUI(UnitCategoryType.GROUND),
                "", "", "");

        public void Reset()
        {
            SourceUID = "";
            CoalitionUI = EditThreatsPage.CoalitionTypeToUI(CoalitionType.BLUE);
            CategoryUI = EditThreatsPage.CategoryTypeToUI(UnitCategoryType.GROUND);
            TypeDCS = "";
            Name = "";
            RadiusWEZ = "";
        }
    }

    // ================================================================================================================

    /// <summary>
    /// main page for the threat editor. this provides the ui view of the threat database in jafdtc and implements
    /// typical editor actions to create, modify, and delete threats.
    /// </summary>
    public sealed partial class EditThreatsPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // types
        //
        // ------------------------------------------------------------------------------------------------------------

        public enum ComboItemIndexCoalition
        {
            BLUE = 0,
            RED = 1,
            NEUTRAL = 2
        };

        public enum ComboItemIndexCategory
        {
            GROUND = 0,
            NAVAL = 1
        };

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

        private bool IsEditorStateImpliesAdd
        {
            get
            {
                ThreatDbaseQuery query = new([ uiTextThreatType.Text ], null, [ ThreatType.USER ]);
                return (ThreatDbase.Instance.Find(query).Count == 0);
            }
        }

        private bool IsEditorStateImpliesClear
        {
            get
            {
                Threat threat = ThreatDbase.Instance.Find(EditThreat.SourceUID);
                return ((threat == null) ||
                        ((threat.Category == CategoryUIToType(uiComboThreatCategory.SelectedIndex)) &&
                         (threat.Coalition == CoalitionUIToType(uiComboThreatCoalition.SelectedIndex)) &&
                         (threat.UnitTypeDCS == uiTextThreatType.Text) &&
                         (threat.Name == uiTextThreatName.Text) &&
                         (threat.RadiusWEZ == double.Parse(uiTextThreatRadius.Text))));
            }
        }

        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curThreatFieldValueMap;
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
                if (threat.Type == ThreatType.SYSTEM)
                    SystemThreatDCSTypes.Add(threat.UnitTypeDCS);

            EditThreat = new ThreatDetails();
            EditThreat.ErrorsChanged += ThreatField_DataValidationError;

            FilterThreat = Settings.LastThreatFilter;
            uiBarBtnFilter.IsChecked = IsFiltered;

            _curThreatFieldValueMap = new()
            {
                ["RadiusWEZ"] = uiTextThreatRadius,
            };
            _defaultBorderBrush = uiTextThreatType.BorderBrush;
            _defaultBkgndBrush = uiTextThreatType.Background;
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
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
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
                bool isValid = (((List<string>)EditThreat.GetErrors(args.PropertyName)).Count == 0);
                if (_curThreatFieldValueMap.TryGetValue(args.PropertyName, out TextBox textBox))
                    SetFieldValidState(textBox, isValid);
            }
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the index of the coalition combo corresponding to a CoalitionType.
        /// </summary>
        public static int CoalitionTypeToUI(CoalitionType coalition) => coalition switch
        {
            CoalitionType.BLUE => (int)ComboItemIndexCoalition.BLUE,
            CoalitionType.RED => (int)ComboItemIndexCoalition.RED,
            CoalitionType.NEUTRAL => (int)ComboItemIndexCoalition.NEUTRAL,
            _ => (int)ComboItemIndexCoalition.BLUE
        };

        /// <summary>
        /// return the CoalitionType corersponding to the item index in the coalition combo.
        /// </summary>
        public static CoalitionType CoalitionUIToType(int index) => index switch
        {
            (int)ComboItemIndexCoalition.BLUE => CoalitionType.BLUE,
            (int)ComboItemIndexCoalition.RED => CoalitionType.RED,
            (int)ComboItemIndexCoalition.NEUTRAL => CoalitionType.NEUTRAL,
            _ => CoalitionType.UNKNOWN
        };

        /// <summary>
        /// return the index of the category combo corresponding to a UnitCategoryType.
        /// </summary>
        public static int CategoryTypeToUI(UnitCategoryType category) => category switch
        {
            UnitCategoryType.GROUND => (int)ComboItemIndexCategory.GROUND,
            UnitCategoryType.NAVAL => (int)ComboItemIndexCategory.NAVAL,
            _ => (int)ComboItemIndexCategory.GROUND
        };

        /// <summary>
        /// return the UnitCategoryType corersponding to the item index in the category combo.
        /// </summary>
        public static UnitCategoryType CategoryUIToType(int index) => index switch
        {
            (int)ComboItemIndexCategory.GROUND => UnitCategoryType.GROUND,
            (int)ComboItemIndexCategory.NAVAL => UnitCategoryType.NAVAL,
            _ => UnitCategoryType.UNKNOWN
        };

        /// <summary>
        /// return a list of threats matching the current filter configuration with a name that  containst the
        /// provided name fragment.
        /// </summary>
        private IReadOnlyList<Threat> GetThreatsMatchingFilter(string name = null)
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
                threatTypes = [.. threatTypes, ThreatType.SYSTEM ];
            if (FilterThreat.ShowThreatsUser)
                threatTypes = [.. threatTypes, ThreatType.USER ];

            ThreatDbaseQuery query = new(null, name, threatTypes, categories, coalitions, false);
            return ThreatDbase.Instance.Find(query, true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async Task<Threat> CopyThreatToUser(Threat threat, bool isPromptOnExisting = true)
        {
            ContentDialogResult result = ContentDialogResult.Primary;
            ThreatDbaseQuery query = new([threat.UnitTypeDCS], null, [ThreatType.USER]);
            if (isPromptOnExisting && (ThreatDbase.Instance.Find(query).Count > 0))
            {
                result = await Utilities.Message2BDialog(Content.XamlRoot,
                    "Replace User Threat",
                    $"A user threat for DCS type “{threat.UnitTypeDCS}” already exists in the database." +
                    $" Would you like to replace it?",
                    "Replace", "Cancel");
                if (result == ContentDialogResult.Primary)
                    ThreatDbase.Instance.RemoveThreat(threat, false);
            }
            if (result == ContentDialogResult.Primary)
            {
                Threat newThreat = new()
                {
                    Type = ThreatType.USER,
                    UnitTypeDCS = threat.UnitTypeDCS,
                    Category = threat.Category,
                    Coalition = threat.Coalition,
                    Name = threat.Name,
                    RadiusWEZ = threat.RadiusWEZ
                };
                ThreatDbase.Instance.AddThreat(newThreat);
                RebuildThreatList();
                return newThreat;
            }
            return null;
        }

        /// <summary>
        /// select the threat in the threat list with the given uid. selection is unchanged if no uid matches.
        /// </summary>
        private void SelectThreatWithUID(string uid)
        {
            for (int i = 0; i < CurThreatItems.Count; i++)
                if (CurThreatItems[i].Threat.UniqueID == uid)
                {
                    uiThreatListView.SelectedIndex = i;
                    uiThreatListView.ScrollIntoView(uiThreatListView.SelectedItem);
                    break;
                }
        }

        /// <summary>
        /// rebuild the content of the threat list based on the current contents of the threat database along with
        /// the currently included types from the filter specification. name specifies the partial name to match,
        /// null if no match on name.
        /// </summary>
        private void RebuildThreatList(string name = null)
        {
// TODO: preserve selection, or nah?
            Dictionary<string, ThreatListItem> sysItems = [ ];
            CurThreatItems.Clear();
            foreach (Threat threat in GetThreatsMatchingFilter(name))
            {
                ThreatListItem item = new(threat);
                if (threat.Type == ThreatType.SYSTEM)
                    sysItems[threat.UnitTypeDCS] = item;
                if ((threat.Type == ThreatType.USER) && SystemThreatDCSTypes.Contains(threat.UnitTypeDCS))
                {
                    item.IsOverride = true;
                    sysItems[threat.UnitTypeDCS].IsOverride = true;
                }
                CurThreatItems.Add(item);
            }
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the threat editor. if we have a
        /// source uid, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitles()
        {
            uiThreatTextBtnAdd.Text = (IsEditorStateImpliesAdd) ? "Add" : "Update";
            uiThreatTextBtnClear.Text = (IsEditorStateImpliesClear) ? "Clear" : "Reset";
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
                isCoreInSel = (isCoreInSel || (item.Threat.Type == ThreatType.SYSTEM));
                isUserInSel = (isUserInSel || (item.Threat.Type == ThreatType.USER));
            }

            bool isEmpty = true;
            foreach (TextBox textBox in _curThreatFieldValueMap.Values)
                if (!string.IsNullOrEmpty(textBox.Text))
                    isEmpty = false;

            Threat threat = ThreatDbase.Instance.Find(EditThreat.SourceUID);
            ThreatDbaseQuery query = new([uiTextThreatType.Text], null, [ThreatType.USER]);
            IReadOnlyList<Threat> matches = ThreatDbase.Instance.Find(query);
            bool isTypeFieldValid = ((matches.Count == 0) ||
                                     (threat == null) || (threat.Type == ThreatType.SYSTEM) ||
                                     (EditThreat.SourceUID == matches[0].UniqueID));
            bool isDirty = ((threat != null) &&
                            ((threat.Category != CategoryUIToType(uiComboThreatCategory.SelectedIndex)) ||
                             (threat.Coalition != CoalitionUIToType(uiComboThreatCoalition.SelectedIndex)) ||
                             (threat.UnitTypeDCS != uiTextThreatType.Text) ||
                             (threat.Name != uiTextThreatName.Text) ||
                             (threat.RadiusWEZ != double.Parse(uiTextThreatRadius.Text))));
            bool isThreatValid = !string.IsNullOrEmpty(EditThreat.TypeDCS) &&
                                 !string.IsNullOrEmpty(EditThreat.Name) &&
                                 !string.IsNullOrEmpty(EditThreat.RadiusWEZ) &&
                                 !EditThreat.HasErrors;

            Utilities.SetEnableState(uiThreatBtnAdd, (isDirty || isThreatValid) && isTypeFieldValid);
            Utilities.SetEnableState(uiThreatBtnClear, !isEmpty);

            Utilities.SetEnableState(uiBarBtnCopyUser, isCoreInSel && (uiThreatListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarBtnDelete, !isCoreInSel && isUserInSel);
            Utilities.SetEnableState(uiBarBtnExport, !isCoreInSel && isUserInSel);

            SetFieldValidState(uiTextThreatType, isTypeFieldValid);
            ValidateAllFields(_curThreatFieldValueMap, EditThreat.GetErrors(null));
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
                IReadOnlyList<Threat> threats = GetThreatsMatchingFilter(sender.Text);
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

            Settings.LastThreatFilter = FilterThreat;

            uiThreatListView.SelectedItems.Clear();
            RebuildThreatList();
            RebuildInterfaceState();

            button.IsChecked = IsFiltered;
        }

        /// <summary>
        /// edit command click: copy the details of the currnetly selected threat into the threat editor fields,
        /// update the database, and rebuild the interface state to reflect the change. this should only be called
        /// on read-only system threats.
        /// </summary>
        private async void CmdCopyUser_Click(object sender, RoutedEventArgs args)
        {
            if (uiThreatListView.SelectedItems.Count == 1)
            {
                ThreatListItem item = uiThreatListView.SelectedItem as ThreatListItem;
                Threat newThreat = await CopyThreatToUser(item.Threat);
                if (newThreat != null)
                    SelectThreatWithUID(newThreat.UniqueID);
                RebuildInterfaceState();
            }
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
                    foreach (ThreatListItem item in uiThreatListView.SelectedItems.Cast<ThreatListItem>())
                        if (item.Threat.Type != ThreatType.SYSTEM)
                            ThreatDbase.Instance.RemoveThreat(item.Threat, false);
                    ThreatDbase.Instance.Save();

                    RebuildThreatList();
                    RebuildInterfaceState();
                }
            }
        }

        /// <summary>
        /// import command click: prompt the user for a file to import threats from and deserialize the contents of
        /// the file into the points of interest database. the database is saved following the import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            bool? isSuccess = await ExchangeThreatUIHelper.ImportFile(Content.XamlRoot, PointOfInterestDbase.Instance);
            if (isSuccess == true)
            {
                RebuildThreatList();
                RebuildInterfaceState();
            }
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
            List<Threat> threats = [ ];
            foreach (ThreatListItem threatItem in uiThreatListView.SelectedItems.Cast<ThreatListItem>())
                if (threatItem.Threat.Type == ThreatType.USER)
                    threats.Add(threatItem.Threat);

            ExchangeThreatUIHelper.ExportFileForUser(Content.XamlRoot, threats);
        }

        private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
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
                isCoreInSel = (isCoreInSel || (item.Threat.Type == ThreatType.SYSTEM));
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

                uiComboThreatCoalition.SelectedIndex = EditThreat.CoalitionUI;
                uiComboThreatCategory.SelectedIndex = EditThreat.CategoryUI;
            }
            else if (uiThreatListView.SelectedItems.Count == 1)
            {
                ThreatListItem item = uiThreatListView.SelectedItem as ThreatListItem;
                EditThreat.SourceUID = item.Threat.UniqueID;
                EditThreat.CoalitionUI = CoalitionTypeToUI(item.Threat.Coalition);
                EditThreat.CategoryUI = CategoryTypeToUI(item.Threat.Category);
                EditThreat.Name = item.Threat.Name;
                EditThreat.TypeDCS = item.Threat.UnitTypeDCS;
                EditThreat.RadiusWEZ = $"{item.Threat.RadiusWEZ:F2}";

                uiComboThreatCoalition.SelectedIndex = EditThreat.CoalitionUI;
                uiComboThreatCategory.SelectedIndex = EditThreat.CategoryUI;
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// threat add/update button click: add a new threat or update an existing threat with the values from the
        /// threat editor.
        /// </summary>
        private async void ThreatBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            Threat newThreat = new()
            {
                Type = ThreatType.USER,
                UnitTypeDCS = EditThreat.TypeDCS,
                Category = CategoryUIToType(uiComboThreatCategory.SelectedIndex),
                Coalition = CoalitionUIToType(uiComboThreatCoalition.SelectedIndex),
                Name = EditThreat.Name,
                RadiusWEZ = double.Parse(EditThreat.RadiusWEZ)
            };
            if (!IsEditorStateImpliesAdd)
            {
                Threat oldThreat = ThreatDbase.Instance.Find(EditThreat.SourceUID);
                ThreatDbase.Instance.RemoveThreat(oldThreat, false);
            }
            newThreat = await CopyThreatToUser(newThreat, false);
            SelectThreatWithUID(newThreat.UniqueID);
        }

        /// <summary>
        /// threat clear button click: reset (if editor is dirty and there's a selected item) or reset (otherwise)
        /// the threat editor.
        /// </summary>
        private void ThreatBtnClear_Click(object sender, RoutedEventArgs args)
        {
            if (!IsEditorStateImpliesClear && (uiThreatListView.SelectedItem is ThreatListItem item))
            {
                EditThreat.CoalitionUI = CoalitionTypeToUI(item.Threat.Coalition);
                EditThreat.CategoryUI = CategoryTypeToUI(item.Threat.Category);
                EditThreat.Name = item.Threat.Name;
                EditThreat.TypeDCS = item.Threat.UnitTypeDCS;
                EditThreat.RadiusWEZ = $"{item.Threat.RadiusWEZ:F2}";
            }
            else
            {
                uiThreatListView.SelectedItems.Clear();
                EditThreat.Reset();
            }
            RebuildInterfaceState();
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// threat editor value changed: update the interface state to reflect changes in the text value.
        /// </summary>
        private void ThreatTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// threat editor value field lost focus: update the interface state to reflect changes in the text value.
        /// </summary>
        private void ThreatTextBox_LostFocus(object sender, RoutedEventArgs args)
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
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            uiComboThreatCoalition.SelectedIndex = 0;
            uiComboThreatCategory.SelectedIndex = 0;

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
