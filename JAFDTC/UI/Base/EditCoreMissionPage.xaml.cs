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
using JAFDTC.File;
using JAFDTC.File.Models;
using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.Pilots;
using JAFDTC.Models.Threats;
using JAFDTC.Models.Units;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// page to edit mission fields. this is a general-purpose class that is instatiated in combination with an
    /// IEditCoreMissionPageHelper class to provide airframe-specific specialization.
    /// </summary>
    public sealed partial class EditCoreMissionPage : SystemEditorPageBase,
                                                      IMapControlVerbHandler, IMapControlMarkerExplainer
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

        private MapWindow _mapWindow;
        private MapWindow MapWindow
        {
            get => _mapWindow;
            set
            {
                if (_mapWindow != value)
                {
                    _mapWindow = value;
                    VerbMirror = value;
                }
            }
        }

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
                Config.Save(this, SystemTag);
            }
            UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build out the data source and so on necessary for the map window and create it. if there are currently
        /// no steerpoints defined, we will prompt for a theater and add a single steerpoint to start us off.
        /// </summary>
        private async void CoreOpenMap()
        {
            if (PageHelper.NumNavpoints(Config) == 0)
            {
                // no navpoints: prompt for a theater then locate the new navpoint in the center of the area for
                // that theater.
                //
                GetListDialog theaterDialog = new(Theater.Theaters, "Theater", 0, 0)
                {
                    XamlRoot = Content.XamlRoot,
                    Title = $"Select a Theater for the Mission",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await theaterDialog.ShowAsync(ContentDialogPlacement.Popup);
                if (result != ContentDialogResult.Primary)
                    return;                                     // EXIT: cancelled, no change...

                TheaterInfo info = Theater.TheaterInfo[theaterDialog.SelectedItem];
                double lat = info.LatMin + ((info.LatMax - info.LatMin) / 2.0);
                double lon = info.LonMin + ((info.LonMax - info.LonMin) / 2.0);

                PageHelper.AddNavpoint(Config, PageHelper.NavptSystemInfo.RouteNames[0], 0, $"{lat:F10}", $"{lon:F10}");
                Config.Save(this, SystemTag);
                NavArgs.ConfigPage.ForceSystemListIconRebuild(PageHelper.NavptSystemInfo.SystemTag);
            }

            // configure and open the map window with the appropriate content.
            //
            bool isLinked = !string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.NavptSystemInfo.SystemTag));
            MapMarkerInfo.MarkerTypeMask editMask = ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.NAV_PT) |
                                                    ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.PATH_EDIT_HANDLE);
            MapMarkerInfo.MarkerTypeMask openMask = MapMarkerInfo.MarkerTypeMask.NAV_PT;

            Dictionary<string, List<INavpointInfo>> routes = PageHelper.GetAllNavpoints(Config);

            MapWindow = NavpointUIHelper.OpenMap(this, PageHelper.NavptSystemInfo.NavptMaxCount,
                                                 PageHelper.NavptSystemInfo.NavptCoordFmt, openMask, editMask, routes,
                                                 EditMsn.Threats, Config.LastMapMarkerImport, Config.LastMapFilter);
            MapWindow.MarkerExplainer = this;
            MapWindow.Closed += MapWindow_Closed;

            NavArgs.ConfigPage.RegisterAuxWindow(MapWindow);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the threat summary string to display in the ui based on the threats in the editor.
        /// </summary>
        private string ThreatSummaryString()
        {
            int numREDFOR = 0;
            int numBLUEFOR = 0;
            foreach (JAFDTC.Models.Planning.Threat threat in EditMsn.Threats)
                if (threat.Coalition == Models.Core.CoalitionType.RED)
                    numREDFOR++;
                else if (threat.Coalition == Models.Core.CoalitionType.BLUE)
                    numBLUEFOR++;

            string name = Path.GetFileNameWithoutExtension(EditMsn.ThreatSource);
            string rfCnt = (numREDFOR == 0) ? "no" : $"{numREDFOR}";
            string rfSfx = (numREDFOR == 1) ? "" : "s";
            string bfCnt = (numBLUEFOR == 0) ? "no" : $"{numBLUEFOR}";
            string bfSfx = (numBLUEFOR == 1) ? "" : "s";
            return $"{name} contains {rfCnt} REDFOR element{rfSfx} and {bfCnt} BLUEFOR element{bfSfx}";
        }

        /// <summary>
        /// import threats according to the import specification and add them to the mission threats (clearing any
        /// currently defined threats first). throws an exception on issues.
        /// </summary>
        void CoreImportThreats(MapImportSpec importSpec)
        {
            // build extractor and import groups/units from the file specified in the import specification.
            //
            IExtractor extractor = Path.GetExtension(importSpec.Path).ToLower() switch
            {
                ".acmi" => new JAFDTC.File.ACMI.Extractor(),
                ".cf" => new JAFDTC.File.CF.Extractor(),
                ".miz" => new JAFDTC.File.MIZ.Extractor(),
                _ => (IExtractor)null
            } ?? throw new Exception($"File type “{Path.GetExtension(importSpec.Path)}” is not supported.");

            ExtractCriteria criteria = new()
            {
                FilePath = importSpec.Path,
// TODO: do we want a theater specification in the mission to put here?
                Theater = null,
                UnitCategories = [ UnitCategoryType.GROUND, UnitCategoryType.NAVAL ],
                IsAlive = (importSpec.IsAliveOnly) ? true : null
            };
            if (importSpec.IsEnemyOnly && (importSpec.FriendlyCoalition == CoalitionType.BLUE))
                criteria.Coalitions = [ CoalitionType.RED ];
            else if (importSpec.IsEnemyOnly && (importSpec.FriendlyCoalition == CoalitionType.RED))
                criteria.Coalitions = [ CoalitionType.BLUE ];
            IReadOnlyList<UnitGroupItem> groups = extractor.Extract(criteria)
                ?? throw new Exception($"Encountered an error while importing from path\n\n{importSpec.Path}");

            FileManager.Log($"EditCoreMissionPage:CoreImportThreats extracted {groups.Count} groups from {importSpec.Path}");

            // add the threats to the threat list. this includes a threat for each unit along with a threat region
            // for groups of units (per-unit threats are left out if import specification asks for summary only).
            //
            EditMsn.Threats.Clear();
            foreach (UnitGroupItem group in groups)
            {
                string threatType = null;
                double threatRadius = 0.0;
                double avgLat = 0.0;
                double avgLon = 0.0;
                foreach (UnitItem unit in group.Units)
                {
                    ThreatDbaseQuery query = new([ unit.Type ], null, null, null, null, true);
                    IReadOnlyList<Threat> dbThreats = ThreatDbase.Instance.Find(query);
                    Threat dbThreat = ((dbThreats != null) && (dbThreats.Count > 0)) ? dbThreats[0] : null;
                    if ((dbThreat != null) && (dbThreat.RadiusWEZ > threatRadius))
                    {
                        threatType = dbThreat.Name;
                        threatRadius = dbThreat.RadiusWEZ;
                    }
                    avgLat += unit.Position.Latitude;
                    avgLon += unit.Position.Longitude;

                    if (!importSpec.IsSummaryOnly)
                    {
                        EditMsn.Threats.Add(new()
                        {
                            Coalition = group.Coalition,
                            Name = unit.Name,
                            Type = unit.Type,
                            Location = new()
                            {
                                Latitude = $"{unit.Position.Latitude:F10}",
                                Longitude = $"{unit.Position.Longitude:F10}",
                                Altitude = $"{unit.Position.Altitude}"
                            },
                            WEZ = (dbThreat != null) ? dbThreat.RadiusWEZ : 0.0
                        });
                    }
                }
                avgLat /= (double)group.Units.Count;
                avgLon /= (double)group.Units.Count;

                if (threatRadius > 0.0)
                {
                    EditMsn.Threats.Add(new()
                    {
                        Coalition = group.Coalition,
                        Name = $"{group.Name}: {threatType} WEZ",
                        Type = null,
                        Location = new()
                        {
                            Latitude = $"{avgLat:F10}",
                            Longitude = $"{avgLon:F10}",
                            Altitude = $"{group.Units[0].Position.Altitude}"
                        },
                        WEZ = threatRadius
                    });
                }
            }
            FileManager.Log($"EditCoreMissionPage:CoreImportThreats added {EditMsn.Threats.Count} threats to mission");
        }

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
            _loadLinks[1].Text = $"Carries same loadout as {EditMsn.Callsign.ToUpper()}-1";
            _loadLinks[2].Text = $"Carries same loadout as {EditMsn.Callsign.ToUpper()}-1";
            if (_loadButtons[2].IsChecked.GetValueOrDefault(false))
                _loadLinks[3].Text = $"Carries same loadout as {EditMsn.Callsign.ToUpper()}-3";
            else
                _loadLinks[3].Text = $"Carries same loadout as {EditMsn.Callsign.ToUpper()}-1";
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
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(SystemTag));

            Utilities.SetEnableState(uiCmbFlightShips, isEditable);
            foreach (PilotComboControl combo in _shipPilots)
                Utilities.SetEnableState(combo, isEditable);
            foreach (TextBox tbox in _loadValues)
                Utilities.SetEnableState(tbox, isEditable);
            foreach (ToggleButton button in _loadButtons)
                Utilities.SetEnableState(button, isEditable);

            uiTextThreats.Visibility = (EditMsn.Threats.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

            Utilities.SetEnableState(uiBtnClearThreats, (EditMsn.Threats.Count > 0));
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

        // ---- map setup ---------------------------------------------------------------------------------------------

        /// <summary>
        /// map command: if the map window is not currently open, build out the data source and so on necessary for
        /// the window and create it. otherwise, activate the window.
        /// </summary>
        private void BtnMap_Click(object sender, RoutedEventArgs args)
        {
            if (MapWindow == null)
                CoreOpenMap();
            else
                MapWindow.Activate();
        }

        // ---- loadout setup -----------------------------------------------------------------------------------------

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

        // ---- threat setup ------------------------------------------------------------------------------------------

        /// <summary>
        /// on threat set click, select a file to import from along with the specification of the infomation to
        /// gather and pull the threats from the file using the correct extractor. update the edit object and
        /// persist.
        /// </summary>
        public async void BtnSetThreats_Click(object sender, RoutedEventArgs args)
        {
            // if there are already threats defined, ask if its ok to clear them out as we can only have one set
            // imported at a time.
            //
            ContentDialogResult result;
            if (EditMsn.Threats.Count > 0)
            {
                result = await Utilities.Message2BDialog(Content.XamlRoot, "Are You Sure?",
                            "Do you want to change the current threat environment?");
                if (result == ContentDialogResult.None)
                    return;                                     // **** EXITS: cancel
            }

            // select file to import threats from with FileOpenPicker.
            //
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Import Threats",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".acmi");
            picker.FileTypeFilter.Add(".cf");
            picker.FileTypeFilter.Add(".miz");

            PickFileResult resultPick = await picker.PickSingleFileAsync();
            if (resultPick == null)
                return;                                         // **** EXITS: cancel

            // set up parameters for the import with ImportParamsThreatDialog.
            //
            MapImportSpec importSpec = new()
            {
                Path = resultPick.Path
            };
            ImportParamsThreatDialog setupDialog = new(importSpec)
            {
                XamlRoot = Content.XamlRoot
            };
            result = await setupDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result != ContentDialogResult.Primary)
                return;                                         // **** EXITS: cancel

            // now that we have parameters, try to do the import using the specification in the setup dialog.
            //
            try
            {
                EditMsn.ThreatSource = resultPick.Path;
                CoreImportThreats(setupDialog.Spec);
                uiTextThreats.Text = ThreatSummaryString();
                SaveEditStateToConfig();
            }
            catch (Exception ex)
            {
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", ex.Message);
            }
        }

        /// <summary>
        /// on threat clear click, clear out threats in edit object and persist.
        /// </summary>
        public async void BtnClearThreats_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot, "Are You Sure?",
                "Do you want to clear the current threat environment?");
            if (result == ContentDialogResult.None)
                return;                                     // **** EXITS: cancel

            EditMsn.ThreatSource = "";
            EditMsn.Threats.Clear();
            SaveEditStateToConfig();
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
        // IMapControlMarkerExplainer
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the display type of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayType(MapMarkerInfo info)
        {
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? PageHelper.NavptSystemInfo.NavptName
                                                                  : NavpointUIHelper.MarkerDisplayType(info);
        }

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = PageHelper.GetNavpoint(Config, info.TagStr, info.TagInt - 1).Name;
                if (string.IsNullOrEmpty(name))
                    name = $"{SystemName} {info.TagInt}";
                return name;
            }
            return NavpointUIHelper.MarkerDisplayName(info);
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = PageHelper.GetNavpoint(Config, info.TagStr, info.TagInt - 1).Alt;
                if (string.IsNullOrEmpty(elev))
                    elev = "0";
                return $"{elev}{units}";
            }
            return NavpointUIHelper.MarkerDisplayElevation(info, units);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "EditCoreMissionPage";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"ECMP:VerbMarkerSelected({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"ECMP:MarkerOpen({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"ECMP:VerbMarkerMoved({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == PageHelper.NavptSystemInfo.RouteNames[0])
            {
                PageHelper.MoveNavpoint(Config, info.TagStr, info.TagInt - 1, info.Lat, info.Lon);
                Config.Save(this, SystemTag);
                NavArgs.ConfigPage.ForceSystemListIconRebuild(PageHelper.NavptSystemInfo.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"ECMP:VerbMarkerAdded({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == PageHelper.NavptSystemInfo.RouteNames[0])
            {
                PageHelper.AddNavpoint(Config, info.TagStr, info.TagInt - 1, info.Lat, info.Lon);
                Config.Save(this, SystemTag);
                NavArgs.ConfigPage.ForceSystemListIconRebuild(PageHelper.NavptSystemInfo.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"ECMP:VerbMarkerDeleted({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == PageHelper.NavptSystemInfo.RouteNames[0])
            {
                PageHelper.RemoveNavpoint(Config, info.TagStr, info.TagInt - 1);
                Config.Save(this, SystemTag);
                NavArgs.ConfigPage.ForceSystemListIconRebuild(PageHelper.NavptSystemInfo.SystemTag);
            }
// TODO: handle other types of markers (user pois?)
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// map window closing: clear map window instance.
        /// </summary>
        private void MapWindow_Closed(object sender, WindowEventArgs args)
        {
            MapWindow = null;
        }

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

            uiTextThreats.Text = ThreatSummaryString();
        }
    }
}
