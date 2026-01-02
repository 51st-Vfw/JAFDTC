// ********************************************************************************************************************
//
// F16CEditSteerpointListPage.xaml.cs : ui c# for viper steerpoint list editor page
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// editor for steerpoints in the viper.
    /// </summary>
    public sealed partial class F16CEditSteerpointListPage : Page, IMapControlVerbHandler, IMapControlMarkerExplainer
    {
        public static ConfigEditorPageInfo PageInfo
            => new(STPTSystem.SystemTag, "Steerpoints", "STPT", Glyphs.STPT, typeof(F16CEditSteerpointListPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditSTPT property.
        //
        private F16CConfiguration Config { get; set; }

        private STPTSystem EditSTPT { get; set; }

        private bool IsClipboardValid { get; set; }

        private int CaptureIndex { get; set; }

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

        private F16CEditSteerpointPage EditStptDetailPage { get; set; }

        private bool _isVerbEvent;
        private bool _isMarshalling;

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSteerpointListPage()
        {
            InitializeComponent();

            EditSTPT = new STPTSystem();

            _configNameToUID = [ ];
            _configNameList = [ ];
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between our local state and the steerpoint configuration.
        /// </summary>
        private void CopyConfigToEdit()
        {
            _isMarshalling = true;
            EditSTPT.Points.Clear();
            foreach (SteerpointInfo stpt in Config.STPT.Points)
                EditSTPT.Add(new SteerpointInfo(stpt));
            EditSTPT.RenumberFrom(1);
            _isMarshalling = false;
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            _isMarshalling = true;
            EditSTPT.RenumberFrom(1);
            Config.STPT = (STPTSystem)EditSTPT.Clone();
            _isMarshalling = false;

            if (isPersist)
                Config.Save(this, STPTSystem.SystemTag);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build out the data source and so on necessary for the map window and create it.
        /// </summary>
        private void CoreOpenMap(bool isMapWindowActive)
        {
            bool isLinked = !string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            MapMarkerInfo.MarkerTypeMask editMask = ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.NAV_PT) |
                                                    ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.PATH_EDIT_HANDLE);
            MapMarkerInfo.MarkerTypeMask openMask = MapMarkerInfo.MarkerTypeMask.NAV_PT;

            Dictionary<string, List<INavpointInfo>> routes = new()
            {
                [ STPTSystem.SystemInfo.RouteNames[0] ] = [.. EditSTPT.Points]
            };
            MapWindow = NavpointUIHelper.OpenMap(this, STPTSystem.SystemInfo.NavptMaxCount, LLFormat.DDM_P3ZF,
                                                 openMask, editMask, routes, Config.LastMapMarkerImport,
                                                 Config.LastMapFilter);
            MapWindow.MarkerExplainer = this;
            MapWindow.Closed += MapWindow_Closed;

            NavArgs.ConfigPage.RegisterAuxWindow(MapWindow);

            if (uiStptListView.SelectedIndex != -1)
                VerbMirror?.MirrorVerbMarkerSelected(this, new(MapMarkerInfo.MarkerType.ANY,
                                                               STPTSystem.SystemInfo.RouteNames[0],
                                                               uiStptListView.SelectedIndex + 1));

// TODO: activate main window on !isMapWindowActive?
        }

        /// <summary>
        /// launch the F16CEditSteerpointPage to edit the specified steerpoint.
        /// </summary>
        private void EditSteerpoint(SteerpointInfo stpt)
        {
            NavArgs.BackButton.IsEnabled = false;
            bool isUnlinked = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            Frame.Navigate(typeof(F16CEditSteerpointPage),
                           new F16CEditStptPageNavArgs(this, VerbMirror, Config, EditSTPT.IndexOf(stpt), isUnlinked),
                           new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// renumber steerpoints sequentially starting from StartingStptNum.
        /// </summary>
        private void RenumberSteerpoints()
        {
            EditSTPT.RenumberFrom(1);
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, STPTSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);

            Utilities.SetEnableState(uiBarAdd, isEditable);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiStptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiStptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiStptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable && isDCSListening);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarMap, (EditSTPT.Count > 0));
            Utilities.SetEnableState(uiBarImportPOIs, true);
            Utilities.SetEnableState(uiBarExportPOIs, isEditable && (EditSTPT.Count > 0));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, (EditSTPT.Count > 0));

            uiStptListView.CanReorderItems = isEditable;
            uiStptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        private void RebuildInterfaceState()
        {
            RebuildLinkControls();
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// open steerpoint button or context menu edit click: open the selected steerpoint.
        /// </summary>
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiStptListView.SelectedItem is SteerpointInfo stpt)
                EditSteerpoint(stpt);
        }

        /// <summary>
        /// add steerpoint: append a new steerpoint and save the configuration.
        /// </summary>
        private async void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            Tuple<string, string> ll = await NavpointUIHelper.ProposeNewNavptLatLon(Content.XamlRoot, [.. EditSTPT.Points ]);
            if (ll != null)
            {
                SteerpointInfo stpt = EditSTPT.Add();
                int index = EditSTPT.Points.IndexOf(stpt);
                EditSTPT.Points[index].Lat = ll.Item1;
                EditSTPT.Points[index].Lon = ll.Item2;
                CopyEditToConfig(true);
                RebuildInterfaceState();

                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAV_PT, STPTSystem.SystemInfo.RouteNames[0],
                                         index + 1, ll.Item1, ll.Item2);
                VerbMirror?.MirrorVerbMarkerAdded(this, info);
                VerbMirror?.MirrorVerbMarkerSelected(this, info);
            }
        }

        /// <summary>
        /// copy button or context menu copy click: serialize the selected steerpoints to json and put the text on
        /// the clipboard.
        /// </summary>
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            General.DataToClipboard(STPTSystem.STPTListTag,
                                    JsonSerializer.Serialize(uiStptListView.SelectedItems, Configuration.JsonOptions));
        }

        /// <summary>
        /// paste button: deserialize the steerpoints on the clipboard from json and append them to the end of the
        /// steerpoint list.
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            if (cboard?.SystemTag == STPTSystem.STPTListTag)
            {
                List<SteerpointInfo> list = JsonSerializer.Deserialize<List<SteerpointInfo>>(cboard.Data);
                foreach (SteerpointInfo stpt in list)
                    EditSTPT.Add(stpt);
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        // delete button or context menu edit click: delete the selected steerpoint(s), clear the selection, renumber
        // the remaining steerpoints, and save the updated configuration.
        //
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiStptListView.SelectedItems.Count > 0);

            if (await NavpointUIHelper.DeleteDialog(Content.XamlRoot, "Steerpoint", uiStptListView.SelectedItems.Count))
            {
                _isMarshalling = true;

                List<int> selectedIndices = [];
                foreach (ItemIndexRange range in uiStptListView.SelectedRanges)
                    for (int i = range.FirstIndex; i <= range.LastIndex; i++)
                        selectedIndices.Add(i);
                selectedIndices.Sort((a, b) => b.CompareTo(a));
                uiStptListView.SelectedItems.Clear();
                foreach (int index in selectedIndices)
                {
                    EditSTPT.Points.RemoveAt(index);
                    VerbMirror?.MirrorVerbMarkerDeleted(this, new(MapMarkerInfo.MarkerType.NAV_PT,
                                                                  STPTSystem.SystemInfo.RouteNames[0], index + 1));
                }
                VerbMirror?.MirrorVerbMarkerSelected(this, new());

                _isMarshalling = false;

                // steerpoint renumbering should be handled by observer to EditSTPT changes...
                //
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdCapture_Click(object sender, RoutedEventArgs args)
        {
            CaptureIndex = EditSTPT.Count;
            if (EditSTPT.Count > 0)
            {
                ContentDialogResult result = await Utilities.CaptureActionDialog(Content.XamlRoot, "Steerpoint");
                if (result != ContentDialogResult.Primary)
                    CaptureIndex = (uiStptListView.SelectedIndex >= 0) ? uiStptListView.SelectedIndex : 0;
            }

            CopyEditToConfig(true);

            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += CmdCapture_WyptCaptureDataReceived;
            await Utilities.CaptureMultipleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= CmdCapture_WyptCaptureDataReceived;

            CopyConfigToEdit();
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CmdCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                for (int i = 0; i < wypts.Length; i++)
                {
                    if (!wypts[i].IsTarget && (CaptureIndex < EditSTPT.Count))
                    {
                        EditSTPT.Points[CaptureIndex].Name = $"WP{i + 1} DCS Capture";
                        EditSTPT.Points[CaptureIndex].Lat = wypts[i].Latitude;
                        EditSTPT.Points[CaptureIndex].Lon = wypts[i].Longitude;
                        EditSTPT.Points[CaptureIndex].Alt = wypts[i].Elevation;
                        CaptureIndex++;
                    }
                    else if (!wypts[i].IsTarget)
                    {
                        SteerpointInfo stpt = new()
                        {
                            Name = $"WP{i + 1} DCS Capture",
                            Lat = wypts[i].Latitude,
                            Lon = wypts[i].Longitude,
                            Alt = wypts[i].Elevation
                        };
                        EditSTPT.Add(stpt);
                        CaptureIndex++;
                    }
                }
            });
        }

        /// <summary>
        /// import poi command: navigate to the poi list to pull in pois. implicitly closes any open map window to
        /// avoid having to do the coordination/coherency thing.
        /// </summary>
        private void CmdImportPOIs_Click(object sender, RoutedEventArgs args)
        {
            MapWindow?.Close();

            CopyEditToConfig();
            NavArgs.BackButton.IsEnabled = false;
            Frame.Navigate(typeof(AddNavpointsFromPOIsPage),
                           new AddNavpointsFromPOIsPage.NavigationArg(this, Config, new F16CEditSteerpointListHelper()),
                           new SlideNavigationTransitionInfo() {
                                Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// export poi command: copy from current navpoints to POIs. Opens modal to prompt for campaign, tags, etc.
        /// </summary>
        private void CmdExportPOIs_Click(object sender, RoutedEventArgs args)
        {
            if ((uiStptListView.Items.Count > 0) && (uiStptListView.SelectedItems.Count == 0))
                NavpointUIHelper.CopyNavpointsAsPoIs(Content.XamlRoot,
                                                     [.. uiStptListView.SelectedItems.OfType<INavpointInfo>() ], true);
            else if (uiStptListView.Items.Count > 0)
                NavpointUIHelper.CopyNavpointsAsPoIs(Content.XamlRoot,
                                                     [.. uiStptListView.Items.OfType<INavpointInfo>() ], false);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            if (await NavpointUIHelper.Import(Content.XamlRoot, AirframeTypes.F16C, EditSTPT, "Steerpoint"))
            {
                Config.Save(this, STPTSystem.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// map command: if the map window is not currently open, build out the data source and so on necessary for
        /// the window and create it. otherwise, activate the window.
        /// </summary>
        private void CmdMap_Click(object sender, RoutedEventArgs args)
        {
            if (MapWindow == null)
                CoreOpenMap(false);
            else
                MapWindow.Activate();
        }

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: remove all steerpoints from the configuration and save it.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (await NavpointUIHelper.ResetDialog(Content.XamlRoot, "Steerpoint"))
            {
                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.STPT.Reset();
                Config.Save(this, STPTSystem.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, STPTSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(STPTSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
            RebuildInterfaceState();
        }

        // ---- steerpoint list ---------------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint list selection change: update ui state to be consistent.
        /// </summary>
        private void StptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!_isMarshalling)
            {
                ListView listView = sender as ListView;
                if ((listView.SelectedItems.Count != 1) && !_isVerbEvent)
                {
// TODO: this will clear map window selection when we have multiple things selected in the poi list.
// TODO: this likely needs to change if multi-selection is ever supported in the map window.
                    VerbMirror?.MirrorVerbMarkerSelected(this, new());
                }
                else if ((listView.SelectedItems.Count == 1) && !_isVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new(MapMarkerInfo.MarkerType.ANY,
                                                                   STPTSystem.SystemInfo.RouteNames[0],
                                                                   listView.SelectedIndex + 1));
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// steerpoint list right click: bring up the steerpoint context menu to select from.
        /// </summary>
        private void StptList_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            SteerpointInfo stpt = (SteerpointInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (!uiStptListView.SelectedItems.Contains(stpt))
            {
                listView.SelectedItem = stpt;
                RebuildInterfaceState();
            }

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            uiStptListCtxMenuFlyout.Items[0].IsEnabled = false;     // edit
            uiStptListCtxMenuFlyout.Items[1].IsEnabled = false;     // copy
            uiStptListCtxMenuFlyout.Items[2].IsEnabled = false;     // paste
            uiStptListCtxMenuFlyout.Items[4].IsEnabled = false;     // delete
            if (stpt == null)
            {
                uiStptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;                // paste
            }
            else
            {
                bool isNotEmpty = (uiStptListView.SelectedItems.Count >= 1);
                uiStptListCtxMenuFlyout.Items[0].IsEnabled = (uiStptListView.SelectedItems.Count == 1);     // edit
                uiStptListCtxMenuFlyout.Items[1].IsEnabled = isEditable && isNotEmpty;                      // copy
                uiStptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;                // paste
                uiStptListCtxMenuFlyout.Items[4].IsEnabled = isEditable && isNotEmpty;                      // delete
            }
            uiStptListCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        /// <summary>
        /// steertpoint list double click: open the selected steerpoint in the editor.
        /// </summary>
        private void StptList_DoubleTapped(object sender, RoutedEventArgs args)
        {
            if (uiStptListView.SelectedItems.Count > 0)
                EditSteerpoint((SteerpointInfo)uiStptListView.SelectedItems[0]);
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
            return (info.Type == MapMarkerInfo.MarkerType.NAV_PT) ? STPTSystem.SystemInfo.NavptName
                                                                  : NavpointUIHelper.MarkerDisplayType(info);
        }

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                if (EditStptDetailPage != null)
                    CopyConfigToEdit();                                 // just in case editor is FA, so it won't FO
                string name = EditSTPT.Points[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"Steerpoint {info.TagInt}";
                return name;
            }
            else
            {
                return NavpointUIHelper.MarkerDisplayName(info);
            }
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                if (EditStptDetailPage != null)
                    CopyConfigToEdit();                                 // just in case editor is FA, so it won't FO
                string elev = EditSTPT.Points[info.TagInt - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            else
            {
                return NavpointUIHelper.MarkerDisplayElevation(info, units);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "F16CEditSteerpointListPage";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"VSLP:VerbMarkerSelected({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if ((info.TagStr != STPTSystem.SystemInfo.RouteNames[0]) ||
                (info.Type == MapMarkerInfo.MarkerType.UNKNOWN))
            {
                _isVerbEvent = true;
                uiStptListView.SelectedIndex = -1;
                _isVerbEvent = false;
            }
            else if ((info.TagStr == STPTSystem.SystemInfo.RouteNames[0]) &&
                     (info.Type == MapMarkerInfo.MarkerType.NAV_PT))
            {
                _isVerbEvent = true;
                uiStptListView.SelectedIndex = info.TagInt - 1;
                uiStptListView.ScrollIntoView(uiStptListView.SelectedItem);
                _isVerbEvent = false;

                EditStptDetailPage?.ChangeToEditNavpointAtIndex(info.TagInt - 1);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"VSLP:MarkerOpen({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                if (EditStptDetailPage == null)
                    EditSteerpoint(EditSTPT.Points[info.TagInt - 1]);
                else
                    EditStptDetailPage?.ChangeToEditNavpointAtIndex(info.TagInt - 1);
            }
            // TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"VSLP:VerbMarkerMoved({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                EditSTPT.Points[info.TagInt - 1].Lat = info.Lat;
                EditSTPT.Points[info.TagInt - 1].Lon = info.Lon;
                CopyEditToConfig(true);

                EditStptDetailPage?.CopyConfigToEditIfEditingNavpointAtIndex(info.TagInt - 1);
            }
            // TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"VSLP:VerbMarkerAdded({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                _isVerbEvent = true;
                SteerpointInfo stpt = EditSTPT.Add(null, info.TagInt - 1);
                int index = EditSTPT.Points.IndexOf(stpt);
                CopyConfigToEdit();
                EditSTPT.Points[index].Lat = info.Lat;
                EditSTPT.Points[index].Lon = info.Lon;
                CopyEditToConfig(true);

                RebuildInterfaceState();
                _isVerbEvent = false;
            }
            // TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"VSLP:VerbMarkerDeleted({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == STPTSystem.SystemInfo.RouteNames[0])
            {
                _isVerbEvent = true;
                uiStptListView.SelectedIndex = -1;

                EditSTPT.Points.RemoveAt(info.TagInt - 1);
                CopyEditToConfig(true);

                EditStptDetailPage?.CancelIfEditingNavpointAtIndex(info.TagInt - 1);

                RebuildInterfaceState();
                _isVerbEvent = false;
            }
            // TODO: handle other types of markers (user pois?)
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// check for clipboard content changes and update state as necessary.
        /// </summary>
        private async void ClipboardChangedHandler(object sender, object args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            IsClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(STPTSystem.STPTListTag)));
            RebuildInterfaceState();
        }

        /// <summary>
        /// when collection changes, renumber. just in case.
        /// </summary>
        private void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs args)
        {
            // TODO: this is a bit of a hack since there's no clear way to know when a re-order via drag has completed
            // TODO: other than looking for these changes.
            //
            // if we're not in a verb-triggered event and marshalling, the collection change is comeing from a drag.
            // in that case, we need to mirror the changes to the collection to the other observers.
            //
            ObservableCollection<SteerpointInfo> list = (ObservableCollection<SteerpointInfo>)sender;
            if (!_isVerbEvent && !_isMarshalling && (args.Action == NotifyCollectionChangedAction.Add))
            {
                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAV_PT, STPTSystem.SystemInfo.RouteNames[0],
                                         args.NewStartingIndex + 1,
                                         (args.NewItems[0] as INavpointInfo).Lat, (args.NewItems[0] as INavpointInfo).Lon);
                VerbMirror?.MirrorVerbMarkerAdded(this, info);
            }
            else if (!_isVerbEvent && !_isMarshalling && (args.Action == NotifyCollectionChangedAction.Remove))
            {
                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAV_PT, STPTSystem.SystemInfo.RouteNames[0],
                                         args.OldStartingIndex + 1);
                VerbMirror?.MirrorVerbMarkerDeleted(this, info);
            }
            if (!_isMarshalling && (list.Count > 0))
                RenumberSteerpoints();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void WindowActivatedHandler(object sender, WindowActivatedEventArgs args)
        {
            if ((args.WindowActivationState == WindowActivationState.PointerActivated) ||
                (args.WindowActivationState == WindowActivationState.CodeActivated))
            {
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// map window closing: persist the import and filter specifications that the user set up for next time, save
        /// the configuration, and cancel subscriptions.
        /// </summary>
        private void MapWindow_Closed(object sender, WindowEventArgs args)
        {
            Config.LastMapMarkerImport = MapWindow.MapImportSpec;
            Config.LastMapFilter = MapWindow.MapFilterSpec;
            Config.Save(this);

            MapWindow = null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on configuration saved, rebuild the interface state to align with the latest save (assuming we go here
        /// through a CopyEditToConfig).
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.SyncSysTag))
                CopyConfigToEdit();
            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        ///
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;

            Config = (F16CConfiguration)NavArgs.Config;
            CopyConfigToEdit();

            EditStptDetailPage = null;

            NavArgs.BackButton.IsEnabled = true;

            Config.ConfigurationSaved += ConfigurationSavedHandler;
            EditSTPT.Points.CollectionChanged += CollectionChangedHandler;
            Clipboard.ContentChanged += ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated += WindowActivatedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, STPTSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            ClipboardChangedHandler(null, null);
            RebuildInterfaceState();

            base.OnNavigatedTo(args);

            if (Settings.MapSettings.IsAutoOpen)
                Utilities.DispatchAfterDelay(DispatcherQueue, 1.0, false, (s, e) => CoreOpenMap(false));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            // we can navigate from here by pushing to a navpoint detail editor (F16CEditSteerpointPage) or by
            // pushing to a add poi list page (AddNavpointsFromPOIsPage). we use F16CEditSteerpointPage to
            // determine when we have pushed the former and not the latter so we can correctly work with any
            // map window.
            //
            EditStptDetailPage = args.Content as F16CEditSteerpointPage;

            Config.ConfigurationSaved -= ConfigurationSavedHandler;
            EditSTPT.Points.CollectionChanged -= CollectionChangedHandler;
            Clipboard.ContentChanged -= ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated -= WindowActivatedHandler;

            CopyEditToConfig(true);
            RebuildInterfaceState();

            base.OnNavigatedFrom(args);
        }
    }
}
