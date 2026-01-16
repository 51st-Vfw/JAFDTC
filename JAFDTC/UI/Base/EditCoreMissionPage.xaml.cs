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
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ui-facing bindings object used to martial between the configuration and the interface. this class does
        /// some thunking between singular elements in the ui (because bindings to array elements are sus) and array
        /// elements in the configuration along with allowing 2-way bindings.
        /// </summary>
        private sealed partial class UIBinds : BindableObject
        {
            // ---- properties

            private Pilot _pilotD1;
            public Pilot PilotD1
            {
                get => _pilotD1;
                set => SetProperty(ref _pilotD1, value);
            }

            private Pilot _pilotD2;
            public Pilot PilotD2
            {
                get => _pilotD2;
                set => SetProperty(ref _pilotD2, value);
            }

            private Pilot _pilotD3;
            public Pilot PilotD3
            {
                get => _pilotD3;
                set => SetProperty(ref _pilotD3, value);
            }

            private Pilot _pilotD4;
            public Pilot PilotD4
            {
                get => _pilotD4;
                set => SetProperty(ref _pilotD4, value);
            }

            private string _loadoutD1;
            public string LoadoutD1
            {
                get => _loadoutD1;
                set => SetProperty(ref _loadoutD1, value);
            }

            private string _loadoutD2;
            public string LoadoutD2
            {
                get => _loadoutD2;
                set => SetProperty(ref _loadoutD2, value);
            }

            private string _loadoutD3;
            public string LoadoutD3
            {
                get => _loadoutD3;
                set => SetProperty(ref _loadoutD3, value);
            }

            private string _loadoutD4;
            public string LoadoutD4
            {
                get => _loadoutD4;
                set => SetProperty(ref _loadoutD4, value);
            }

            private bool? _customLoadoutD2;
            public bool? CustomLoadoutD2
            {
                get => _customLoadoutD2;
                set => SetProperty(ref _customLoadoutD2, value);
            }

            private bool? _customLoadoutD3;
            public bool? CustomLoadoutD3
            {
                get => _customLoadoutD3;
                set => SetProperty(ref _customLoadoutD3, value);
            }

            private bool? _customLoadoutD4;
            public bool? CustomLoadoutD4
            {
                get => _customLoadoutD4;
                set => SetProperty(ref _customLoadoutD4, value);
            }

            // ---- computed properties

            public IReadOnlyList<Pilot> AssignedPilots
            {
                get => [ PilotD1, PilotD2, PilotD3, PilotD4 ];
                set
                {
                    PilotD1 = value[0]; PilotD2 = value[1]; PilotD3 = value[2]; PilotD4 = value[3];
                }
            }

            public IReadOnlyList<string> Loadouts
            {
                get => [ LoadoutD1, LoadoutD2, LoadoutD3, LoadoutD4 ];
                set
                {
                    LoadoutD1 = value[0]; LoadoutD2 = value[1]; LoadoutD3 = value[2]; LoadoutD4 = value[3];
                }
            }

            public IReadOnlyList<bool?> CustomLoadouts
            {
                get => [ null, CustomLoadoutD2, CustomLoadoutD3, CustomLoadoutD4 ];
                set
                {
                    CustomLoadoutD2 = value[1]; CustomLoadoutD3 = value[2]; CustomLoadoutD4 = value[3];
                }
            }

            // ---- methods

            public void SetAssignedPilot(int index, Pilot value)
            {
                switch(index)
                {
                    case 0: PilotD1 = value; break;
                    case 1: PilotD2 = value; break;
                    case 2: PilotD3 = value; break;
                    case 3: PilotD4 = value; break;
                    default: break;
                }
            }

            public void SetLoadout(int index, string value)
            {
                switch (index)
                {
                    case 0: LoadoutD1 = value; break;
                    case 1: LoadoutD2 = value; break;
                    case 2: LoadoutD3 = value; break;
                    case 3: LoadoutD4 = value; break;
                    default: break;
                }
            }

            public void SetCustomLoadout(int index, bool value)
            {
                switch (index)
                {
                    case 1: CustomLoadoutD2 = value; break;
                    case 2: CustomLoadoutD3 = value; break;
                    case 3: CustomLoadoutD4 = value; break;
                    default: break;
                }
            }
        }

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

        // ---- internal properties

        private IEditCoreMissionPageHelper PageHelper { get; set; }

        private bool IsIgnoringSelection { get; set; }

        private string OwnshipCallsign { get; set; }

        private IReadOnlyList<Pilot> AvailablePilots { get; set; }

        private int ShipsUI
        {
            get => EditMsn.Ships - 1;
            set => EditMsn.Ships = value + 1;
        }

        private UIBinds BindsUI { get; set; }

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

            BindsUI = new UIBinds
            {
                AssignedPilots = [ PilotComboControl.UnassignedPilot, PilotComboControl.UnassignedPilot,
                                   PilotComboControl.UnassignedPilot, PilotComboControl.UnassignedPilot ],
                Loadouts = [ string.Empty, string.Empty, string.Empty, string.Empty ],
                CustomLoadouts = [ new bool?(true), new bool?(false), new bool?(false), new bool?(false) ]
            };

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

                // copy values from the edit configuration to class properties that tie to the ui to keep the ui
                // in sync with the config and edit state. ignore any "selection update" events while doing this.
                //
                IsIgnoringSelection = true;
                for (int i = 0; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                {
                    BindsUI.SetAssignedPilot(i, PilotDbase.Instance.Find(EditMsn.PilotUIDs[i])
                                                ?? PilotComboControl.UnassignedPilot);
                    BindsUI.SetLoadout(i, (!string.IsNullOrEmpty(EditMsn.Loadouts[i])) ? EditMsn.Loadouts[i]
                                                                                       : string.Empty);
                    BindsUI.SetCustomLoadout(i, !string.IsNullOrEmpty(EditMsn.Loadouts[i]));
                }
                IsIgnoringSelection = false;
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
                IReadOnlyList<Pilot> pilots = BindsUI.AssignedPilots;
                IReadOnlyList<string> loadouts = BindsUI.Loadouts;
                for (int i = 0; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                {
                    EditMsn.PilotUIDs[i] = (pilots[i].UniqueID != PilotComboControl.UnassignedPilot.UniqueID)
                        ? pilots[i].UniqueID : null;
                    EditMsn.Loadouts[i] = (!string.IsNullOrEmpty(loadouts[i])) ? loadouts[i] : null;
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
                    BindsUI.SetLoadout(shipIndex, string.Empty);
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
                bool isCustom = BindsUI.CustomLoadouts[i].GetValueOrDefault(false);
                _loadValues[i].Visibility = (isCustom) ? Visibility.Visible : Visibility.Collapsed;
                _loadLinks[i].Visibility = (isCustom) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// rebuild the control enable state that is not handled by SystemEditorPageBase
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(CoreMissionSystem.SystemTag));

            Utilities.SetEnableState(uiCmbFlightShips, isEditable);
            foreach (PilotComboControl combo in _shipPilots)
                Utilities.SetEnableState(combo, isEditable);
            foreach (TextBox tbox in _loadValues)
                Utilities.SetEnableState(tbox, isEditable);
            foreach (ToggleButton button in _loadButtons)
                Utilities.SetEnableState(button, isEditable);
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildEnableState();
            RebuildEditPanelCallsigns();
            for (int i = 1; i < CoreMissionSystem.NUM_SHIPS_IN_FLIGHT; i++)
                RebuildEditPanelLoadoutEditor(i);
            RebuildEditPanelLinkText();
        }

        /// <summary>
        /// reset to defaults. check all of the systems buttons.
        /// </summary>
        protected override void ResetConfigToDefault()
        {
            SystemConfig.Reset();
            CopyConfigToEditState();

            foreach (ToggleButton button in _loadButtons)
                button?.IsChecked = false;
            uiCmbFlightShips.SelectedIndex = ShipsUI;
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
                BindsUI.SetLoadout(index, string.Empty);

            SaveEditStateToConfig();
        }

        // ---- combo boxes -------------------------------------------------------------------------------------------

        /// <summary>
        /// flight ship count combo changes: on changes to the ship count in the flight, hide or show edit panels
        /// based on the change. hidden panels have their corresponding properties (pilot, loadout) set to null.
        /// </summary>
        private void CmbFlightShips_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (IsIgnoringSelection)
                return;

            for (int i = 0; i < _shipEditorPanels.Count; i++)
            {
                if (i > uiCmbFlightShips.SelectedIndex)
                {
                    _shipEditorPanels[i].Visibility = Visibility.Collapsed;

                    _loadButtons[i].IsChecked = false;

                    BindsUI.SetAssignedPilot(i, PilotComboControl.UnassignedPilot);
                    BindsUI.SetLoadout(i, string.Empty);
                    BindsUI.SetCustomLoadout(i, false);
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
            if (IsIgnoringSelection)
                return;

            PilotComboControl cbox = sender as PilotComboControl;
            Pilot selectedPilot = cbox.SelectedPilot;
            int shipIndex = int.Parse(cbox.Tag as string);
            if (selectedPilot != null)
            {
                // update selected pilot and reconfigure the edit panel as needed.
                //
                IReadOnlyList<Pilot> assignedPilots = BindsUI.AssignedPilots;
                ConfigureEditPanelForPilotChange(shipIndex, assignedPilots[shipIndex].UniqueID, selectedPilot.UniqueID);

                // check to see if we are changing to a pilot that is currently assigned to a different ship in the
                // flight. if so, we will set the other ship to an unassigned pilot.
                //
                int matchIndex = -1;
                for (int i = 0; i < assignedPilots.Count; i++)
                    if ((i != shipIndex) &&
                        (assignedPilots[i].UniqueID != PilotComboControl.UnassignedPilot.UniqueID) &&
                        (assignedPilots[i].UniqueID == selectedPilot.UniqueID))
                    {
                        matchIndex = i;
                        break;
                    }
                if (matchIndex != -1)
                {
                    BindsUI.SetAssignedPilot(matchIndex, PilotComboControl.UnassignedPilot);
                    BindsUI.SetLoadout(matchIndex, string.Empty);
                    BindsUI.SetCustomLoadout(matchIndex, false);
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
            BindsUI.SetLoadout(int.Parse(tbox.Tag as string), tbox.Text);

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
