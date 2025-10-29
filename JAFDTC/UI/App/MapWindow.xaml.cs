// ********************************************************************************************************************
//
// MapWindow.xaml.cs -- ui c# for map window
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

// define ENABLE_MAP_FILE_CACHE to enable file caching of map tiles via ImageFileCache from MapControl.
//
#define ENABLE_MAP_FILE_CACHE

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using MapControl;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: document.
    /// </summary>
    public sealed partial class MapWindow : Window, IMapControlVerbMirror
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
        public const MapMarkerInfo.MarkerTypeMask RO_MARKER_TYPES = (MapMarkerInfo.MarkerTypeMask.DCS_CORE |
                                                                     MapMarkerInfo.MarkerTypeMask.IMPORT_GEN |
                                                                     MapMarkerInfo.MarkerTypeMask.IMPORT_S2A);

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

        // ---- private properties

        private MapControl.Caching.ImageFileCache _mapTileCache;

        private bool _isViewportChanging = false;

        private readonly Dictionary<string, IMapControlVerbHandler> _mapObservers = [ ];

        private static WinProc _newWndProc = null;
        private static IntPtr _oldWndProc = IntPtr.Zero;

        private static readonly SizeInt32 _windSizeBase = new() { Width = 500, Height = 750 };
        private static readonly SizeInt32 _windSizeMin = new() { Width = 750, Height = 500 };
        private static SizeInt32 _windSizeMax = new() { Width = 1800, Height = 1600 };

        private static PointInt32 _windPosnCur = new() { X = 0, Y = 0 };
        private static SizeInt32 _windSizeCur = new() { Width = 0, Height = 0 };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapWindow()
        {
            InitializeComponent();

            Title = "JAFDTC Map View";

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;

            Theater = "Unknown";
            CoordFormat = LLFormat.DD;
            OpenMask = MapMarkerInfo.MarkerTypeMask.NONE;
            EditMask = MapMarkerInfo.MarkerTypeMask.NONE;
            MaxRouteLength = 0;
            CanOpenMarker = true;

            Closed += MapWindow_Closed;
            SizeChanged += MapWindow_SizeChanged;

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

#if ENABLE_MAP_FILE_CACHE

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                       "JAFDTC", "MapControlTileCache");
            FileManager.Log($"Enabling MapControl ImageFileCache at: {path}");
            _mapTileCache = new MapControl.Caching.ImageFileCache(path);
            TileImageLoader.Cache = _mapTileCache;

#else

            _mapTileCache = null;

#endif

            // ---- window setup

            string lastSetup = Settings.LastWindowSetupMap;

            nint hWnd = GetWindowHandleForCurrentWindow(this);
            SizeInt32 baseSize = Utilities.BuildWindowSize(GetDpiForWindow(hWnd), _windSizeBase, lastSetup);
            AppWindow.Resize(baseSize);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWind = AppWindow.GetFromWindowId(windowId);
            if (appWind != null)
            {
                appWind.SetIcon(@"Images/JAFDTC_Icon.ico");
                DisplayArea dispArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                if (dispArea != null)
                {
                    PointInt32 posn = Utilities.BuildWindowPosition(dispArea.WorkArea, baseSize, lastSetup);
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
        /// setup initial map content (markers, routes) and load it into the map control as well as various ui
        /// elements in the window. set up the initial map view configuraiton to center on the bounding box of the
        /// marker set.
        /// 
        /// this should be the final call in the setup process.
        /// </summary>
        public void SetupMapContent(Dictionary<string, List<INavpointInfo>> routes,
                                    Dictionary<string, PointOfInterest> marks)
        {
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

            uiMap.SetupMapContent(routes, marks);

            BoundingBox bounds = uiMap.GetMarkerBoundingBox(2.0);
            uiMap.ZoomToBounds(bounds);                                     // ztb here avoids visual glitch
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                uiMap.ZoomToBounds(bounds);                                 // ztb here with non-0 window size
            });

            RebuildInterfaceState();
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
        /// TODO: document
        /// </summary>
        private void RebuildStatusNavpointInfo()
        {
            MapMarkerInfo mrkInfo = uiMap.SelectedMarkerInfo;
            if (mrkInfo == null)
            {
                uiTxtSelName.Text = "No Selection";
                uiTxtSelLat.Visibility = Visibility.Collapsed;
                uiTxtSelLon.Visibility = Visibility.Collapsed;
                uiTxtSelAlt.Visibility = Visibility.Collapsed;
                uiLblSelAlt.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (mrkInfo.Type == MapMarkerInfo.MarkerType.UNKNOWN)
                {
                    uiTxtSelName.Text = "Unknown Selection";
                }
                else
                {
                    uiTxtSelName.Text = MarkerExplainer?.MarkerDisplayName(mrkInfo) ?? "Unknown";
                    uiTxtSelAlt.Text = MarkerExplainer?.MarkerDisplayElevation(mrkInfo, "'") ?? "Unknown";
                    uiTxtSelLat.Text = Coord.ConvertFromLatDD(mrkInfo.Lat, CoordFormat);
                    uiTxtSelLon.Text = Coord.ConvertFromLonDD(mrkInfo.Lon, CoordFormat);

                    uiTxtSelLat.Visibility = Visibility.Visible;
                    uiTxtSelLon.Visibility = Visibility.Visible;
                    uiTxtSelAlt.Visibility = Visibility.Visible;
                    uiLblSelAlt.Visibility = Visibility.Visible;
                }
            }
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

// TODO: correctly set enable when implemented
            Utilities.SetEnableState(uiBarBtnAdd, false);
            Utilities.SetEnableState(uiBarBtnEdit, isOpenable && CanOpenMarker);
            Utilities.SetEnableState(uiBarBtnDelete, uiMap.CanEditSelectedMarker);
// TODO: correctly set enable when implemented
            Utilities.SetEnableState(uiBarBtnImport, false);
// TODO: correctly set enable when implemented
            Utilities.SetEnableState(uiBarBtnSettings, false);
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
        /// import command: manage import points.
        /// </summary>
        public void CmdImport_Click(object sender, RoutedEventArgs args)
        {
// TODO: implement
        }

        /// <summary>
        /// settings command: open the settings dialog to select settings.
        /// </summary>
        public void CmdSettings_Click(object sender, RoutedEventArgs args)
        {
// TODO: implement
        }

        // ---- zoom slider -------------------------------------------------------------------------------------------

        /// <summary>
        /// zoom slider: update zoom level on map.
        /// </summary>
        public void ZoomSlider_ValueChanged(object sender, RoutedEventArgs args)
        {
            if (!_isViewportChanging)
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
            _isViewportChanging = true;
            uiSliderZoom.Value = 100.0 * (uiMap.ZoomLevel / (uiMap.MaxZoomLevel - uiMap.MinZoomLevel));
            _isViewportChanging = false;
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
