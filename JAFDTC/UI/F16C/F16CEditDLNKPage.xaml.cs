// ********************************************************************************************************************
//
// F16CEditDLNKPage.xaml.cs : ui c# for viper data link editor page
//
// Copyright(C) 2023-2026 ilominar/raven
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

using CommunityToolkit.WinUI;
using JAFDTC.Core.Expressions;
using JAFDTC.Models;
using JAFDTC.Models.Core;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.Pilots;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// Page obejct for the system editor page that handles the ui for the viper datalink table editor. this handles
    /// setup for the tndl, callsign, flight lead and other data link state.
    /// </summary>
    public sealed partial class F16CEditDLNKPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        public static ConfigEditorPageInfo PageInfo
            => new(DLNKSystem.SystemTag, "Datalink", "DLNK", Glyphs.DLNK, typeof(F16CEditDLNKPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // internal classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// specialization of the datalink system to include ui views of several parameters.
        /// </summary>
        private partial class DLNKSystemUI : DLNKSystem
        {
            private string _ownshipCallsignUI;                      // string, two letter [A-Z][A-Z]
            public string OwnshipCallsignUI
            {
                get => _ownshipCallsignUI;
                set
                {
                    OwnshipCallsign = (value != "––") ? value : "";
                    SetProperty(ref _ownshipCallsignUI, OwnshipCallsign);
                }
            }

            private string _ownshipFENumberUI;                      // string, two digit [1-9][1-9]
            public string OwnshipFENumberUI
            {
                get => _ownshipFENumberUI;
                set
                {
                    OwnshipFENumber = (value != "––") ? value : "";
                    SetProperty(ref _ownshipFENumberUI, OwnshipFENumber);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditDLNK property.
        //
        private F16CConfiguration Config { get; set; }

        private DLNKSystemUI EditDLNK { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        private string OwnshipDriverUID { get; set; }

        private string OwnshipCallsign { get; set; }

        private IReadOnlyList<Pilot> AvailablePilots { get; set; }

        private ObservableCollection<Pilot> AssignedPilots { get; set; } = [ ];

        private Pilot UnknownPilot { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly List<CheckBox> _tableTDOACkbxList;
        private readonly List<TextBox> _tableTNDLTextList;
        private readonly List<PilotComboControl> _tableCallsignComboList;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditDLNKPage()
        {
            UnknownPilot = new Pilot();

            PilotDbaseQuery query = new()
            {
                Airframes = [AirframeTypes.F16C]
            };
            List<Pilot> pilots = [.. PilotDbase.Instance.Find(query, true)];
            pilots.Insert(0, UnknownPilot);
            AvailablePilots = pilots;
            OwnshipCallsign = Settings.Callsign;
            OwnshipDriverUID = "<unknown>";
            foreach (Pilot driver in AvailablePilots)
                if (driver.Name == OwnshipCallsign)
                {
                    OwnshipDriverUID = driver.UniqueID;
                    break;
                }

            EditDLNK = new DLNKSystemUI();
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                EditDLNK.TeamMembers[i].ErrorsChanged += TeamField_DataValidationError;
            EditDLNK.ErrorsChanged += BaseField_DataValidationError;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            InitializeComponent();

            for (int i = 0; i < DLNKSystem.NUM_SLOTS_IN_TEAM; i++)
                AssignedPilots.Add(UnknownPilot);

            _configNameToUID = [];
            _configNameList = [];

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["OwnshipCallsign"] = uiOwnTextCallsign,
                ["OwnshipFENumber"] = uiOwnTextFENum,
                ["FillEmptyTNDL"] = uiOwnTextFillTNDL
            };
            _tableTDOACkbxList =
            [
                uiTNDLCkbxTDOA1, uiTNDLCkbxTDOA2, uiTNDLCkbxTDOA3, uiTNDLCkbxTDOA4,
                uiTNDLCkbxTDOA5, uiTNDLCkbxTDOA6, uiTNDLCkbxTDOA7, uiTNDLCkbxTDOA8
            ];
            _tableTNDLTextList =
            [
                uiTNDLTextTNDL1, uiTNDLTextTNDL2, uiTNDLTextTNDL3, uiTNDLTextTNDL4,
                uiTNDLTextTNDL5, uiTNDLTextTNDL6, uiTNDLTextTNDL7, uiTNDLTextTNDL8,
            ];
            _tableCallsignComboList =
            [
                uiTNDLComboCallsign1, uiTNDLComboCallsign2, uiTNDLComboCallsign3, uiTNDLComboCallsign4,
                uiTNDLComboCallsign5, uiTNDLComboCallsign6, uiTNDLComboCallsign7, uiTNDLComboCallsign8
            ];
            _defaultBorderBrush = uiTNDLTextTNDL1.BorderBrush;
            _defaultBkgndBrush = uiTNDLTextTNDL1.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshal data from the datalink configuration to our local edit state.
        /// </summary>
        private void CopyConfigToEdit()
        {
            EditDLNK.Ownship = Config.DLNK.Ownship;
            EditDLNK.OwnshipCallsignUI = Config.DLNK.OwnshipCallsign;
            EditDLNK.OwnshipFENumberUI = Config.DLNK.OwnshipFENumber;
            //
            // to prevent potential fuckery and chaos, always disable mission linking when the configuration of the
            // datalink system to another datalink system, regardless of the setting in the source system.
            //
            EditDLNK.IsLinkedMission = (Config.DLNK.IsLinkedMission &&
                                        string.IsNullOrEmpty(Config.SystemLinkedTo(DLNKSystem.SystemTag)));
            EditDLNK.IsOwnshipLead = Config.DLNK.IsOwnshipLead;
            EditDLNK.IsFillEmptyTNDL = Config.DLNK.IsFillEmptyTNDL;
            EditDLNK.FillEmptyTNDL = (Config.DLNK.IsFillEmptyTNDL) ? Config.DLNK.FillEmptyTNDL : "";
            if (string.IsNullOrEmpty(Config.DLNK.FillEmptyTNDL))
                EditDLNK.IsFillEmptyTNDL = false;
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                Pilot pilot = PilotDbase.Instance.Find(Config.DLNK.TeamMembers[i].DriverUID);
                if (pilot != null)
                {
                    AssignedPilots[i] = pilot;
                    EditDLNK.TeamMembers[i].TDOA = Config.DLNK.TeamMembers[i].TDOA;
                    EditDLNK.TeamMembers[i].TNDL = new(pilot.AvionicsID);
                    EditDLNK.TeamMembers[i].DriverUID = new(pilot.UniqueID);
                }
                else if (string.IsNullOrEmpty(Config.DLNK.TeamMembers[i].DriverUID))
                {
                    AssignedPilots[i] = UnknownPilot;
                    EditDLNK.TeamMembers[i].TDOA = Config.DLNK.TeamMembers[i].TDOA;
                    EditDLNK.TeamMembers[i].TNDL = new(Config.DLNK.TeamMembers[i].TNDL);
                    EditDLNK.TeamMembers[i].DriverUID = "";
                }
                else
                {
                    AssignedPilots[i] = UnknownPilot;
                    EditDLNK.TeamMembers[i].TDOA = false;
                    EditDLNK.TeamMembers[i].TNDL = "";
                    EditDLNK.TeamMembers[i].DriverUID = "";
                }

                // it's possible that Ownship can get out of sync with the team members (for example, if you directly
                // import a DLNK configuration from another pilot). check that here and update if necessary.
                //
                string ownship = (i + 1).ToString();
                if ((Config.DLNK.TeamMembers[i].DriverUID == OwnshipDriverUID) && (ownship != Config.DLNK.Ownship))
                {
                    EditDLNK.Ownship = ownship;
                    CopyEditToConfig(true);
                }
            }
        }

        /// <summary>
        /// marshal data from our local edit state to the datalink configuration if there are no errors. configuration
        /// is optionally persisted.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!CurStateHasErrors())
            {
                Config.DLNK.Ownship = new(EditDLNK.Ownship);
                //
                // OwnshipCallsign, OwnshipFENumber fields use text masks and can come back as "--" when empty. this
                // is really "" and, since that value is OK, remove the error.
                //
                Config.DLNK.OwnshipCallsign = EditDLNK.OwnshipCallsign;
                Config.DLNK.OwnshipFENumber = EditDLNK.OwnshipFENumber;
                //
                // to prevent potential fuckery and chaos, always disable mission linking when the configuration of the
                // datalink system to another datalink system, regardless of the setting in the source system.
                //
                Config.DLNK.IsLinkedMission = (EditDLNK.IsLinkedMission &&
                                               string.IsNullOrEmpty(Config.SystemLinkedTo(DLNKSystem.SystemTag)));
                Config.DLNK.IsOwnshipLead = EditDLNK.IsOwnshipLead;
                Config.DLNK.IsFillEmptyTNDL = EditDLNK.IsFillEmptyTNDL;
                Config.DLNK.FillEmptyTNDL = (EditDLNK.IsFillEmptyTNDL) ? EditDLNK.FillEmptyTNDL : "";
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                {
                    Config.DLNK.TeamMembers[i].TDOA = EditDLNK.TeamMembers[i].TDOA;
                    Config.DLNK.TeamMembers[i].TNDL = new(EditDLNK.TeamMembers[i].TNDL);
                    Config.DLNK.TeamMembers[i].DriverUID = new(EditDLNK.TeamMembers[i].DriverUID);
                }

                if (isPersist)
                    Config.Save(this, DLNKSystem.SystemTag);
            }
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

        /// <summary>
        /// event handler for a validation error. check for errors in the base EditDLNK.TeamMembers fields and update
        /// the state of the ui element to indicate error state.
        /// 
        /// NOTE: of the properties we bind to the ui, only TeamMember.TNDL fields can raise an error.
        /// </summary>
        private void TeamField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                    SetFieldValidState(_tableTNDLTextList[i], EditDLNK.TeamMembers[i].HasErrors);
            }
            else
            {
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                    if (sender.Equals(EditDLNK.TeamMembers[i]))
                    {
                        SetFieldValidState(_tableTNDLTextList[i], !EditDLNK.TeamMembers[i].HasErrors);
                        break;
                    }
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// event handler for a validation error. check for errors in the base EditDLNK fields and update the state of
        /// the ui element to indicate error state.
        /// 
        /// NOTE: of the properties we bind to the ui, only TeamMember.TNDL fields can raise an error.
        /// </summary>
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                Dictionary<string, bool> map = [];
                foreach (string error in EditDLNK.GetErrors(null))
                    map[error] = true;
                foreach (KeyValuePair<string, TextBox> kvp in _baseFieldValueMap)
                    SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
            else
            {
                SetFieldValidState(_baseFieldValueMap[args.PropertyName],
                                   (((List<string>)EditDLNK.GetErrors(args.PropertyName)).Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current state in EditDLNK has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            TextBox tboxCS = _baseFieldValueMap["OwnshipCallsign"];
            TextBox tboxFE = _baseFieldValueMap["OwnshipFENumber"];
            if ((!TextBoxExtensions.GetIsValid(tboxCS) && (tboxCS.Text != "––")) ||
                (!TextBoxExtensions.GetIsValid(tboxFE) && (tboxFE.Text != "––")))
                return true;

            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                if (EditDLNK.TeamMembers[i].HasErrors)
                    return true;

            return (((List<string>)EditDLNK.GetErrors("FillEmptyTNDL")).Count != 0);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// force an update to the table callsign combo and ownship callsign/fe number.
        /// </summary>
        private void ForceSyncUI()
        {
            // TODO: get the bindings set up right so we don't have to do this crap.
            //
            IsRebuildingUI = true;
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                _tableCallsignComboList[i].SelectedPilot = AssignedPilots[i];
                _tableTDOACkbxList[i].IsChecked = EditDLNK.TeamMembers[i].TDOA;
                _tableTNDLTextList[i].Text = EditDLNK.TeamMembers[i].TNDL;
            }
            EditDLNK.OwnshipCallsignUI = Config.DLNK.OwnshipCallsign;
            EditDLNK.OwnshipFENumberUI = Config.DLNK.OwnshipFENumber;
            uiOwnTextCallsign.Text = EditDLNK.OwnshipCallsignUI;
            uiOwnTextFENum.Text = EditDLNK.OwnshipFENumberUI;
            IsRebuildingUI = false;
        }

        /// <summary>
        /// update edit state to be consistent with linkage setting. caller should rebuild the ui (to update enable
        /// state) and save the configuration after calling this.
        /// </summary>
        private void SetupMissionLinkage(bool isLinked)
        {
            if (isLinked)
            {
                int ownshipPosition = -1;
                for (int i = 0; i < Config.Mission.Ships; i++)
                {
                    for (int j = 0; j < EditDLNK.TeamMembers.Length; j++)
                        if ((i != j) && (Config.Mission.PilotUIDs[i] == EditDLNK.TeamMembers[j].DriverUID))
                        {
                            _tableCallsignComboList[i].SelectedPilot = UnknownPilot;
                            EditDLNK.TeamMembers[j].Reset();
                        }

                    if (Config.Mission.PilotUIDs[i] == OwnshipDriverUID)
                    {
                        EditDLNK.TeamMembers[i].TDOA = true;
                        ownshipPosition = i + 1;
                    }
                    Pilot pilot = PilotDbase.Instance.Find(Config.Mission.PilotUIDs[i]);
                    //
                    // NOTE: directly select new pilot here as don't have 2-way bindings from control in xaml.
                    //
                    _tableCallsignComboList[i].SelectedPilot = pilot ?? UnknownPilot;
                    EditDLNK.TeamMembers[i].TNDL = (pilot != null) ? pilot.AvionicsID : "";
                }

                MatchCollection m = CommonExpressions.CallsignRegex().Matches(Config.Mission.Callsign.ToUpper());
                if ((ownshipPosition != -1) && (m.Count == 1) && string.IsNullOrEmpty(m[0].Groups[3].Value.ToString()))
                {
                    EditDLNK.OwnshipCallsignUI = $"{m[0].Groups[1].ToString().First()}{m[0].Groups[1].ToString().Last()}";
                    EditDLNK.OwnshipFENumberUI = $"{m[0].Groups[2]}{ownshipPosition}";
                }
                else
                {
                    EditDLNK.OwnshipCallsignUI = "––";
                    EditDLNK.OwnshipFENumberUI = "––";
                }
                EditDLNK.IsOwnshipLead = (Config.Mission.PilotUIDs[0] == OwnshipDriverUID);
            }
        }

        /// <summary>
        /// set up the swap and mission information state based on the "link to mission" setting. for linked
        /// configurations both elements are hidden.
        /// </summary>
        private void RebuildSwapAndInfo()
        {
            if (!string.IsNullOrEmpty(Config.SystemLinkedTo(DLNKSystem.SystemTag)))
            {
                uiTxtMissionInfo.Visibility = Visibility.Collapsed;
                uiTNDLBtnSwap.Visibility = Visibility.Collapsed;
            }
            else if (EditDLNK.IsLinkedMission)
            {
                uiTxtMissionInfo.Text = $"Pilots for TNDL entries 1-{Config.Mission.Ships} correspond to" +
                                        $" {Config.Mission.Callsign}-1 through" +
                                        $" {Config.Mission.Callsign}-{Config.Mission.Ships} and are set by the" +
                                        $" mission configuration.";
                uiTxtMissionInfo.Visibility = Visibility.Visible;
                uiTNDLBtnSwap.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiTxtMissionInfo.Visibility = Visibility.Collapsed;
                uiTNDLBtnSwap.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildOwnshipMenu()
        {
            int indexPDbOwnship = -1;
            int indexCurOwnship = -1;
            List<string> list = [];
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                if (!string.IsNullOrEmpty(EditDLNK.TeamMembers[i].TNDL))
                {
                    list.Add((i + 1).ToString());
                    if (!string.IsNullOrEmpty(EditDLNK.Ownship) && (int.Parse(EditDLNK.Ownship) == (i + 1)))
                        indexCurOwnship = list.Count - 1;
                }
                if (EditDLNK.TeamMembers[i].DriverUID == OwnshipDriverUID)
                    indexPDbOwnship = list.Count - 1;
            }
            uiOwnComboEntry.ItemsSource = list;
            if (indexPDbOwnship != -1)
                uiOwnComboEntry.SelectedIndex = indexPDbOwnship;
            else if (indexCurOwnship != -1)
                uiOwnComboEntry.SelectedIndex = indexCurOwnship;
        }

        /// <summary>
        /// rebuild the link and reset controls at the bottom of the page.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, DLNKSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        public void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(DLNKSystem.SystemTag));

            bool isOwnInTable = false;
            bool isAnyInTable = false;
            for (int i = 0; i < _tableTDOACkbxList.Count; i++)
            {
                bool isOwn = (EditDLNK.TeamMembers[i].DriverUID == OwnshipDriverUID);
                bool isEmpty = string.IsNullOrEmpty(_tableTNDLTextList[i].Text);
                bool isInMission = (EditDLNK.IsLinkedMission && (i < Config.Mission.Ships));
                Utilities.SetEnableState(_tableTDOACkbxList[i], isEditable && !isEmpty && !isOwn);
                bool isCallsignSelect = ((_tableCallsignComboList[i].SelectedPilot != null) &&
                                         (_tableCallsignComboList[i].SelectedPilot.UniqueID != UnknownPilot.UniqueID));
                Utilities.SetEnableState(_tableTNDLTextList[i], isEditable && !isCallsignSelect && !isInMission);
                Utilities.SetEnableState(_tableCallsignComboList[i], isEditable);

                if (EditDLNK.TeamMembers[i].DriverUID == OwnshipDriverUID)
                    isOwnInTable = true;
                isAnyInTable |= !EditDLNK.TeamMembers[i].IsDefault;
            }

            Utilities.SetEnableState(uiMsnCkbxLink, isEditable);

            Utilities.SetEnableState(uiOwnComboEntry, isEditable && !isOwnInTable && isAnyInTable);
            Utilities.SetEnableState(uiOwnCkbxLead, isEditable && !EditDLNK.IsLinkedMission);

            Utilities.SetEnableState(uiOwnTextCallsign, isEditable && !EditDLNK.IsLinkedMission);
            Utilities.SetEnableState(uiOwnTextFENum, isEditable && !EditDLNK.IsLinkedMission);

            for (int i = 0; i < Config.Mission.Ships; i++)
                _tableCallsignComboList[i].IsEnabled = !EditDLNK.IsLinkedMission;

            Utilities.SetEnableState(uiTNDLBtnSwap, isEditable);

            Utilities.SetEnableState(uiOwnCkbxFill, isEditable);
            Utilities.SetEnableState(uiOwnTextFillTNDL, isEditable && (bool)uiOwnCkbxFill.IsChecked);

            Utilities.SetEnableState(uiPageBtnResetAll, !EditDLNK.IsDefault);
        }

        /// <summary>
        /// rebuild the user interface state to match current backing state. the method will enqueue a low-priority
        /// task to do the rebuild assuming there is not a rebuild currently pending and the app is not shutting
        /// down.
        /// </summary>
        public void RebuildInterfaceState()
        {
            if (!IsRebuildPending && !(Application.Current as JAFDTC.App).IsAppShuttingDown)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildSwapAndInfo();
                    RebuildOwnshipMenu();
                    RebuildLinkControls();
                    RebuildEnableState();
                    IsRebuildingUI = false;
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: reset all dlnk settings back to their defaults if the user consents.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the datalink configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(DLNKSystem.SystemTag);
                Config.DLNK.Reset();
                Config.Save(this, DLNKSystem.SystemTag);
                CopyConfigToEdit();
                ForceSyncUI();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, DLNKSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(DLNKSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(DLNKSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
                ForceSyncUI();
            }
        }

        // ---- mission link elements ---------------------------------------------------------------------------------

        /// <summary>
        /// on mission checkbox clicks, update the ui and editor state as appropriate either linking it to the
        /// mission setup or unlinking it.
        /// </summary>
        private void MsnCkbxLink_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditDLNK.IsLinkedMission = (bool)cbox.IsChecked;
            SetupMissionLinkage(EditDLNK.IsLinkedMission);
            CopyEditToConfig(true);
            RebuildInterfaceState();
        }

        // ---- ownship elements --------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OwnCkbxLead_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditDLNK.IsOwnshipLead = (bool)cbox.IsChecked;
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OwnCkbxFill_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditDLNK.IsFillEmptyTNDL = (bool)cbox.IsChecked;
            EditDLNK.FillEmptyTNDL = (EditDLNK.IsFillEmptyTNDL) ? "66716" : "";
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OwnTextFillTNDL_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox tbox = (TextBox)sender;
            tbox.IsEnabled = false;
            if (string.IsNullOrEmpty(tbox.Text))
            {
                int index = int.Parse((string)tbox.Tag);
                EditDLNK.TeamMembers[index].Reset();
                if (!string.IsNullOrEmpty(EditDLNK.Ownship) && (EditDLNK.Ownship == (index + 1).ToString()))
                    uiOwnComboEntry.SelectedIndex = -1;
            }
            CopyEditToConfig(true);
        }

        /// <summary>
        /// on changes to the ownship combo, update the tdoa value: ownship tdoa is always true, non-ownship
        /// starts as false.
        /// </summary>
        private void OwnComboEntry_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (!IsRebuildingUI)
            {
                if (!string.IsNullOrEmpty(EditDLNK.Ownship))
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship) - 1].TDOA = false;
                EditDLNK.Ownship = (string)comboBox.SelectedItem;
                if (!string.IsNullOrEmpty(EditDLNK.Ownship))
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship) - 1].TDOA = true;
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        /// status) and rebuild the interface state.
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// </summary>
        private void OwnText_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox textBox = (TextBox)sender;
            if (((textBox == uiOwnTextCallsign) || (textBox == uiOwnTextFENum)) && (textBox.Text == "––"))
            {
                // callsign and flight/element fields uses text mask and can come back as "--" when empty. this
                // is really "" and, since that value is OK, remove the error. note that as we just lost focus,
                // the bound property in EditDLNK.OwnshipCallsign or .OwnshipFENumber may not yet be set up.
                //
                EditDLNK.ClearErrors((textBox == uiOwnTextCallsign) ? "OwnshipCallsign" : "OwnshipFENumber");
                SetFieldValidState(textBox, true);
            }

            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(true);
            });
        }

        // ---- member interface elements -----------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLCkbxTDOA_Click(object sender, RoutedEventArgs args)
        {
            if (!IsRebuildingUI)
                CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLTextTNDL_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox tbox = (TextBox)sender;
            if (string.IsNullOrEmpty(tbox.Text))
            {
                int index = int.Parse((string)tbox.Tag);
                EditDLNK.TeamMembers[index].Reset();
                if (!string.IsNullOrEmpty(EditDLNK.Ownship) && (EditDLNK.Ownship == (index + 1).ToString()))
                    uiOwnComboEntry.SelectedIndex = -1;
            }
            CopyEditToConfig(true);
        }

        /// <summary>
        /// on callsign combo selection changes, update the internal team members table to match the selection and
        /// update other entries as required.
        /// </summary>
        private async void TNDLComboCallsign_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            PilotComboControl comboBox = (PilotComboControl)sender;
            int index = int.Parse(comboBox.Tag as string);
            Pilot pilot = comboBox.SelectedPilot;

            if (IsRebuildingUI || (pilot == null) || (pilot == AssignedPilots[index]))
                return;

            bool isOldOwnByCallsign = (OwnshipDriverUID == EditDLNK.TeamMembers[index].DriverUID);
            bool isNewOwnByCallsign = ((pilot.UniqueID != UnknownPilot.UniqueID) &&
                                       (OwnshipDriverUID == pilot.UniqueID));

            int curPilotIndex = -1;
            if (pilot.UniqueID != UnknownPilot.UniqueID)
            {
                // figure out if the pilot we are selecting is currently in the table in a different slot so we can
                // clear that slot out prior to wrapping up the update.
                //
                for (int i = 0; i < _tableCallsignComboList.Count; i++)
                {
                    Pilot tablePilot = _tableCallsignComboList[i].SelectedPilot;
                    if ((i != index) && (tablePilot != null) && (tablePilot.UniqueID == pilot.UniqueID))
                    {
                        curPilotIndex = i;
                        break;
                    }
                }

                // if we are trying to select a pilot that is already in the table from the mission link, veto that
                // suggestion and tell the user why.
                //
                if (EditDLNK.IsLinkedMission && (curPilotIndex >= 0) && (curPilotIndex < Config.Mission.Ships))
                {
                    await Utilities.Message1BDialog(Content.XamlRoot, "Pilot Already Assigned",
                        $"{pilot.Name} is already assigned to the table according to the mission. Their position " +
                        $" cannot be changed without first unlinking datalink setup from the mission setup.");
                    IsRebuildingUI = true;
                    comboBox.SelectedPilot = AssignedPilots[index];
                    IsRebuildingUI = false;
                    return;
                }
            }

            AssignedPilots[index] = pilot;
            if (curPilotIndex != -1)
            {
                IsRebuildingUI = true;
                _tableCallsignComboList[curPilotIndex].SelectedPilot = UnknownPilot;
                IsRebuildingUI = false;
                EditDLNK.TeamMembers[curPilotIndex].Reset();
                AssignedPilots[curPilotIndex] = UnknownPilot;
            }

            if (!isOldOwnByCallsign && !isNewOwnByCallsign)
            {
                // combo selection update is not changing from or changing to the ownship: either clear the entry
                // (if we are setting the generic callsign entry), or setup the entry from the pilot database.
                //
                if (comboBox.SelectedPilot.UniqueID == UnknownPilot.UniqueID)
                {
                    EditDLNK.TeamMembers[index].Reset();
                    CopyEditToConfig(true);
                }
                else
                {
                    // rebuild enable state to avoid some visual glitches from enabled -> disabled transition with
                    // the field contents updating around the same time.
                    //
                    RebuildEnableState();
                    EditDLNK.TeamMembers[index].TDOA = false;
                    EditDLNK.TeamMembers[index].TNDL = pilot.AvionicsID;
                    EditDLNK.TeamMembers[index].DriverUID = pilot.UniqueID;
                    CopyEditToConfig(true);
                }
            }
            else if (isNewOwnByCallsign)
            {
                // combo selection update is changing to the ownship. clear any old table entry associated with the
                // callsign and update the new entry to match ownship. if there was an ownship specified explicitly
                // (via ownship combo), disable TDOA.
                //
                if (curPilotIndex != -1)
                {
                    IsRebuildingUI = true;
                    _tableCallsignComboList[curPilotIndex].SelectedPilot = UnknownPilot;
                    IsRebuildingUI = false;
                    EditDLNK.TeamMembers[curPilotIndex].Reset();
                }
                if (!isOldOwnByCallsign && !string.IsNullOrEmpty(EditDLNK.Ownship))
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship) - 1].TDOA = false;
                //
                // rebuild enable state to avoid some visual glitches from enabled -> disabled transition with
                // the field contents updating around the same time.
                //
                RebuildEnableState();
                EditDLNK.Ownship = (index + 1).ToString();
                EditDLNK.TeamMembers[index].TDOA = true;
                EditDLNK.TeamMembers[index].TNDL = pilot.AvionicsID;
                EditDLNK.TeamMembers[index].DriverUID = pilot.UniqueID;
                CopyEditToConfig(true);
            }
            else if (isOldOwnByCallsign)
            {
                // we are making an edit that removes an implicitly defined (ie, through callsign) ownship in the
                // team member table. update the entry and clear the ownship.
                //
                // rebuild enable state to avoid some visual glitches from enabled -> disabled transition with
                // the field contents updating around the same time.
                //
                RebuildEnableState();
                EditDLNK.Ownship = "";
                EditDLNK.TeamMembers[index].TDOA = false;
                EditDLNK.TeamMembers[index].TNDL = (pilot != null) ? pilot.AvionicsID : "";
                EditDLNK.TeamMembers[index].DriverUID = (pilot != null) ? pilot.UniqueID : "";
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLBtnSwap_Click(object sender, RoutedEventArgs args)
        {
            for (int i = 0; i < 4; i++)
            {
                TeamMember tmL = (EditDLNK.TeamMembers[i].HasErrors) ? new TeamMember()
                                                                     : new TeamMember(EditDLNK.TeamMembers[i]);
                TeamMember tmR = (EditDLNK.TeamMembers[i + 4].HasErrors) ? new TeamMember()
                                                                         : new TeamMember(EditDLNK.TeamMembers[i + 4]);

                // copy individual fields to make sure property changed events get posted...
                //
                EditDLNK.TeamMembers[i].TDOA = tmR.TDOA;
                EditDLNK.TeamMembers[i].TNDL = tmR.TNDL;
                EditDLNK.TeamMembers[i].DriverUID = tmR.DriverUID;

                EditDLNK.TeamMembers[i + 4].TDOA = tmL.TDOA;
                EditDLNK.TeamMembers[i + 4].TNDL = tmL.TNDL;
                EditDLNK.TeamMembers[i + 4].DriverUID = tmL.DriverUID;

                (AssignedPilots[i + 4], AssignedPilots[i]) = (AssignedPilots[i], AssignedPilots[i + 4]);
            }
            CopyEditToConfig(true);

            ForceSyncUI();
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on configuration saved, rebuild the interface to ensure ui state is consistent with confiugration.
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.SyncSysTag))
                CopyConfigToEdit();

            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to this page, set up internal and ui state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F16CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, DLNKSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            SetupMissionLinkage(EditDLNK.IsLinkedMission);
            ForceSyncUI();
            RebuildInterfaceState();

            CopyEditToConfig(true);

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down internal and ui state. primarily, need to stop hooking config
        /// saved events as the configuration may outlive us.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
