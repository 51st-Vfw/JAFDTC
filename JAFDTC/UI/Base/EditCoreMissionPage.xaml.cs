// ********************************************************************************************************************
//
// EditCoreMissionPage.cs : ui c# for for general mission editor page
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

using JAFDTC.Core.Extensions;
using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.Pilots;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// page to edit mission fields. this is a general-purpose class that is instatiated in combination with an
    /// IEditCoreMissionPageHelper class to provide airframe-specific specialization.
    /// </summary>
    public sealed partial class EditCoreMissionPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPageBase properties

        protected override SystemBase SystemConfig => PageHelper.GetSystemConfig(Config);

        protected override string SystemTag => CoreMissionSystem.SystemTag;

        protected override string SystemName => "Mission";

        protected override bool IsPageStateDefault => EditMsn.IsDefault;

        // ---- public properties

        public string OwnshipCallsign { get; set; }

        public IReadOnlyList<Pilot> AvailablePilots { get; set; }

        public int ShipsUI
        {
            get => EditMsn.Ships - 1;
            set => EditMsn.Ships = value + 1;
        }

        public ObservableCollection<Pilot> AssignedPilots { get; set; } = [ ];

        public ObservableCollection<string> Loadouts { get; set; } = [ ];

        public ObservableCollection<bool?> IsLoadoutCustom { get; set; } = [ ];

        // ---- internal properties

        private IEditCoreMissionPageHelper PageHelper { get; set; }

        // ---- read-only properties

        private readonly CoreMissionSystem EditMsn;

        private readonly List<StackPanel> _shipEditorPanels;
        private readonly List<List<UIElement>> _shipEditorPanelElements;
        private readonly List<TextBlock> _shipCallsigns;
        private readonly List<PilotComboControl> _shipPilots;
        private readonly List<TextBox> _loadValues;
        private readonly List<TextBlock> _loadLinks;
        private readonly List<ToggleButton> _loadButtons;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditCoreMissionPage()
        {
            EditMsn = new();
            EditMsn.PropertyChanged += EditMsn_PropertyChanged;

            // set these to valid, but "empty" objects. we will update in-place as things change.
            //
            for (int i = 0; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
            {
                AssignedPilots.Add(PilotComboControl.UnassignedPilot);
                Loadouts.Add(string.Empty);
                IsLoadoutCustom.Add(new bool?(false));
            }

            InitializeComponent();
            InitializeBase(EditMsn, uiTxtFlightCallsign, uiCtlLinkResetBtns, [ "Name", "Callsign", "Tasking" ]);

            _shipEditorPanels = [ null, uiPnlEditDash2, uiPnlEditDash3, uiPnlEditDash4 ];
            _shipEditorPanelElements = [
                [ uiLblLoadDash1, uiTxtLoadDash1 ],
                [ uiLblLoadDash2, uiTxtLoadDash2, uiLblLoadLinkDash2, uiBtnEditLoadDash2 ],
                [ uiLblLoadDash3, uiTxtLoadDash3, uiLblLoadLinkDash3, uiBtnEditLoadDash3 ],
                [ uiLblLoadDash4, uiTxtLoadDash4, uiLblLoadLinkDash4, uiBtnEditLoadDash4 ]
            ];
            _shipCallsigns = [ uiLblShipCallsignDash1, uiLblShipCallsignDash2, uiLblShipCallsignDash3, uiLblShipCallsignDash4 ];

            _shipPilots = [ uiCmbPilotDash1, uiCmbPilotDash2, uiCmbPilotDash3, uiCmbPilotDash4 ];
            _loadValues = [ uiTxtLoadDash1, uiTxtLoadDash2, uiTxtLoadDash3, uiTxtLoadDash4 ];
            _loadLinks = [ null, uiLblLoadLinkDash2, uiLblLoadLinkDash3, uiLblLoadLinkDash4 ];
            _loadButtons = [ null, uiBtnEditLoadDash2, uiBtnEditLoadDash3, uiBtnEditLoadDash4 ];
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void EditMsn_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(EditMsn.Callsign))
                UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between the dtc configuration and our local dtc state.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                PageHelper.CopyConfigToEdit(Config, EditMsn);
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit);

                // copy values from the edit configuration to class properties that tie to the ui to keep the ui in
                // sync with the configuration and edit state.
                //
                for (int i = 0; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                {
                    Pilot pilot = PilotDbase.Instance.Find(EditMsn.PilotUIDs[i]);
                    AssignedPilots[i] = (pilot != null) ? pilot : PilotComboControl.UnassignedPilot;
                    Loadouts[i] = (!string.IsNullOrEmpty(EditMsn.Loadouts[i])) ? EditMsn.Loadouts[i] : string.Empty;
                    IsLoadoutCustom[i] = (!string.IsNullOrEmpty(EditMsn.Loadouts[i]));
                }
            }
            UpdateUIFromEditState();
        }

        /// <summary>
        /// marshall data between our local dtc state and the dtc configuration.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if ((EditState != null) && !IsUIRebuilding)
            {
                // copy values from the class properties that tie to the ui to the edit configuration keep the
                // configuration and edit state in sync with the ui.
                //
                for (int i = 0; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                {
                    EditMsn.PilotUIDs[i] = (AssignedPilots[i].UniqueID != PilotComboControl.UnassignedPilot.UniqueID)
                        ? AssignedPilots[i].UniqueID : null;
                    EditMsn.Loadouts[i] = (!string.IsNullOrEmpty(Loadouts[i])) ? Loadouts[i] : null;
                }

                PageHelper.CopyEditToConfig(EditMsn, Config);
                CopyAllSettings(SettingLocation.Edit, SettingLocation.Config, true);
                Config.Save(this, CoreSimDTCSystem.SystemTag);
            }
            UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure the elements in an edit panel for changes to the pilot. reconfiguration happens when the
        /// pilot is changing from or to unassigned as these transitions cause visibility changes in edit panel.
        /// </summary>
        private void ConfigureEditPanelForPilotChange(int shipIndex, string curUID, string newUID)
        {
            if ((curUID != newUID) && ((curUID == PilotComboControl.UnassignedPilot.UniqueID) ||
                                       (newUID == PilotComboControl.UnassignedPilot.UniqueID)))
            {
                Visibility visibility = (curUID == PilotComboControl.UnassignedPilot.UniqueID) ? Visibility.Visible
                                                                                               : Visibility.Collapsed;
                foreach (UIElement elem in _shipEditorPanelElements[shipIndex])
                    elem.Visibility = visibility;
                if (curUID == PilotComboControl.UnassignedPilot.UniqueID)
                {
                    Loadouts[shipIndex] = string.Empty;
                    RebuildEditPanelLoadoutEditor(shipIndex);
                }
            }
        }

        /// <summary>
        /// rebuilds the per-pilot/ship abbreviated callsigns.
        /// </summary>
        private void RebuildEditPanelCallsigns()
        {
            string callsignBase = EditMsn.Callsign.ToShortCallsign();
            for (int i = 0; i < _shipCallsigns.Count; i++)
                _shipCallsigns[i].Text = $"{callsignBase}-{i + 1}";
        }

        /// <summary>
        /// rebuilds the static text in the text element that indicates a non-custom loadout for a given ship.
        /// </summary>
        private void RebuildEditPanelLinkText()
        {
            _loadLinks[1].Text = $"Uses same loadout as {EditMsn.Callsign.ToUpper()}-1";
            _loadLinks[2].Text = $"Uses same loadout as {EditMsn.Callsign.ToUpper()}-1";
            if (_loadButtons[2].IsChecked.GetValueOrDefault(false))
                _loadLinks[3].Text = $"Uses same loadout as {EditMsn.Callsign.ToUpper()}-3";
            else
                _loadLinks[3].Text = $"Uses same loadout as {EditMsn.Callsign.ToUpper()}-1";
        }

        /// <summary>
        /// rebuilds the loadout editor fields for the ship at the indicated index. this either shows the editable
        /// custom loadout field or the static link field based on IsLoadoutCustom state.
        /// </summary>
        private void RebuildEditPanelLoadoutEditor(int i)
        {
            if (i > 0)
            {
                bool isCustom = IsLoadoutCustom[i].GetValueOrDefault(false);
                _loadValues[i].Visibility = (isCustom) ? Visibility.Visible : Visibility.Collapsed;
                _loadLinks[i].Visibility = (isCustom) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildEditPanelCallsigns();
            RebuildEditPanelLinkText();
            for (int i = 1; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                RebuildEditPanelLoadoutEditor(i);
        }

        /// <summary>
        /// reset to defaults. check all of the systems buttons.
        /// </summary>
        protected override void ResetConfigToDefault()
        {
            SystemConfig.Reset();
            CopyConfigToEditState();

            uiCmbFlightShips.SelectedIndex = ShipsUI;
            foreach (PilotComboControl combo in _shipPilots)
                combo.SelectedPilot = PilotComboControl.UnassignedPilot;
            foreach (ToggleButton button in _loadButtons)
                button?.IsChecked = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on click on edit button, update the internal loadout state based on state of the button.
        /// </summary>
        private void BtnEditLoad_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton button = sender as ToggleButton;
            int index = int.Parse(button.Tag as string);
            bool isChecked = button.IsChecked.GetValueOrDefault(false);
            if (!isChecked)
                Loadouts[index] = string.Empty;
            IsLoadoutCustom[index] = isChecked;

            SaveEditStateToConfig();
        }

        // ---- combo boxes -------------------------------------------------------------------------------------------

        /// <summary>
        /// flight ship count combo changes: on changes to the ship count in the flight, hide or show edit panels
        /// based on the change. hidden panels have their corresponding properties (pilot, loadout) set to null.
        /// </summary>
        private void CmbFlightShips_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Debug.WriteLine($"{uiCmbFlightShips.SelectedIndex}");
            for (int i = 0; i < _shipEditorPanels.Count; i++)
            {
                if (i > uiCmbFlightShips.SelectedIndex)
                {
                    _shipEditorPanels[i].Visibility = Visibility.Collapsed;

                    _shipPilots[i].SelectedPilot = PilotComboControl.UnassignedPilot;
                    _loadButtons[i].IsChecked = false;

                    AssignedPilots[i] = PilotComboControl.UnassignedPilot;
                    Loadouts[i] = string.Empty;
                    IsLoadoutCustom[i] = false;
                }
                else
                {
                    _shipEditorPanels[i]?.Visibility = Visibility.Visible;
                }
            }
            SaveEditStateToConfig();
        }

        /// <summary>
        /// pilot combo changes: reconfigure the edit panel for the change in pilot.
        /// </summary>
        private void CmbPilot_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            PilotComboControl cbox = sender as PilotComboControl;
            Pilot selectedPilot = cbox.SelectedPilot;
            int shipIndex = int.Parse(cbox.Tag as string);
            if (selectedPilot != null)
            {
                // update selected pilot and reconfigure the edit panel as needed.
                //
                ConfigureEditPanelForPilotChange(shipIndex, AssignedPilots[shipIndex].UniqueID, selectedPilot.UniqueID);
                AssignedPilots[shipIndex] = selectedPilot;

                // check to see if we are changing to a pilot that is currently assigned to a different ship in the
                // flight. if so, we will set the other ship to an unassigned pilot.
                //
                int matchIndex = -1;
                for (int i = 0; i < AssignedPilots.Count; i++)
                    if ((i != shipIndex) &&
                        (AssignedPilots[i].UniqueID != PilotComboControl.UnassignedPilot.UniqueID) &&
                        (AssignedPilots[i].UniqueID == selectedPilot.UniqueID))
                    {
                        matchIndex = i;
                        break;
                    }
                if (matchIndex != -1)
                {
                    AssignedPilots[matchIndex] = PilotComboControl.UnassignedPilot;
                    _shipPilots[matchIndex].SelectedPilot = AssignedPilots[matchIndex];
                    Loadouts[matchIndex] = string.Empty;
                    IsLoadoutCustom[matchIndex] = false;
                }
                SaveEditStateToConfig();
            }
        }

        // ---- text fields -------------------------------------------------------------------------------------------

        /// <summary>
        /// on loadout field loosing focus, copy out the text box element state and persist current edit state to the
        /// configuration.
        /// </summary>
        private void LoadoutField_LostFocus(object sender, RoutedEventArgs args)
        {
            // HACK: 100% uncut cya. as the app is shutting down we can get lost focus events that may try to
            // HACK: operate on ui that has been torn down. in that case, return without doing anything.
            // HACK: this potentially prevents persisting changes made to the control prior to focus loss.
            //
            if ((Application.Current as JAFDTC.App).IsAppShuttingDown)
                return;

            TextBox tbox = sender as TextBox;
            int index = int.Parse(tbox.Tag as string);
            Loadouts[index] = tbox.Text;

            SaveEditStateToConfig();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// 
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditCoreMissionPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);

            OwnshipCallsign = Settings.Callsign;

            PilotDbaseQuery query = new()
            {
                Airframes = [ PageHelper.Airframe ]
            };
            List<Pilot> pilots = [.. PilotDbase.Instance.Find(query, true) ];
            pilots.Insert(0, PilotComboControl.UnassignedPilot);
            AvailablePilots = pilots;

            base.OnNavigatedTo(args);

            CopyConfigToEditState();
        }
    }
}
