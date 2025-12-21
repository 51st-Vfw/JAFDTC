// ********************************************************************************************************************
//
// MapControl.cs : map control
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

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using MapControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

using static JAFDTC.UI.Controls.Map.MapMarkerControl;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// world map control is a control that displays a map upon which paths and markers can be overlaid and edited.
    /// these can represent navigation routes, points of interest, units, threats, etc. the map only tracks and
    /// handles lat/lon. edits for other parameters, such as name or altitude, are assumed to be handled elsewhere.
    /// </summary>
    public partial class MapControl : MapBase, IMapControlVerbHandler
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// information on the locations present in a geometry for a map control representing a path (such as
        /// MapRoutePath or MapThreatPath). includes a Location instance for each lat/lon that defines the geometry.
        /// </summary>
        private sealed class MapPathControlPath(ObservableCollection<Location> locations)
        {
            public ObservableCollection<Location> Locations { get; set; } = locations;
        }

        // ================================================================================================================

        /// <summary>
        /// information on a map path control that has both foreground and background layers that render the same path
        /// geometry with different properties (stroke, width, etc.) to implement highlighting. includes separate controls
        /// for foreground and background along with the points that define the path geometry.
        /// </summary>
        private sealed class MapPathControlLayered()
        {
            // ControlFg and ControlBg are set to appropriate instances via the data template used during creation.
            //
            public MapItemsControl ControlFg { get; set; } = null;
            public MapItemsControl ControlBg { get; set; } = null;
            //
            // extra level of hierarchy here (Paths is a list of *one* element: a list of Locations) is due to the way
            // xaml-map-control implements MapItemsControl as a ListBox. ListBox expects an ItemSource (Paths in our
            // case) which provides items that the ui elements can pull from via bindings to build screen content.
            //
            // NOTE: both MapItemsControl (ControlFg and ControlBg) use this as their data source.
            //
            public List<MapPathControlPath> Paths { get; set; } = [ ];              // always has 1 element

            /// <summary>
            /// set the visibility of the path control as indicated. note that the path is assumed to be unselected
            /// on visibility changes so the background control is always collapsed.
            /// </summary>
            /// <param name="visibility"></param>
            public void Visibility(Visibility visibility)
            {
                ControlBg.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                ControlFg.Visibility = visibility;
            }

            /// <summary>
            /// remove the elements associated with the path control from a map control.
            /// </summary>
            public void Remove(MapControl map)
            {
                map.RemoveElement(ControlFg);
                map.RemoveElement(ControlBg);
            }
        }

        // ================================================================================================================

        /// <summary>
        /// information on the controls that make up a path of straight lines connecting a sequence of locations (such as
        /// a navigation route or boundary line displayed on avionics). this includes the foreground and background path
        /// controls, the positive and negative edit handles, and the marker points for the locations on the path.
        /// </summary>
        private sealed class PathInfo()
        {
            public MapPathControlLayered Path { get; set; } = new();
            public MapMarkerControl EditHandlePos { get; set; } = null;             // null if r/o
            public MapMarkerControl EditHandleNeg { get; set; } = null;             // null if r/o
            public List<MapMarkerControl> Points { get; set; } = [ ];

            /// <summary>
            /// set the visibility of the path control as indicated. note that the path is assumed to be unselected
            /// on visibility changes so the background control and edit handles are always collapsed.
            /// </summary>
            /// <param name="visibility"></param>
            public void Visibility(Visibility visibility)
            {
                Path.Visibility(visibility);
                if (EditHandleNeg != null)
                    EditHandleNeg.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                if (EditHandlePos != null)
                    EditHandlePos.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                foreach (MapMarkerControl marker in Points)
                    marker.Visibility = visibility;
            }

            /// <summary>
            /// remove the elements associated with the path from a map control.
            /// </summary>
            public void Remove(MapControl map)
            {
                Path.Remove(map);
                map.RemoveElement(EditHandlePos);
                map.RemoveElement(EditHandleNeg);
                foreach (MapMarkerControl marker in Points)
                    map.RemoveElement(marker);
            }
        }

        // ================================================================================================================

        /// <summary>
        /// information on the controls that make up a marker and optional threat ring. this includes the foreground and
        /// background threat ring path controls, a threat ring edit handles, and the marker itself.
        /// </summary>
        private sealed class MarkerInfo()
        {
            public MapMarkerControl Marker { get; set; } = null;
            public MapPathControlLayered ThreatRing { get; set; } = null;           // null if no ring
            public MapMarkerControl EditHandle { get; set; } = null;                // null if nor ring or r/o
            public double RadiusThreatRing { get; set; } = 0.0;                     // 0.0 if ThreatRing is null

            /// <summary>
            /// set the visibility of the marker control including any threat ring as indicated. note that the marker is
            /// assumed to be unselected on visibility changes so the edit handles are always collapsed.
            /// </summary>
            public void Visibility(Visibility visibilityMark, Visibility visibilityRing)
            {
                Marker.Visibility = visibilityMark;
                ThreatRing?.Visibility(visibilityRing);
                if (EditHandle != null)
                    EditHandle.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }

            /// <summary>
            /// remove the elements associated with the marker from a map control if the type matches one of the
            /// types listed (empty or null list implies remove all types). returns true if the marker was removed,
            /// false otherwise.
            /// </summary>
            public bool Remove(MapControl map, List<MapMarkerInfo.MarkerType> removingTypes = null)
            {
                CrackMarkerTag(Marker, out MapMarkerInfo.MarkerType type, out string _, out int _);
                if ((removingTypes == null) || (removingTypes.Count == 0) || removingTypes.Contains(type))
                {
                    map.RemoveElement(Marker);
                    map.RemoveElement(EditHandle);
                    ThreatRing?.Remove(map);
                    return true;
                }
                return false;
            }
        }

        // ================================================================================================================

        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        // number of different route colors, needs to be in sync with resources in .xaml that instantiates control.
        //
        private const int NUM_ROUTE_COLORS = 4;

        // delta (in decimal degrees) tp offset the edit handle for a route endpoint.
        //
        private const double EDIT_HANDLE_DELTA = 2.0 / 60.0;

        // movement threshold (squared) that is considered "moving" when dragging edit handles.
        //
        private const double DIST2_MOVE_THRESHOLD = (7.0 * 7.0);

        // defines state of map or marker drags for managing ui behaviors.
        //
        private enum DragStateEnum
        {
            IDLE = 0,                   // idle
            READY_MARKER = 1,           // pointer has been pressed (but not released) on a marker
            ACTIVE_MARKER = 2,          // actively dragging the previously-selected marker
            READY_MAP = 3,              // pointer has been pressed (but not released) on the map
            ACTIVE_MAP = 4              // actively dragging the map
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- dependency properties

        public static readonly DependencyProperty MouseWheelZoomDeltaProperty =
            DependencyPropertyHelper.Register<MapControl, double>(nameof(MouseWheelZoomDelta), 0.25);

        public static readonly DependencyProperty MouseWheelZoomAnimatedProperty =
            DependencyPropertyHelper.Register<MapControl, bool>(nameof(MouseWheelZoomAnimated), true);

        // ---- public properties

        public MapMarkerInfo.MarkerTypeMask EditMask { get; set; }

        public int MaxRouteLength { get; set; }

        public double MouseWheelZoomDelta
        {
            get => (double)GetValue(MouseWheelZoomDeltaProperty);
            set => SetValue(MouseWheelZoomDeltaProperty, value);
        }

        public bool MouseWheelZoomAnimated
        {
            get => (bool)GetValue(MouseWheelZoomAnimatedProperty);
            set => SetValue(MouseWheelZoomAnimatedProperty, value);
        }

        public IMapControlMarkerPopupFactory MarkerPopupFactory { get; set; }

        // ---- computed properties

        public bool CanEditSelectedMarker => CanEditMarker(_selectedMarker);

        public MapMarkerInfo SelectedMarkerInfo => (_selectedMarker != null) ? new(_selectedMarker) : null;

        // ---- private properties

        private MapMarkerControl _selectedMarker = null;
        private MapMarkerControl _mouseOverMarker = null;
        private Popup _mouseOverPopup = null;
        private int _mouseOverSerial = 0;

        private VirtualKeyModifiers _lastPressKeyModifiers;

        private DragStateEnum _dragState = DragStateEnum.IDLE;
        private bool _isNewMarker = false;

        private MapPathControlLayered _theaterBounds;

        // ---- read-only properties

        private readonly Dictionary<string, PathInfo> _paths = [ ];
        private readonly Dictionary<string, MarkerInfo> _marks = [ ];

        private readonly Dictionary<string, string> _brushIndexMap = [ ];

        private readonly Dictionary<MapMarkerInfo.MarkerType, int> _mapMarkerZ = new()
        {
            // z of 4 reserved for theater bounds, MAP_MARKER_Z_THEATER_BOX
            // z of 5 reserved for threat rings, MAP_MARKER_Z_PATH_RING
            //
            [MapMarkerInfo.MarkerType.POI_SYSTEM] = 10,
            [MapMarkerInfo.MarkerType.BULLSEYE] = 11,
            [MapMarkerInfo.MarkerType.POI_USER] = 12,
            [MapMarkerInfo.MarkerType.POI_CAMPAIGN] = 13,
            //
            // z of 20 reserved for route paths, MAP_MARKER_Z_PATH
            //
            [MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE] = 21,
            [MapMarkerInfo.MarkerType.RING_EDIT_HANDLE] = 22,
            [MapMarkerInfo.MarkerType.UNIT_FRIEND] = 23,
            [MapMarkerInfo.MarkerType.UNIT_ENEMY] = 24,
            [MapMarkerInfo.MarkerType.NAV_PT] = 25,
            [MapMarkerInfo.MarkerType.USER_PT] = 26,
            //
            // z of 30 reserved for current selection, MAP_MARKER_Z_SELECTION
        };

        private const int MAP_MARKER_Z_THEATER_BOX = 4;
        private const int MAP_MARKER_Z_PATH_RING = 5;
        private const int MAP_MARKER_Z_PATH = 20;
        private const int MAP_MARKER_Z_SELECTION = 30;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapControl()
        {
            ManipulationMode = ManipulationModes.Scale
                             | ManipulationModes.TranslateX
                             | ManipulationModes.TranslateY
                             | ManipulationModes.TranslateInertia;

            PointerWheelChanged += OnPointerWheelChanged;
            PointerMoved += OnPointerMoved;
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            DoubleTapped += OnDoubleTapped;
            ManipulationDelta += OnManipulationDelta;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return Location instance initialized from string lat/lon.
        /// </summary>
        public static Location Location(string lat, string lon) => new(double.Parse(lat), double.Parse(lon));

        public static Location Location(INavpointInfo navpt) => new(double.Parse(navpt.Lat), double.Parse(navpt.Lon));

        /// <summary>
        /// remove the element from our canvas if it is not null.
        /// </summary>
        private void RemoveElement(UIElement element)
        {
            if (element != null)
                Children.Remove(element);
        }

        /// <summary>
        /// programatically unselect the marker if there is a current selection. invokes a select verb to inform
        /// others selection has cleared.
        /// </summary>
        private void DoUnselectMarker()
        {
            if (_selectedMarker != null)
            {
                UnselectMarker(_selectedMarker);
                VerbMirror?.MirrorVerbMarkerSelected(this, new());
                _selectedMarker = null;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // control setup
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the resource name for the marker type.
        /// </summary>
        private string GetResourceName(MapMarkerInfo.MarkerType markerType, string what, string index)
        {
            string rdOnly = (!CanEdit(markerType)) ? "_RO" : "";
            return markerType switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM
                    => $"{what}_POI_Core_{index}{rdOnly}",
                MapMarkerInfo.MarkerType.POI_USER
                    => $"{what}_POI_User_{index}{rdOnly}",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN
                    => $"{what}_POI_Campaign_{index}{rdOnly}",
                MapMarkerInfo.MarkerType.NAV_PT
                or MapMarkerInfo.MarkerType.USER_PT
                or MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE
                    => $"{what}_Path_{index}{rdOnly}",
                MapMarkerInfo.MarkerType.UNIT_ENEMY
                    => $"{what}_Unit_0{rdOnly}",
                MapMarkerInfo.MarkerType.UNIT_FRIEND
                or MapMarkerInfo.MarkerType.RING_EDIT_HANDLE
                    => $"{what}_Unit_1{rdOnly}",
// TODO: fix this
                MapMarkerInfo.MarkerType.BULLSEYE
                    => null,
                _ => $"{what}_Default",
            };
        }

        /// <summary>
        /// return the resource object associated with a marker type.
        /// </summary>
        private object GetResource(MapMarkerInfo.MarkerType markerType, string what, string index)
            => (Resources.TryGetValue(GetResourceName(markerType, what, index), out object rsrc)) ? rsrc : null;

        /// <summary>
        /// returns the latitude and longitude extents for all known markers.
        /// </summary>
        public void GetMarkerExtents(double dP,
                                     out double minLat, out double maxLat, out double minLon, out double maxLon)
        {
            minLat = 90.0;
            maxLat = -90.0;
            minLon = 180.0;
            maxLon = -180.0;
            foreach (var child in Children)
            {
                MapMarkerControl marker = child as MapMarkerControl;
                if (marker != null)
                {
                    CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string _, out int _);
                    if (type != MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE)
                    {
                        minLat = Math.Min(minLat, marker.Location.Latitude - dP);
                        maxLat = Math.Max(maxLat, marker.Location.Latitude + dP);
                        minLon = Math.Min(minLon, marker.Location.Longitude - dP);
                        maxLon = Math.Max(maxLon, marker.Location.Longitude + dP);
                    }
                }
            }
        }

        /// <summary>
        /// returns the latitude and longitude extents for all known markers as a BoundingBox.
        /// </summary>
        public BoundingBox GetMarkerBoundingBox(double dP = 0.0)
        {
            GetMarkerExtents(dP, out double minLat, out double maxLat, out double minLon, out double maxLon);
            return new(minLat, minLon, maxLat, maxLon);
        }

        /// <summary>
        /// set the theater to focus on for the map control and add the theater bounding box to the map if the
        /// theater is known. any previous theater bounds are removed.
        /// </summary>
        public void SetTheater(string theater)
        {
            if (_theaterBounds != null)
                Children.Remove(_theaterBounds.ControlFg);
            _theaterBounds = null;

            if (Theater.TheaterInfo.TryGetValue(theater, out TheaterInfo info))
            {
                ObservableCollection<Location> path = [
                    new Location(info.LatMin, info.LonMin),
                    new Location(info.LatMin, info.LonMax),
                    new Location(info.LatMax, info.LonMax),
                    new Location(info.LatMax, info.LonMin),
                    new Location(info.LatMin, info.LonMin)
                ];
                _theaterBounds = new()
                {
                    ControlBg = null                            // theater bounds geometry has no bg layer
                };
                _theaterBounds.Paths.Add(new MapPathControlPath(path));
                _theaterBounds.ControlFg = BuildMapItemsControl("<TheaterBoundary>", false, MAP_MARKER_Z_THEATER_BOX,
                                                                "Tmplt_Boundary_Path", _theaterBounds.Paths);
            }
        }

        /// <summary>
        /// clear all paths currently associated with the control along with the current selection. selection
        /// verb is invoked for the selection clear. delete verb(s)) are not invoked for element(s) removed from
        /// the map.
        /// </summary>
        public void ClearPaths()
        {
            DoUnselectMarker();
            foreach (PathInfo pathInfo in _paths.Values)
                pathInfo.Remove(this);
            _paths.Clear();
        }

        /// <summary>
        /// set path visibility based on the visibility lambda and clear the selection. lambda takes a tag string
        /// argument and returns true if the path should be visible, false if it should be collapsed. selection
        /// verb is invoked for the selection clear.
        /// </summary>
        public void PathVisibility(Func<string, bool> fnIsVisible)
        {
            DoUnselectMarker();
            foreach (KeyValuePair<string, PathInfo> kvp in _paths)
                kvp.Value.Visibility(fnIsVisible(kvp.Key) ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// clear all markers matching a removal type that are currently associated with the control along with the
        /// current selection. selection verb is invoked for the selection clear. delete verb(s) are not invoked for
        /// element(s) removed from the map.
        /// </summary>
        public void ClearMarkers(List<MapMarkerInfo.MarkerType> removingTypes)
        {
            DoUnselectMarker();
            Dictionary<string, MarkerInfo> tmpMarks = new(_marks);
            foreach (KeyValuePair<string, MarkerInfo> kvp in tmpMarks)
                if (kvp.Value.Remove(this, removingTypes))
                    _marks.Remove(kvp.Key);
        }

        /// <summary>
        /// set marker visibility based on the visibility lambda and clear the selection. on isRingOnly, only those
        /// markers with a ring are updated. lambda takes marker type, tag string, and boolean "has ring" argument
        /// and returns a tuple of two booleans true if the marker, ring should be visible, false if marker, ring
        /// should be collapsed. selection verb is invoked for the selection clear.
        /// </summary>
        public void MarkerVisibility(Func<MapMarkerInfo.MarkerType, string, bool, Tuple<bool, bool>> fnIsVisible)
        {
            DoUnselectMarker();
            foreach (KeyValuePair<string, MarkerInfo> kvp in _marks)
            {
                CrackMarkerTag(kvp.Value.Marker, out MapMarkerInfo.MarkerType type, out string tag, out _);
                Tuple<bool, bool> isViz = fnIsVisible(type, tag, (kvp.Value.ThreatRing != null));
                kvp.Value.Visibility(isViz.Item1 ? Visibility.Visible : Visibility.Collapsed,
                                     isViz.Item2 ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        /// <summary>
        /// add a path to the control by creating markers for each navigation point along the route as well as
        /// geometries for the lines that connect points on the route. add verbs are not invoked.
        /// </summary>
        public void AddPath(MapMarkerInfo.MarkerType markerType, string tag, ObservableCollection<Location> npts)
        {
            if (!_brushIndexMap.ContainsKey(tag))
                _brushIndexMap[tag] = $"{_paths.Count % NUM_ROUTE_COLORS}";

            PathInfo pathInfo = new();

            pathInfo.Path.Paths.Add(new(npts));
            pathInfo.Path.ControlBg = BuildPathControl(markerType, pathInfo.Path, tag, false);
            pathInfo.Path.ControlFg = BuildPathControl(markerType, pathInfo.Path, tag, true);
            pathInfo.Path.ControlBg.Visibility = Visibility.Collapsed;

            pathInfo.EditHandleNeg = MarkerFactory(MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE, tag, -1);
            pathInfo.EditHandlePos = MarkerFactory(MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE, tag, -1);

            for (int i = 0; i < npts.Count; i++)
                pathInfo.Points.Add(MarkerFactory(markerType, tag, i + 1, npts[i]));

            _paths[tag] = pathInfo;
        }

        /// <summary>
        /// add a mark to the control by creating a new maker at a lat/lon and adding it to the list of known
        /// marks. add verbs are not invoked.
        /// </summary>
        public void AddMarker(MapMarkerInfo.MarkerType type, string tag, Location location, double radiusRing = 0.0,
                              bool isRingMarkerHidden = true)
        {
            MarkerInfo markerInfo = new()
            {
                Marker = MarkerFactory(type, tag, -1, location),
                RadiusThreatRing = radiusRing
            };

            if (radiusRing > 0.0)
            {
                MapPathControlLayered ring = new();
                ObservableCollection<Location> path = [.. MapThreatPath.MakeLocationsForThreat(location, radiusRing) ];
                ring.Paths.Add(new MapPathControlPath(path));
                ring.ControlBg = BuildMapItemsControl(tag, false, MAP_MARKER_Z_PATH_RING, $"Tmplt_Ring_Bg",
                                                      ring.Paths);
                ring.ControlFg = BuildMapItemsControl(tag, false, MAP_MARKER_Z_PATH_RING,
                                                      GetResourceName(type, "Tmplt_Ring", "0"), ring.Paths);
                ring.ControlBg.Visibility = Visibility.Collapsed;

                markerInfo.ThreatRing = ring;

                if (isRingMarkerHidden)
                    markerInfo.Marker.Visibility = Visibility.Collapsed;
            }
            if (type == MapMarkerInfo.MarkerType.USER_PT)
                markerInfo.EditHandle = MarkerFactory(MapMarkerInfo.MarkerType.RING_EDIT_HANDLE, tag, -1);

            _marks[tag] = markerInfo;
        }

        /// <summary>
        /// helper function to build a map items control from the given parameters and add it to the canvas at the
        /// specified z depth. the control is built using a data template with the specified name that will set the
        /// specific type of control created.
        /// </summary>
        private MapItemsControl BuildMapItemsControl(string tag, bool isHittable, int zIndex, string templateName,
                                                     object itemsSource)
        {
            MapItemsControl control = new()
            {
                Tag = tag,
                IsHitTestVisible = isHittable,
            };
            Canvas.SetZIndex(control, zIndex);
            Children.Add(control);
            //
            // TODO: since paths use MapItemsControl (a ListBox), they need a data template. no way to build those
            // TODO: programatically without basically parsing xaml here. this implies that colors are (for now)
            // TODO: are setup and specified in .xaml.
            //
            if (Resources.TryGetValue(templateName, out object template))
                control.ItemTemplate = template as DataTemplate;
            control.ItemsSource = itemsSource;
            return control;
        }

        /// <summary>
        /// helper function to build out a path control.
        /// </summary>
        private MapItemsControl BuildPathControl(MapMarkerInfo.MarkerType markerType, MapPathControlLayered info,
                                                 string tag, bool isFg)
        {
            string rsrcName = (isFg) ? GetResourceName(markerType, "Tmplt", _brushIndexMap.GetValueOrDefault(tag, "0"))
                                     : "Tmplt_Path_Bg";
            return BuildMapItemsControl(tag, true, MAP_MARKER_Z_PATH, rsrcName, info.Paths);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // marker utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the data source allows edits on markers of the given type.
        /// </summary>
        private bool CanEdit(MapMarkerInfo.MarkerType type)
            => (type != MapMarkerInfo.MarkerType.UNKNOWN) &&
               EditMask.HasFlag((MapMarkerInfo.MarkerTypeMask)(1 << (int)type));

        private bool CanEditMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string _, out int _);
            return CanEdit(type);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private MapMarkerControl MarkerFactory(MapMarkerInfo.MarkerType type, string tagStr, int tagInt,
                                               Location loc = null)
        {
            Brush brush = GetResource(type, "Brush_Marker", _brushIndexMap.GetValueOrDefault(tagStr, "0")) as Brush
                          ?? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            MapMarkerControl marker = type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM
                    => new MapMarkerSquareControl(brush, brush, new Size(20.0, 20.0)),
                MapMarkerInfo.MarkerType.POI_USER
                    => new MapMarkerCircleControl(brush, brush, new Size(22.0, 22.0)),
                MapMarkerInfo.MarkerType.POI_CAMPAIGN
                    => new MapMarkerCircleControl(brush, brush, new Size(22.0, 22.0)),
                MapMarkerInfo.MarkerType.NAV_PT
                    => new MapMarkerDiamondControl(brush, brush, new Size(24.0, 24.0)),
                MapMarkerInfo.MarkerType.UNIT_ENEMY
                    => new MapMarkerTriangleControl(brush, brush, new Size(22.0, 22.0)),
                MapMarkerInfo.MarkerType.UNIT_FRIEND
                    => new MapMarkerTriangleControl(brush, brush, new Size(22.0, 22.0)),
                MapMarkerInfo.MarkerType.BULLSEYE
                    => new MapMarkerTriangleControl(brush, brush, new Size(22.0, 22.0)),
                MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE
                or MapMarkerInfo.MarkerType.RING_EDIT_HANDLE
                    => new MapMarkerCircleControl(brush, brush, new Size(18.0, 18.0))
                    {
                        Visibility = Visibility.Collapsed
                    },
                _ => new MapMarkerCircleControl(),
            };
            marker.Location = loc ?? new Location(0.0, 0.0);
            marker.Tag = TagForMarkerOfKind(type, tagStr, tagInt);

            marker.PointerEntered += Marker_PointerEntered;
            marker.PointerExited += Marker_PointerExited;

            Canvas.SetZIndex(marker, _mapMarkerZ[type]);
            Children.Add(marker);
            return marker;
        }

        /// <summary>
        /// break or create a tag object from a MapMarkerControl into it's constituent pieces. marker tags (navpoints
        /// and handles) are a tuple of the form: { [type], [integer], [string] }.
        ///
        /// navpoints        [type]      MapMarkerInfo.MarkerType.NAVPT
        ///                  [string]    path tag for this._paths
        ///                  [integer]   number of the navpoint in the path list (so, 1-based index)
        ///
        /// edit handles     [type]      MapMarkerInfo.MarkerType.NAVPT_HANDLE
        ///                  [string]    path tag for this._paths the edit handle is associated with
        ///                  [integer]   position in the navpoint list where a new point should be inserted (0 implies
        ///                              before first point)
        ///
        /// all others       [type]      MapMarkerInfo.MarkerType.[others]
        ///                  [string]    marker tag for this._marks
        ///                  [integer]   -1
        ///
        /// when tag is null, returns type of UNKNOWN, tagInt of -1, and tagStr of null.
        /// </summary>
        private static void CrackMarkerTag(MapMarkerControl marker,
                                           out MapMarkerInfo.MarkerType type, out string tagStr, out int tagInt)
        {
            if ((marker == null) || (marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int> == null))
            {
                type = MapMarkerInfo.MarkerType.UNKNOWN;
                tagStr = null;
                tagInt = -1;
            }
            else
            {
                Tuple<MapMarkerInfo.MarkerType, string, int> tuple = marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int>;
                type = tuple.Item1;
                tagStr = tuple.Item2;
                tagInt = tuple.Item3;
            }
        }

        /// <summary>
        /// return tag used to identify MapMarkerControl, tags are tuples of the form: { [type], [integer], [string] }.
        /// </summary>
        private static Tuple<MapMarkerInfo.MarkerType, string, int> TagForMarkerOfKind(MapMarkerInfo.MarkerType type,
                                                                                       string tagStr, int tagInt)
            => new(type, tagStr, tagInt);

        /// <summary>
        /// return the first MapMarkerControl that is a parent of the starting source framework element from the view
        /// hierarhcy, null if none is found.
        /// </summary>
        private static MapMarkerControl IsSourceMarker(object source)
        {
            for (object elem = source;
                 (elem != null) && (elem is FrameworkElement) && (elem is not MapControl);
                 elem = (elem as FrameworkElement).Parent)
            {
                if (elem is MapMarkerControl)
                    return elem as MapMarkerControl;
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // route marker management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// drag an editable map marker to a new location. this updates edit handles as well.
        /// </summary>
        private void MoveMapMarker(MapMarkerControl marker, Location newLocation)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string str, out int number);
            if (CanEdit(type) && (type == MapMarkerInfo.MarkerType.NAV_PT))
            {
                _isNewMarker = false;

                marker.Location = newLocation;

                PathInfo pathInfo = _paths[str];

                HideEditHandle(pathInfo.EditHandlePos);
                HideEditHandle(pathInfo.EditHandleNeg);

                pathInfo.Points[number - 1].Location = newLocation;

                Debug.Assert(pathInfo.Path.Paths.Count == 1);

                pathInfo.Path.Paths[0].Locations.RemoveAt(number - 1);
                pathInfo.Path.Paths[0].Locations.Insert(number - 1, newLocation);
            }
            else if (CanEdit(type) && (type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE))
            {
                _isNewMarker = true;

                PathInfo pathInfo = _paths[str];
                HideEditHandle(pathInfo.EditHandlePos);
                HideEditHandle(pathInfo.EditHandleNeg);

                _selectedMarker = AddMarkerToPath(str, number, newLocation);
            }
            else if (CanEdit(type))
            {
// TODO: need to move threat ring too...
                _marks[str].Marker.Location = newLocation;
            }
        }

        /// <summary>
        /// add a new marker to a path at the given index in the path. creates the marker ui object and updates the
        /// path to include the new point. returns the MapMarkerInfo for the new marker.
        /// </summary>
        private MapMarkerControl AddMarkerToPath(string pathTag, int number, Location newLocation)
        {
            PathInfo pathInfo = _paths[pathTag];

            MapMarkerControl marker = MarkerFactory(MapMarkerInfo.MarkerType.NAV_PT, pathTag, 0, newLocation);
            marker.VisualState = VisualStateMask.SHOW_BG;

            pathInfo.Points.Insert(number - 1, marker);
            for (int i = 0; i < pathInfo.Points.Count; i++)
                pathInfo.Points[i].Tag = TagForMarkerOfKind(MapMarkerInfo.MarkerType.NAV_PT, pathTag, i + 1);

            pathInfo.Path.Paths[0].Locations.Insert(number - 1, newLocation);

            return marker;
        }

        /// <summary>
        /// return the location for the path edit handle between points p0 (the selected point on the path) and an
        /// adjacent point p1. there are three cases: (1) within a small distance left/right of p0 if there is only
        /// one point, (2) midway between p0 and p1 if both points are in the point list, and (3) a small distance
        /// beyond p0 along the line from the adjacent point if p0 is an endpoint of the path.
        /// </summary>
        private static Location LocatePathHandle(List<MapMarkerControl> points, int indexP0, int indexP1)
        {
            double dLat = 0.0;
            double dLon = (indexP1 > indexP0) ? EDIT_HANDLE_DELTA : -EDIT_HANDLE_DELTA;

            Location p0Loc = points[indexP0].Location;
            if (points.Count > 1)
            {
                if (((indexP0 >= 0) && (indexP0 < points.Count)) &&
                    ((indexP1 >= 0) && (indexP1 < points.Count)))
                {
                    Location p1Loc = points[indexP1].Location;
                    dLat = (p1Loc.Latitude - p0Loc.Latitude) / 2.0;
                    dLon = (p1Loc.Longitude - p0Loc.Longitude) / 2.0;
                }
                else
                {
                    Location p1Loc = (indexP0 == 0) ? points[1].Location : points[^2].Location;
                    double len = Math.Sqrt(Math.Pow(p1Loc.Latitude - p0Loc.Latitude, 2) +
                                           Math.Pow(p1Loc.Longitude - p0Loc.Longitude, 2));
                    dLat = ((p0Loc.Latitude - p1Loc.Latitude) / len) * EDIT_HANDLE_DELTA;
                    dLon = ((p0Loc.Longitude - p1Loc.Longitude) / len) * EDIT_HANDLE_DELTA;
                }
            }
            return new(p0Loc.Latitude + dLat, p0Loc.Longitude + dLon);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string tagStr, out int tagInt);
            bool isPath = (type == MapMarkerInfo.MarkerType.NAV_PT);
            bool isEditHandle = (type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE);
            double dtHandle = (_dragState == DragStateEnum.IDLE) ? 0.10 : 0.30;
            PathInfo pathInfo = _paths.GetValueOrDefault(tagStr, null);

            Canvas.SetZIndex(marker, MAP_MARKER_Z_SELECTION);

            if (isPath || isEditHandle)
            {
                foreach (MapMarkerControl pathMarker in pathInfo.Points)
                    pathMarker.VisualState = VisualStateMask.SHOW_BG | VisualStateMask.SHOW_FILL;

                pathInfo.Path.ControlBg.Visibility = Visibility.Visible;
                pathInfo.Path.ControlFg.Visibility = Visibility.Visible;
            }

            if (isPath && !isEditHandle)
            {
                marker.VisualState &= ~VisualStateMask.SHOW_FILL;

                if (CanEdit(MapMarkerInfo.MarkerType.NAV_PT) &&
                    (pathInfo.Points.Count < MaxRouteLength) &&
                    (_dragState != DragStateEnum.ACTIVE_MARKER))
                {
                    Utilities.DispatchAfterDelay(DispatcherQueue, dtHandle, false,
                        (sender, evt) =>
                        {
                            // NOTE: remember, intParam from navpoint and handle control tags is navpoint number, *not*
                            // NOTE: an array index...

                            ShowEditHandleAtLocation(pathInfo.EditHandleNeg,
                                                     TagForMarkerOfKind(MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE, tagStr, tagInt),
                                                     LocatePathHandle(pathInfo.Points, tagInt - 1, tagInt - 2));

                            ShowEditHandleAtLocation(pathInfo.EditHandlePos,
                                                     TagForMarkerOfKind(MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE, tagStr, tagInt + 1),
                                                     LocatePathHandle(pathInfo  .Points, tagInt - 1, tagInt));
                        });
                }
            }
            if (isEditHandle)
                ShowEditHandleAtLocation(marker);
            else if (!isPath)
            {
                marker.VisualState = VisualStateMask.SHOW_BG;
                if (_marks.TryGetValue(tagStr, out MarkerInfo info) &&
                    (info.ThreatRing != null) &&
                    (info.ThreatRing.ControlBg != null))
                {
                    info.ThreatRing.ControlBg.Visibility = Visibility.Visible;
                    Canvas.SetZIndex(info.ThreatRing.ControlBg, MAP_MARKER_Z_SELECTION);
                    Canvas.SetZIndex(info.ThreatRing.ControlFg, MAP_MARKER_Z_SELECTION);
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void UnselectMarker(MapMarkerControl marker)
        {
            CrackMarkerTag(marker, out MapMarkerInfo.MarkerType type, out string tagStr, out int _);
            bool isPath = (type == MapMarkerInfo.MarkerType.NAV_PT);
            bool isEditHandle = (type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE);
            PathInfo pathInfo = _paths.GetValueOrDefault(tagStr, null);

            Canvas.SetZIndex(marker, _mapMarkerZ[type]);

            if (isPath || isEditHandle)
                foreach (MapMarkerControl pathMarker in pathInfo.Points)
                    pathMarker.VisualState = VisualStateMask.SHOW_FILL;

            if (isPath && !isEditHandle)
            {
                pathInfo.Path.ControlFg.Visibility = Visibility.Visible;
                pathInfo.Path.ControlBg.Visibility = Visibility.Collapsed;
                HideEditHandle(pathInfo.EditHandleNeg);
                HideEditHandle(pathInfo.EditHandlePos);
            }
            if (!isEditHandle)
                marker.VisualState = VisualStateMask.SHOW_FILL;

            if (!isPath)
            {
                marker.VisualState = VisualStateMask.SHOW_FILL;
                if (_marks.TryGetValue(tagStr, out MarkerInfo info) &&
                    (info.ThreatRing != null) &&
                    (info.ThreatRing.ControlBg != null))
                {
                    info.ThreatRing.ControlBg.Visibility = Visibility.Collapsed;
                    Canvas.SetZIndex(info.ThreatRing.ControlBg, MAP_MARKER_Z_PATH_RING);
                    Canvas.SetZIndex(info.ThreatRing.ControlFg, MAP_MARKER_Z_PATH_RING);
                }
            }
        }

        /// <summary>
        /// shows an edit handle via its visibility. optionally update the tag and/or location of the handle prior
        /// to showing.
        /// </summary>
        private static void ShowEditHandleAtLocation(MapMarkerControl marker,
                                                     Tuple<MapMarkerInfo.MarkerType, string, int> tag = null,
                                                     Location location = null)
        {
            marker.Tag = tag ?? marker.Tag;
            marker.Location = location ?? marker.Location;
            marker.VisualState = VisualStateMask.SHOW_FILL | VisualStateMask.SHOW_BG;
            marker.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// hides an edit handle via its visibility.
        /// </summary>
        private static void HideEditHandle(MapMarkerControl marker)
        {
            marker.VisualState = VisualStateMask.SHOW_FILL | VisualStateMask.SHOW_BG;
            marker.Visibility = Visibility.Collapsed;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "MapControl";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// handle a change to the selected marker from another source of map actions.
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"MC:MarkerSelect({param}) {info.Type}, {info.TagStr} {info.TagInt}");
            if (_selectedMarker != null)
                UnselectMarker(_selectedMarker);
            _selectedMarker = null;

            if ((info.TagStr != null) && (_paths.TryGetValue(info.TagStr, out PathInfo pathInfo)))
            {
                _selectedMarker = pathInfo.Points[info.TagInt - 1];
                SelectMarker(_selectedMarker);
            }
            else if ((info.TagStr != null) && (_marks.TryGetValue(info.TagStr, out MarkerInfo markerInfo)))
            {
                _selectedMarker = markerInfo.Marker;
                SelectMarker(_selectedMarker);
            }
        }

        /// <summary>
        /// map does not have anything to do on opens originating from other senders.
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0) { }

        /// <summary>
        /// handle the change of a marker location.
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"MC:VerbMarkerMoved({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if ((info.TagStr == null) || !CanEdit(info.Type))
                return;
            else if (_paths.TryGetValue(info.TagStr, out PathInfo pathInfo))
                MoveMapMarker(pathInfo.Points[info.TagInt - 1], Location(info.Lat, info.Lon));
            else if (_marks.TryGetValue(info.TagStr, out MarkerInfo markerInfo))
                MoveMapMarker(markerInfo.Marker, Location(info.Lat, info.Lon));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"MC:VerbMarkerAdded({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if ((info.TagStr == null) || !CanEdit(info.Type))
                return;
            else if (_paths.TryGetValue(info.TagStr, out PathInfo pathInfo))
            {
                HideEditHandle(pathInfo.EditHandlePos);
                HideEditHandle(pathInfo.EditHandleNeg);
                AddMarkerToPath(info.TagStr, info.TagInt, Location(info.Lat, info.Lon));
            }
            else
            {
                AddMarker(info.Type, info.TagStr, Location(info.Lat, info.Lon));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"MC:VerbMarkerDeleted({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == null)
                return;

            CrackMarkerTag(_selectedMarker, out MapMarkerInfo.MarkerType _, out string tagStr, out int _);
            if (tagStr == info.TagStr)
            {
                UnselectMarker(_selectedMarker);
                _selectedMarker = null;
            }

            if (CanEdit(info.Type) && _paths.TryGetValue(info.TagStr, out PathInfo pathInfo))
            {
                MapMarkerControl marker = pathInfo.Points[info.TagInt - 1];
                Children.Remove(marker);

                pathInfo.Points.RemoveAt(info.TagInt - 1);
                for (int i = 0; i < pathInfo.Points.Count; i++)
                    pathInfo.Points[i].Tag = TagForMarkerOfKind(info.Type, info.TagStr, i + 1);
                pathInfo.Path.Paths[0].Locations.RemoveAt(info.TagInt - 1);
            }
            else if (CanEdit(info.Type) && _marks.TryGetValue(info.TagStr, out MarkerInfo markerInfo))
            {
                markerInfo.Remove(this);
                _marks.Remove(info.TagStr);
            }
// TODO: handle delete of markers w/ threat rings?
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // event handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on mouse wheel, zoom to integer multiple of MouseWheelZoomDelta when the event was raised by a mouse wheel
        /// or by a large movement on a touch pad or other high resolution device.
        /// </summary>
        private void OnMouseWheel(Windows.Foundation.Point position, double delta)
        {
            var zoomLevel = TargetZoomLevel + MouseWheelZoomDelta * delta;
            var animated = false;

            if ((_dragState != DragStateEnum.READY_MARKER) || (_dragState != DragStateEnum.ACTIVE_MARKER))
            {
                if ((delta <= -1d) || (delta >= 1d))
                {
                    zoomLevel = MouseWheelZoomDelta * Math.Round(zoomLevel / MouseWheelZoomDelta);
                    animated = MouseWheelZoomAnimated;
                }
                ZoomMap(position, zoomLevel, animated);
            }
        }

        /// <summary>
        /// on pointer wheel changes, convert the event to a mouse wheel event and pass off to OnMouseWheel().
        /// </summary>
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs evt)
        {
            if ((_dragState != DragStateEnum.READY_MARKER) || (_dragState != DragStateEnum.ACTIVE_MARKER))
            {
                Microsoft.UI.Input.PointerPoint point = evt.GetCurrentPoint(this);
                OnMouseWheel(point.Position, point.Properties.MouseWheelDelta / 120d);
            }
        }

        /// <summary>
        /// on pointer pressed, select a marker and get ready to drag it (if press is in a marker) or clear
        /// the selection and get ready to drag the map (if press outside a marker). only left mouse button
        /// is tracked. returns to idle if no match. marks event as handled.
        /// </summary>
        protected void OnPointerPressed(object sender, PointerRoutedEventArgs evt)
        {
            _isNewMarker = false;
            _lastPressKeyModifiers = evt.KeyModifiers;

            MapMarkerControl marker = IsSourceMarker(evt.OriginalSource);
            if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed && (marker != null))
            {
                if (_selectedMarker != marker)
                {
                    if (_selectedMarker != null)
                        UnselectMarker(_selectedMarker);
                    SelectMarker(marker);
                    _selectedMarker = marker;
                    VerbMirror?.MirrorVerbMarkerSelected(this, new(_selectedMarker));
                }
                _dragState = DragStateEnum.READY_MARKER;
            }
            else if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _dragState = DragStateEnum.READY_MAP;
            }
            else
            {
                _dragState = DragStateEnum.IDLE;
            }

            evt.Handled = true;
        }

        /// <summary>
        /// on pointer release, TODO
        /// </summary>
        protected void OnPointerReleased(object sender, PointerRoutedEventArgs evt)
        {
            if (_dragState == DragStateEnum.ACTIVE_MARKER)
            {
                // mark end of drag by sending a moved verb in the final location with a param of 1.
                //
                VerbMirror?.MirrorVerbMarkerMoved(this, new(_selectedMarker), 1);

                _dragState = DragStateEnum.IDLE;                // force SelectMarker() to restore handles...
                SelectMarker(_selectedMarker);
            }
            else if (_dragState == DragStateEnum.READY_MAP)
            {
                if (_selectedMarker != null)
                    UnselectMarker(_selectedMarker);
                _selectedMarker = null;
                VerbMirror?.MirrorVerbMarkerSelected(this, new());
            }

            _dragState = DragStateEnum.IDLE;
            _isNewMarker = false;

            evt.Handled = true;
        }

        /// <summary>
        /// on double-tap, TODO
        /// </summary>
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs evt)
        {
            MapMarkerControl marker = IsSourceMarker(evt.OriginalSource);
            if (marker != null)
                VerbMirror?.MirrorVerbMarkerOpened(this, new(marker));
            else if (_lastPressKeyModifiers == VirtualKeyModifiers.None)
                Center = ViewToLocation(evt.GetPosition(this));
            else if (_lastPressKeyModifiers == VirtualKeyModifiers.Shift)
                ZoomToBounds(GetMarkerBoundingBox(2.0));
            _dragState = DragStateEnum.IDLE;
        }

        /// <summary>
        /// on pointer move, enable manipulation of the map if the left button is pressed and there are no modifier
        /// keys pressed.
        /// </summary>
        private void OnPointerMoved(object sender, PointerRoutedEventArgs evt)
        {
            if (evt.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (_dragState == DragStateEnum.READY_MAP)
                {
                    _dragState = (evt.KeyModifiers == VirtualKeyModifiers.None) ? DragStateEnum.ACTIVE_MAP
                                                                                : DragStateEnum.IDLE;
                }
                else if (_dragState == DragStateEnum.ACTIVE_MARKER)
                {
                    MoveMapMarker(_selectedMarker, ViewToLocation(evt.GetCurrentPoint(this).Position));
                    //
                    // if we're moving an edit handle, MoveMapMarker will take care of creating a new marker to
                    // replace the handle (setting it to the selected marker) and getting the handle out of the
                    // way (_isNewMarker will indicate this state). we'll send an initial moved verb with a -1
                    // param to indicate a drag is starting. otherwise, just continue to drag.
                    //
                    if (_isNewMarker)
                    {
                        VerbMirror?.MirrorVerbMarkerAdded(this, new(_selectedMarker));
                        VerbMirror?.MirrorVerbMarkerMoved(this, new(_selectedMarker), -1);
                    }
                    else
                    {
                        VerbMirror?.MirrorVerbMarkerMoved(this, new(_selectedMarker));
                    }
                    _isNewMarker = false;
                }
            }
            else
            {
                _dragState = DragStateEnum.IDLE;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs evt)
        {
            if (_dragState == DragStateEnum.READY_MARKER)
            {
                CrackMarkerTag(_selectedMarker, out MapMarkerInfo.MarkerType type, out string _, out int _);
                double dist2 = Math.Pow(evt.Cumulative.Translation.X, 2.0) + Math.Pow(evt.Cumulative.Translation.Y, 2.0);
                if ((dist2 > DIST2_MOVE_THRESHOLD) && CanEdit(type))
                {
                    _dragState = DragStateEnum.ACTIVE_MARKER;
                    if (type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE)
                        _isNewMarker = true;
                    else
                        VerbMirror?.MirrorVerbMarkerMoved(this, new(_selectedMarker), -1);
                }
            }
            else if (_dragState == DragStateEnum.ACTIVE_MAP)
            {
// TODO: tweak translation to keep map on theater?
                TranslateMap(evt.Delta.Translation);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TriggerHover(object tag, int serial)
        {
            if ((_mouseOverMarker != null) && (_mouseOverMarker.Tag == tag) && (_mouseOverSerial == serial))
            {
                _mouseOverPopup = MarkerPopupFactory.MarkerPopup(new(_mouseOverMarker));
                (_mouseOverMarker.Content as Panel).Children.Add(_mouseOverPopup);
                _mouseOverPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void Marker_PointerEntered(object sender, PointerRoutedEventArgs args)
        {
            MapMarkerControl marker = sender as MapMarkerControl;
            if ((marker != null) && (MarkerPopupFactory != null))
            {
                _mouseOverMarker = marker;
                object tag = marker.Tag;
                int enterSerial = ++_mouseOverSerial;
                Utilities.DispatchAfterDelay(DispatcherQueue, 1.5, false, (s, e) => TriggerHover(tag, enterSerial));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void Marker_PointerExited(object sender, PointerRoutedEventArgs args)
        {
            if (_mouseOverPopup != null)
            {
                _mouseOverPopup.IsOpen = false;
                (_mouseOverMarker.Content as Panel).Children.Remove(_mouseOverPopup);
                _mouseOverPopup = null;
            }
            if (_mouseOverMarker != null)
                _mouseOverMarker = null;
        }
    }
}
