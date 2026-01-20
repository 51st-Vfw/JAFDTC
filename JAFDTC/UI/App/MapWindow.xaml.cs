// ********************************************************************************************************************
//
// MapWindow.xaml.cs -- ui c# for map window
//
// Copyright(C) 2025-2026 ilominar/raven
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

// define ENABLE_MAP_FILE_CACHE to enable file caching of map tiles via ImageFileCache from MapControl.
//
#define ENABLE_MAP_FILE_CACHE

using JAFDTC.Core.Extensions;
using JAFDTC.File;
using JAFDTC.File.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.Models.Threats;
using JAFDTC.Models.Units;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using MapControl;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Text;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: document.
    /// </summary>
    public sealed partial class MapWindow : Window, IMapControlMarkerPopupFactory, IMapControlVerbMirror
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public nint hwnd;
            public nint hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [Flags]
        private enum WindowLongIndexFlags : int
        {
            GWL_WNDPROC = -4,
        }

        private enum WindowMessage : int
        {
            WM_GETMINMAXINFO = 0x0024,
            WM_WINDOWPOSCHANGED = 0x0047
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        // the following marker types are never editable.
        //
        public const MapMarkerInfo.MarkerTypeMask RO_MARKER_TYPES = (MapMarkerInfo.MarkerTypeMask.POI_SYSTEM |
                                                                     MapMarkerInfo.MarkerTypeMask.UNIT_FRIEND |
                                                                     MapMarkerInfo.MarkerTypeMask.UNIT_ENEMY);

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public IMapControlMarkerExplainer MarkerExplainer { get; set; }

        public string Theater { get; set; }

        private LLFormat _coordFormat;
        public LLFormat CoordFormat
        {
            get => _coordFormat;
            set
            {
                _coordFormat = value;
                RebuildInterfaceState();
            }
        }

        public MapMarkerInfo.MarkerTypeMask OpenMask { get; set; }

        public MapMarkerInfo.MarkerTypeMask EditMask
        {
            get => uiMap.EditMask;
            set => uiMap.EditMask = value & ~RO_MARKER_TYPES;
        }

        public int MaxRouteLength
        {
            get => uiMap.MaxRouteLength;
            set => uiMap.MaxRouteLength = value;
        }

        public bool CanOpenMarker { get; set; }

        public MapFilterSpec MapFilterSpec { get; private set; }

        public MapImportSpec MapImportSpec { get; private set; }

        // ---- private properties

        private bool IsViewportChanging { get; set; }

        private bool IsThreatsEnabled { get; set; }

        private bool IsFilterEnabled { get; set; }

        private readonly Dictionary<string, List<string>> _mappedCampaigns = [ ];

        private readonly Dictionary<string, string> _mapImportThreatNameDict = [ ];

        private readonly Dictionary<string, string> _mapImportThreatTypeDict = [ ];

        private readonly Dictionary<string, string> _mapImportMarkerNameDict = [ ];

        private readonly Dictionary<string, string> _mapImportMarkerTypeDict = [ ];

        private readonly MapControl.Caching.ImageFileCache _mapTileCache;

        private readonly Dictionary<string, IMapControlVerbHandler> _mapObservers = [ ];

        private static WinProc _newWndProc = null;
        private static IntPtr _oldWndProc = IntPtr.Zero;

        private static readonly SizeInt32 _windSizeBase = new() { Width = 500, Height = 750 };
        private static readonly SizeInt32 _windSizeMin = new() { Width = 750, Height = 500 };
        private static SizeInt32 _windSizeMax = new() { Width = 1800, Height = 1600 };

        private static PointInt32 _windPosnCur = new() { X = 0, Y = 0 };
        private static SizeInt32 _windSizeCur = new() { Width = 0, Height = 0 };

        // ---- constructed properties

        private bool IsFiltered => ((MapFilterSpec != null) && !MapFilterSpec.IsDefault);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapWindow(bool isThreatsEnabled = false, bool isFilterEnabled = false)
        {
            InitializeComponent();

            Title = "JAFDTC Map View";

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;

            Theater = "Unknown";
            IsThreatsEnabled = isThreatsEnabled;
            IsFilterEnabled = isFilterEnabled;
            IsViewportChanging = false;
            CoordFormat = LLFormat.DD;
            OpenMask = MapMarkerInfo.MarkerTypeMask.NONE;
            EditMask = MapMarkerInfo.MarkerTypeMask.NONE;
            MaxRouteLength = 0;
            CanOpenMarker = true;
            MapFilterSpec = new();
            MapImportSpec = new();

            Closed += MapWindow_Closed;
            SizeChanged += MapWindow_SizeChanged;

            // ---- map control setup

            uiMap.MarkerPopupFactory = this;
            uiMap.VerbMirror = this;
            RegisterMapControlVerbObserver(uiMap);

            uiMap.PointerMoved += Map_PointerMoved;
            uiMap.ViewportChanged += Map_ViewportChanged;

            Dictionary<string, string> tileParams = FileManager.LoadParametersDictionary("params-map-tiles");
            if (tileParams.Count > 0)
            {
                uiMapTiles.TileSource = new TileSource()
                {
                    UriTemplate = tileParams["UriTemplate"],
                };
                uiMapTiles.SourceName = tileParams["SourceName"];
                uiMapTiles.Description = tileParams["Description"];
            }
            foreach (Inline inline in Utilities.TextToInlines(uiMapTiles.Description))
                uiMapTextAttribution.Inlines.Add(inline);

            _mapTileCache = null;
            if (Settings.MapSettings.IsTileCacheEnabled)
            {
#if ENABLE_MAP_FILE_CACHE

                _mapTileCache = new MapControl.Caching.ImageFileCache(FileManager.MapTileCachePath);
                TileImageLoader.Cache = _mapTileCache;
                FileManager.Log($"MapControl ImageFileCache path: {FileManager.MapTileCachePath}");
                FileManager.Log($"MapControl ImageFileCache size: {FileManager.GetCurrentMapTileCacheSize()}");

#endif
            }
            else
            {
                FileManager.Log($"MapControl ImageFileCache disabled");
            }

            // ---- window setup

            string lastSetup = Settings.LastWindowSetupMap;

            nint hWnd = GetWindowHandleForCurrentWindow(this);
            double dpiWnd = GetDpiForWindow(hWnd);
            SizeInt32 baseSize = Utilities.BuildWindowSize(dpiWnd, _windSizeBase, lastSetup);
            AppWindow.Resize(baseSize);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWind = AppWindow.GetFromWindowId(windowId);
            if (appWind != null)
            {
                appWind.SetIcon(@"Images/JAFDTC_Icon.ico");
                DisplayArea dispArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                if (dispArea != null)
                {
                    PointInt32 posn = Utilities.BuildWindowPosition(dpiWnd, dispArea.WorkArea, baseSize, lastSetup);
                    appWind.Move(posn);
                    _windSizeMax.Width = Math.Max(_windSizeMax.Width, dispArea.WorkArea.Width);
                    _windSizeMax.Height = Math.Max(_windSizeMax.Height, dispArea.WorkArea.Height);
                }
            }

            // sets up min/max window sizes using the right magic. code pulled from stackoverflow:
            //
            // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c
            //
            _newWndProc = new WinProc(WndProc);
            _oldWndProc = SetWindowLongPtr(hWnd, WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);

            // OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            // presenter.IsAlwaysOnTop = Settings.IsAlwaysOnTop;
            // presenter.IsResizable = false;
            // presenter.IsMaximizable = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // window sizing support
        //
        // ------------------------------------------------------------------------------------------------------------

        // sets up min/max window sizes using the right magic, see
        //
        // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c

        private static IntPtr GetWindowHandleForCurrentWindow(object target)
            => WinRT.Interop.WindowNative.GetWindowHandle(target);

        private static IntPtr WndProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_WINDOWPOSCHANGED:
                    var windPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    _windPosnCur.X = windPos.x;
                    _windPosnCur.Y = windPos.y;
                    break;
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(_windSizeMin.Width * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.y = (int)(_windSizeMax.Height * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.x = (int)(_windSizeMax.Width * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(_windSizeMin.Height * scalingFactor);

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(_oldWndProc, hWnd, Msg, wParam, lParam);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // setup
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set up initial map content (markers, routes, threats) and load it into the map control as well as various
        /// ui elements in the window. set the initial map view configuraiton to center on bounding box of markers.
        /// 
        /// routes are specified by a dictionary keyed by route unique identifiers with navpoint list values.
        ///
        /// marks are specified by a dictionary keyed by mark unique identifiers with point of interest values. note
        /// that the point of interest type may be a MapMarkerInfo.MarkerType, not just a PointOfInterestType.
        /// 
        /// threats are specified using the import specification.
        /// 
        /// this should be the final call in the setup process and will not generate add/delete/etc. verb calls.
        /// </summary>
        public void SetupMapContent(Dictionary<string, List<INavpointInfo>> paths,
                                    Dictionary<string, PointOfInterest> marks,
                                    List<Models.Planning.Threat> threats = null,
                                    MapImportSpec mapImport = null, MapFilterSpec mapFilter = null)
        {
            mapImport ??= new();

            MapFilterSpec = mapFilter ?? new();
            if (uiBarBtnFilter.IsChecked != IsFiltered)
                uiBarBtnFilter.IsChecked = IsFiltered;

            // set up theater in status area at bottom of window and size the width of the current mouse lat/lon
            // based on the coordinate format we are using.
            //
            uiTxtTheater.Text = Theater;
            double width = CoordFormat switch
            {
                LLFormat.DDM_P1ZF => (6.0 + (5.0 * 0.5)) * 8.0,             // M 00 00.0, M 000 00.0
                LLFormat.DDM_P2ZF => (7.0 + (5.0 * 0.5)) * 8.0,             // M 00 00.000, M 000 00.00
                LLFormat.DDM_P3ZF => (8.0 + (5.0 * 0.5)) * 8.0,             // M 00 00.000, M 000 00.000
                LLFormat.DMS => (7.0 + (4.0 * 0.5)) * 8.0,                  // M 00 00 00, M 000 00 00
                _ => 100.0
            };
            uiTxtMouseLat.Width = width;
            uiTxtMouseLon.Width = width + 8.0;

            uiMap.SetTheater(Theater);

            // build out paths and marks based on the information in the provided dictionaries.
            //
            foreach (KeyValuePair<string, List<INavpointInfo>> kvp in paths)
            {
                ObservableCollection<Location> locations = [];
                for (int i = 0; i < kvp.Value.Count; i++)
                    locations.Add(new Location(double.Parse(kvp.Value[i].Lat), double.Parse(kvp.Value[i].Lon)));
                uiMap.AddPath(MapMarkerInfo.MarkerType.NAV_PT, kvp.Key, locations);
            }
            foreach (KeyValuePair<string, PointOfInterest> kvp in marks)
            {
                if (kvp.Value.Type == PointOfInterestType.CAMPAIGN)
                    if (_mappedCampaigns.TryGetValue(kvp.Value.Campaign, out List<string> value))
                        value.Add(kvp.Value.UniqueID);
                    else
                        _mappedCampaigns[kvp.Value.Campaign] = [ kvp.Value.UniqueID ];
                uiMap.AddMarker((MapMarkerInfo.MarkerType) kvp.Value.Type, kvp.Key,
                                new Location(double.Parse(kvp.Value.Latitude), double.Parse(kvp.Value.Longitude)));
            }

            // import threats from the threat environment and the last used import path. if we are unable to find
            // or import, clear the import specification so it's not used going forward.
            //
            CoreImportThreats(threats);
            try
            {
                MapImportSpec = null;
                if (!string.IsNullOrEmpty(mapImport.Path) && System.IO.File.Exists(mapImport.Path))
                {
                    CoreImportMarkers(mapImport);
                    MapImportSpec = mapImport;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"SetupMapContent: import failed, {ex.Message}");
            }

            // zoom the map control to fit all of the markers defined in the map.
            //
            BoundingBox bounds = uiMap.GetMarkerBoundingBox(2.0);
            uiMap.ZoomToBounds(bounds);                                     // ztb here avoids visual glitch
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                // TODO: should this center on theater instead of marker bounds?
                uiMap.ZoomToBounds(bounds);                                 // ztb here with non-0 window size
            });

            RebuildElementsForFilter();
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerPopupFactory
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a popup to use to identify the a map marker. the map control uses this method to provide the
        /// ui element to display when a popup is called for.
        /// </summary>
        public Popup MarkerPopup(MapMarkerInfo mrkInfo)
        {
            Popup popup = new()
            {
                HorizontalOffset = 24,
                VerticalOffset = -6
            };
            StackPanel pupTitleStack = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock pupTextTitle = new()
            {
                Margin = new Thickness(0, 0, 0, 2),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
            };
            pupTitleStack.Children.Add(pupTextTitle);
            if (uiMap.EditMask.HasFlag((MapMarkerInfo.MarkerTypeMask)(1 << (int)mrkInfo.Type)))
            {
                FontIcon pupStatusIcon = new()
                {
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new("Segoe Fluent Icons"),
                    FontSize = 11,
                    Glyph = "\xE70F"
                };
                pupTitleStack.Children.Add(pupStatusIcon);
            }

            TextBlock pupTextSubtitle = new()
            {
                FontSize = 11,
                FontStyle = FontStyle.Italic
            };

            StackPanel pupStack = new()
            {
                Margin = new Thickness(8, 4, 8, 8),
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            pupStack.Children.Add(pupTitleStack);
            pupStack.Children.Add(pupTextSubtitle);

            popup.Child = new Border ()
            {
                Padding = new Thickness(0, 0, 0, 0),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                Child = pupStack
            };

            if (_mapImportMarkerNameDict.TryGetValue(mrkInfo.TagStr, out string markerName))
                pupTextTitle.Text = markerName;
            else if (_mapImportThreatNameDict.TryGetValue(mrkInfo.TagStr, out string threatName))
                pupTextTitle.Text = threatName;
            else
                pupTextTitle.Text = MarkerExplainer?.MarkerDisplayName(mrkInfo) ?? "Unknown";
            string unitType = "Element";
            if (_mapImportMarkerTypeDict.TryGetValue(mrkInfo.TagStr, out string markerType))
                unitType = markerType;
            else if (_mapImportThreatTypeDict.TryGetValue(mrkInfo.TagStr, out string threatType))
                unitType = threatType;
            pupTextSubtitle.Text = mrkInfo.Type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM => "System POI",
                MapMarkerInfo.MarkerType.POI_USER => "User POI",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => "Campaign POI",
                MapMarkerInfo.MarkerType.NAV_PT => "Navigation Route",
                MapMarkerInfo.MarkerType.UNIT_FRIEND => $"Friendly {unitType}",
                MapMarkerInfo.MarkerType.UNIT_ENEMY => $"Enemy {unitType}",
                MapMarkerInfo.MarkerType.BULLSEYE => "Bullseye",
                _ => "Map marker"
            };

            return popup;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlVerbMirror
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// registers a map control verb observer with the mirror.
        /// </summary>
        public void RegisterMapControlVerbObserver(IMapControlVerbHandler observer)
        {
            _mapObservers[observer.VerbHandlerTag] = observer;
        }

        // following functions simply wlk the observers list and pass the verb along to all observers other than the
        // verb's sender. implicitly rebuilds our interface state after updates are completed.

        private void MirrorVerb(Action<IMapControlVerbHandler, MapMarkerInfo, int> verb, string senderTag,
                                MapMarkerInfo info, int param)
        {
            foreach (string tag in _mapObservers.Keys)
                if (tag != senderTag)
                    verb(_mapObservers[tag], info, param);
            RebuildInterfaceState();
        }

        public void MirrorVerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            void verb(IMapControlVerbHandler t, MapMarkerInfo i, int param) { t.VerbMarkerSelected(sender, i, param); }
            MirrorVerb(verb, sender?.VerbHandlerTag, info, param);
        }

        public void MirrorVerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            void verb(IMapControlVerbHandler t, MapMarkerInfo i, int param) { t.VerbMarkerOpened(sender, i, param); }
            MirrorVerb(verb, sender?.VerbHandlerTag, info, param);
        }

        public void MirrorVerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            void verb(IMapControlVerbHandler t, MapMarkerInfo i, int param) { t.VerbMarkerMoved(sender, i, param); }
            MirrorVerb(verb, sender?.VerbHandlerTag, info, param);
        }

        public void MirrorVerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            void verb(IMapControlVerbHandler t, MapMarkerInfo i, int param) { t.VerbMarkerAdded(sender, i, param); }
            MirrorVerb(verb, sender?.VerbHandlerTag, info, param);
        }

        public void MirrorVerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            void verb(IMapControlVerbHandler t, MapMarkerInfo i, int param) { t.VerbMarkerDeleted(sender, i, param); }
            MirrorVerb(verb, sender?.VerbHandlerTag, info, param);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// import markers according to the import specification and add them to the map control updating internal
        /// state as necessary. throws an exception on issues.
        /// </summary>
        void CoreImportThreats(List<Models.Planning.Threat> threats)
        {
            bool isSummaryOnly = true;
            foreach (Models.Planning.Threat threat in threats)
                if (!string.IsNullOrEmpty(threat.Type))
                {
                    isSummaryOnly = false;
                    break;
                }

            int marker = 0;
            foreach (Models.Planning.Threat threat in threats)
            {
                MapMarkerInfo.MarkerType type = (threat.Coalition == CoalitionType.BLUE)
                                                ? MapMarkerInfo.MarkerType.UNIT_FRIEND
                                                : MapMarkerInfo.MarkerType.UNIT_ENEMY;
                string uid = $"<threat_{marker}>";

                if (string.IsNullOrEmpty(threat.Type))
                {
                    uiMap.AddMarker(type, uid, new Location(double.Parse(threat.Location.Latitude),
                                                            double.Parse(threat.Location.Longitude)),
                                    threat.WEZ.GetValueOrDefault(0.0), !isSummaryOnly);
                }
                else
                {
                    uiMap.AddMarker(type, uid, new Location(double.Parse(threat.Location.Latitude),
                                                            double.Parse(threat.Location.Longitude)));
                    _mapImportThreatTypeDict[uid] = threat.Type.ToCleanDCSUnitType();
                }
                _mapImportThreatNameDict[uid] = threat.Name;
                marker++;
            }
        }

        /// <summary>
        /// import markers according to the import specification and add them to the map control updating internal
        /// state as necessary. throws an exception on issues.
        /// </summary>
        void CoreImportMarkers(MapImportSpec importSpec)
        {
            // build extractor and import groups/units from the file.
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
                Theater = Theater,
                UnitCategories = [UnitCategoryType.GROUND, UnitCategoryType.NAVAL],
                IsAlive = (importSpec.IsAliveOnly) ? true : null
            };
            if (importSpec.IsEnemyOnly && (importSpec.FriendlyCoalition == CoalitionType.BLUE))
                criteria.Coalitions = [CoalitionType.RED];
            else if (importSpec.IsEnemyOnly && (importSpec.FriendlyCoalition == CoalitionType.RED))
                criteria.Coalitions = [CoalitionType.BLUE];
            IReadOnlyList<UnitGroupItem> groups = extractor.Extract(criteria)
                ?? throw new Exception($"Encountered an error while importing from path\n\n{importSpec.Path}");

            FileManager.Log($"MapWindow:CoreImportMarkers extracted {groups.Count} groups from {importSpec.Path}");

            // add the threats to the map control. this includes a marker for each unit if we are not importing
            // only summary information and a marker with a threat ring at the average location of the group
            // for the threat wez (this marker will only have a visible center when we're not showing
            // individual units).
            //
            int nMarkers = 0;
            foreach (UnitGroupItem group in groups)
            {
// TODO: what to do on units outside of current theater? ignore? warn and create anyway?
                MapMarkerInfo.MarkerType type = (group.Coalition == importSpec.FriendlyCoalition)
                                                ? MapMarkerInfo.MarkerType.UNIT_FRIEND
                                                : MapMarkerInfo.MarkerType.UNIT_ENEMY;
                string threatType = null;
                double threatRadius = 0.0;
                double avgLat = 0.0;
                double avgLon = 0.0;
                foreach (UnitItem unit in group.Units)
                {
                    ThreatDbaseQuery query = new([unit.Type], null, null, null, null, true);
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
                        uiMap.AddMarker(type, unit.UniqueID,
                                        new Location(unit.Position.Latitude, unit.Position.Longitude));
                        _mapImportMarkerNameDict[unit.UniqueID] = (unit.IsAlive) ? unit.Name : (unit.Name + " [DEAD]");
                        _mapImportMarkerTypeDict[unit.UniqueID] = unit.Type.ToCleanDCSUnitType();
                        nMarkers++;
                    }
                }
                avgLat /= (double)group.Units.Count;
                avgLon /= (double)group.Units.Count;

                if (threatRadius > 0.0)
                {
                    uiMap.AddMarker(type, group.UniqueID, new Location(avgLat, avgLon), threatRadius,
                                    !importSpec.IsSummaryOnly);
                    _mapImportMarkerNameDict[group.UniqueID] = $"{group.Name}: {threatType} WEZ";
                }
            }
            FileManager.Log($"MapWindow:CoreImportMarkers added {nMarkers} markers to map");
        }

        /// <summary>
        /// set the visibility of elements in the map control based on the current filter settings.
        /// </summary>
        private void RebuildElementsForFilter()
        {
            List<string> campMarkTags = [];
            if (!string.IsNullOrEmpty(MapFilterSpec.ShowCampaign) &&
                _mappedCampaigns.TryGetValue(MapFilterSpec.ShowCampaign, out List<string> value))
            {
                campMarkTags = value;
            }

            uiMap.PathVisibility(( _ ) => MapFilterSpec.ShowNavRoutes);

            uiMap.MarkerVisibility((type, tag, hasRing) => {
                if (type == MapMarkerInfo.MarkerType.POI_SYSTEM)
                    return new(MapFilterSpec.ShowPOIDCS, MapFilterSpec.ShowPOIDCS);
                else if (type == MapMarkerInfo.MarkerType.POI_USER)
                    return new(MapFilterSpec.ShowPOIUsr, MapFilterSpec.ShowPOIUsr);
                else if ((type == MapMarkerInfo.MarkerType.POI_CAMPAIGN) && string.IsNullOrEmpty(MapFilterSpec.ShowCampaign))
                    return new(false, false);
                else if ((type == MapMarkerInfo.MarkerType.POI_CAMPAIGN) && MapFilterSpec.ShowCampaign.Equals("*"))
                    return new(true, true);
                else if (type == MapMarkerInfo.MarkerType.POI_CAMPAIGN)
                    return new(campMarkTags.Contains(tag), campMarkTags.Contains(tag));
                else if ((type != MapMarkerInfo.MarkerType.UNIT_FRIEND) && (type != MapMarkerInfo.MarkerType.UNIT_ENEMY))
                    return new(true, true);

                // NOTE: marker/ring visibilities assume that threat rings are separate from units. that is, a
                // NOTE: unit with a threat ring would have a marker for the unit and a seperate marker for the
                // NOTE: threat ring. this way a single marker can stand in for the collective threat ring of a
                // NOTE: set of units. when no relevant units are visible, threat ring center markers are visible.
                //
                //                       mark = UNIT_FRIEND    mark = UNIT_ENEMY
                // units     threats     no ring   has ring    no ring   has ring
                // --------  --------    --------  --------    --------  --------
                // ALL       ALL         t, -      f*, t       t, -      f*, t
                // ALL       OPPOSING    t, -      f*, f*      t, -      f*, t
                // ALL       NONE        t, -      f*, f*      t, -      f*, f*
                //
                // OPPOSING  ALL         f, -      t*, t*      t, -      f*, t
                // OPPOSING  OPPOSING    f, -      f, f        t, -      f*, t
                // OPPOSING  NONE        f, -      f, f        t, -      f*, f
                // 
                // NONE      ALL         f, -      t*, t*      f, -      t*, t*
                // NONE      OPPOSING    f, -      f, f        f, -      t*, t*
                // NONE      NONE        f, -      f, f        f, -      f, f
                //
                // * => change from "no ring" specifying both marker and ring visibility in ring cases.

                bool isMarkVis = true;
                if (type == MapMarkerInfo.MarkerType.UNIT_FRIEND)
                    isMarkVis = (MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.ALL);
                else if (type == MapMarkerInfo.MarkerType.UNIT_ENEMY)
                    isMarkVis = (MapFilterSpec.ShowUnits != MapFilterSpec.ImportFilter.NONE);
                bool isRingVis = isMarkVis;

                if (hasRing && (type == MapMarkerInfo.MarkerType.UNIT_FRIEND))
                {
                    if ((MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.ALL) &&
                        (MapFilterSpec.ShowThreatRings == MapFilterSpec.ImportFilter.ALL))
                    {
                        isMarkVis = false;
                        isRingVis = true;
                    }
                    else if ((MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.ALL) &&
                             (MapFilterSpec.ShowThreatRings != MapFilterSpec.ImportFilter.ALL))
                    {
                        isMarkVis = false;
                        isRingVis = false;
                    }
                    else if ((MapFilterSpec.ShowUnits != MapFilterSpec.ImportFilter.ALL) &&
                             (MapFilterSpec.ShowThreatRings == MapFilterSpec.ImportFilter.ALL))
                    {
                        isMarkVis = true;
                        isRingVis = true;
                    }
                }
                else if (hasRing && (type == MapMarkerInfo.MarkerType.UNIT_ENEMY))
                {
                    if ((MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.ALL) &&
                        (MapFilterSpec.ShowThreatRings != MapFilterSpec.ImportFilter.NONE))
                    {
                        isMarkVis = false;
                    }
                    else if ((MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.ALL) &&
                             (MapFilterSpec.ShowThreatRings == MapFilterSpec.ImportFilter.NONE))
                    {
                        isMarkVis = false;
                        isRingVis = false;
                    }
                    else if (MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.OPPOSING)
                    {
                        isMarkVis = false;
                    }
                    else if ((MapFilterSpec.ShowUnits == MapFilterSpec.ImportFilter.NONE) &&
                             (MapFilterSpec.ShowThreatRings != MapFilterSpec.ImportFilter.NONE))
                    {
                        isMarkVis = true;
                        isRingVis = true;
                    }
                }

                return new(isMarkVis, isRingVis);
            });

            RebuildStatusNavpointInfo();
        }

        /// <summary>
        /// update the content of the status bar at the bottom of the map window that indicates lat/lon, selection
        /// name, etc.
        /// </summary>
        private void RebuildStatusNavpointInfo()
        {
            bool isMarkerSelected = false;
            bool isMarkerEditable = false;
            MapMarkerInfo mrkInfo = uiMap.SelectedMarkerInfo;
            if (mrkInfo == null)
            {
                uiTxtSelName.Text = "No Selection";
            }
            else if (mrkInfo.Type == MapMarkerInfo.MarkerType.UNKNOWN)
            {
                uiTxtSelName.Text = "Unknown Selection";
            }
            else
            {
                if (mrkInfo.Type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE)
                    uiTxtSelName.Text = "Add Navpoint Handle";
                else if (_mapImportMarkerNameDict.TryGetValue(mrkInfo.TagStr, out string markerName))
                    uiTxtSelName.Text = markerName;
                else if (_mapImportThreatNameDict.TryGetValue(mrkInfo.TagStr, out string threatName))
                    uiTxtSelName.Text = threatName;
                else
                    uiTxtSelName.Text = MarkerExplainer?.MarkerDisplayName(mrkInfo) ?? "Unknown";

                uiTxtSelAlt.Text = MarkerExplainer?.MarkerDisplayElevation(mrkInfo, "'") ?? "Unknown";
                uiTxtSelLat.Text = Coord.ConvertFromLatDD(mrkInfo.Lat, CoordFormat);
                uiTxtSelLon.Text = Coord.ConvertFromLonDD(mrkInfo.Lon, CoordFormat);

                isMarkerSelected = true;
                isMarkerEditable = EditMask.HasFlag((MapMarkerInfo.MarkerTypeMask)(1 << (int)mrkInfo.Type));
            }

            uiIconEditStatus.Visibility = (isMarkerEditable) ? Visibility.Visible : Visibility.Collapsed;
            uiTxtSelLat.Visibility = (isMarkerSelected) ? Visibility.Visible : Visibility.Collapsed;
            uiTxtSelLon.Visibility = (isMarkerSelected) ? Visibility.Visible : Visibility.Collapsed;
            uiTxtSelAlt.Visibility = (isMarkerSelected) ? Visibility.Visible : Visibility.Collapsed;
            uiLblSelAlt.Visibility = (isMarkerSelected) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// rebuild the enable state of controls based on current context.
        /// </summary>
        private void RebuildEnableState()
        {
            MapMarkerInfo mkrInfo = uiMap.SelectedMarkerInfo;
            bool isOpenable = ((mkrInfo != null) &&
                               (mkrInfo.Type != MapMarkerInfo.MarkerType.UNKNOWN) &&
                               OpenMask.HasFlag((MapMarkerInfo.MarkerTypeMask)(1 << (int)mkrInfo.Type)));

// TODO: correctly set enable when add implemented
            Utilities.SetEnableState(uiBarBtnAdd, false);
            Utilities.SetEnableState(uiBarBtnEdit, isOpenable && CanOpenMarker);
            Utilities.SetEnableState(uiBarBtnDelete, uiMap.CanEditSelectedMarker);
            Utilities.SetEnableState(uiBarBtnImport, IsThreatsEnabled);
            Utilities.SetEnableState(uiBarBtnClear, IsThreatsEnabled && (_mapImportMarkerNameDict.Count > 0));
            Utilities.SetEnableState(uiBarBtnFilter, IsFilterEnabled);
            Utilities.SetEnableState(uiBarBtnSettings, true);
        }

        /// <summary>
        /// update the user interface state based on current content.
        /// </summary>
        public void RebuildInterfaceState()
        {
            RebuildStatusNavpointInfo();
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- command bar -------------------------------------------------------------------------------------------

        /// <summary>
        /// add command: add non-route marker to map.
        /// </summary>
        public void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
// TODO: implement
        }

        /// <summary>
        /// edit command: open selected marker for editing by outside ui.
        /// </summary>
        public void CmdEdit_Click(object sender, RoutedEventArgs args)
        {
            MirrorVerbMarkerOpened(null, uiMap.SelectedMarkerInfo);
        }

        /// <summary>
        /// delete command: delete selected marker.
        /// </summary>
        public async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            string type = MarkerExplainer.MarkerDisplayType(uiMap.SelectedMarkerInfo);
            if (await NavpointUIHelper.DeleteDialog(Content.XamlRoot, type, 1))
            {
                MapMarkerInfo info = uiMap.SelectedMarkerInfo;
                MirrorVerbMarkerSelected(null, new());
                MirrorVerbMarkerDeleted(null, info);
            }
        }

        /// <summary>
        /// import command: manage import threats. prompt for a file, then pull units to create threats to add to
        /// the map. imported threats are added to the set of known threats.
        /// </summary>
        public async void CmdImportThreats_Click(object sender, RoutedEventArgs args)
        {
            // if there are already markers imported, ask if its ok to clear them out as we can only have one set
            // imported at a time.
            //
            ContentDialogResult result;
            if (_mapImportMarkerNameDict.Count > 0)
            {
                result = await Utilities.Message2BDialog(Content.XamlRoot, "Are You Sure?",
                            "Importing threat and unit markers will remove any previously imported markers. Are" +
                            " you sure you want to proceed?",
                            "Remove Markers");
                if (result == ContentDialogResult.None)
                    return;                                     // **** EXITS: cancel

                uiMap.ClearMarkers((t, s) => (((t == MapMarkerInfo.MarkerType.UNIT_FRIEND) ||
                                               (t == MapMarkerInfo.MarkerType.UNIT_ENEMY)) && !s.StartsWith("<threat_")));
                _mapImportMarkerNameDict.Clear();
                _mapImportMarkerTypeDict.Clear();
            }

            // select file to import threats from with FileOpenPicker.
            //
            FileOpenPicker picker = new(this.AppWindow.Id)
            {
                CommitButtonText = "Import Elements",
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
                CoreImportMarkers(setupDialog.Spec);
                MapImportSpec = setupDialog.Spec;
                RebuildInterfaceState();
            }
            catch (Exception ex)
            {
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", ex.Message);
            }
        }

        /// <summary>
        /// clear command: clear any imported threat markers by removing them from the map. asks permission first.
        /// </summary>
        public async void CmdClearThreats_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                "Are You Sure?",
                "Remove all imported threat and unit markers from the map? This action cannot be undone.",
                "Remove Markers");
            if (result == ContentDialogResult.Primary)
            {
                MapImportSpec = null;
                uiMap.ClearMarkers((t, s) => (((t == MapMarkerInfo.MarkerType.UNIT_FRIEND) ||
                                               (t == MapMarkerInfo.MarkerType.UNIT_ENEMY)) && !s.StartsWith("<threat_")));
                _mapImportMarkerNameDict.Clear();
                _mapImportMarkerTypeDict.Clear();

                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// snap command: toggle enable on the snap command.
        /// </summary>
        public async void CmdSnap_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            uiMap.IsSnappingEnabled = button.IsChecked.GetValueOrDefault(false);
        }

        /// <summary>
        /// filter command: set the filter to apply to markers on the map.
        /// </summary>
        public async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            List<string> campaigns = [.. _mappedCampaigns.Keys ];
            campaigns.Sort();
            MapFilterDialog filterDialog = new(MapFilterSpec, campaigns)
            {
                XamlRoot = Content.XamlRoot,
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                MapFilterSpec = filterDialog.Filter;
            else if (result == ContentDialogResult.Secondary)
                MapFilterSpec = MapFilterSpec.Default;
            else
                return;                                         // EXIT: cancelled, no change...

            RebuildElementsForFilter();

            button.IsChecked = IsFiltered;
        }

        /// <summary>
        /// settings command: open the settings dialog to select and change map settings. updates persisted settings
        /// as necessary.
        /// </summary>
        public async void CmdSettings_Click(object sender, RoutedEventArgs args)
        {
            MapSettingsDialog settingsDialog = new(Settings.MapSettings,
                                                   FileManager.MapTileCachePath, FileManager.GetCurrentMapTileCacheSize())
            {
                XamlRoot = Content.XamlRoot
            };
            ContentDialogResult result = await settingsDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                Settings.MapSettings = settingsDialog.Settings;
        }

        // ---- zoom slider -------------------------------------------------------------------------------------------

        /// <summary>
        /// zoom slider: update zoom level on map.
        /// </summary>
        public void ZoomSlider_ValueChanged(object sender, RoutedEventArgs args)
        {
            if (!IsViewportChanging)
            {
                Slider slider = sender as Slider;
                uiMap.ZoomLevel = ((slider.Value / 100.0) * (uiMap.MaxZoomLevel - uiMap.MinZoomLevel)) + uiMap.MinZoomLevel;
            }
        }

        // ---- system events -----------------------------------------------------------------------------------------

        /// <summary>
        /// on viewport changed, update the slider value to match current zoom settings.
        /// </summary>
        private void Map_ViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            IsViewportChanging = true;
            uiSliderZoom.Value = 100.0 * (uiMap.ZoomLevel / (uiMap.MaxZoomLevel - uiMap.MinZoomLevel));
            IsViewportChanging = false;
        }

        /// <summary>
        /// on pointer moved, update the mouse lat/lon information in the window.
        /// </summary>
        private void Map_PointerMoved(object sender, PointerRoutedEventArgs args)
        {
            PointerPoint point = args.GetCurrentPoint(uiMap);
            Location location = uiMap.ViewToLocation(point.Position);

            string newLat = Coord.ConvertFromLatDD($"{location.Latitude}", CoordFormat);
            if (!string.IsNullOrEmpty(newLat))
                uiTxtMouseLat.Text = newLat;

            double lon = location.Longitude;
            while (lon > 180.0)
                lon = -180.0 + (lon - 180.0);
            while (lon < -180.0)
                lon = 180.0 + (lon + 180.0);
            string newLon = Coord.ConvertFromLonDD($"{lon}", CoordFormat);
            if (!string.IsNullOrEmpty(newLon))
                uiTxtMouseLon.Text = newLon;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on window closed, persist the last window location and size to settings and dispose any map tile cache.
        /// </summary>
        private void MapWindow_Closed(object sender, WindowEventArgs args)
        {
            Settings.LastWindowSetupMap = Utilities.BuildWindowSetupString(_windPosnCur, _windSizeCur);
            uiMap?.ClearMarkers();
            uiMap?.ClearPaths();
            _mapTileCache?.Dispose();
        }

        /// <summary>
        /// on size changed, stash the current window size into our window size for later persistance.
        /// </summary>
        private void MapWindow_SizeChanged(object sender, WindowSizeChangedEventArgs evt)
        {
            _windSizeCur.Width = (int)evt.Size.Width;
            _windSizeCur.Height = (int)evt.Size.Height;
        }
    }
}
