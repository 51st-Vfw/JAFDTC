// ********************************************************************************************************************
//
// MapThreatPath.cs : path geometry for a threat ring in map window
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
//
// adapted from code from https://github.com/ClemensFischer/XAML-Map-Control
//
// MIT License
//
// Copyright(c) 2025 Clemens Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without
// limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
// ********************************************************************************************************************

using MapControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// MapPath for the path that defines the wez of a threat. the path is defined by two locations: the center of
    /// the wez and the edge of the wez due east from the center location.
    /// </summary>
    public partial class MapThreatPath : MapPath
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private const double METER_PER_NAUTMILE = 1852.0;
        private const double EARTH_RADIUS = 6378137.0;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty LocationsProperty =
            DependencyPropertyHelper.Register<MapThreatPath, IEnumerable<Location>>(nameof(Locations), null,
                (threatPath, oldValue, newValue) => threatPath.DataCollectionPropertyChanged(oldValue, newValue)
            );

        public IEnumerable<Location> Locations
        {
            get => (IEnumerable<Location>)GetValue(LocationsProperty);
            set => SetValue(LocationsProperty, value);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapThreatPath()
        {
            Data = new PathGeometry();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // support functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the locations enumerable for a threat ring centered at the given lat/lon with the given radius
        /// in nautical miles. locations are the center and the edge of the wez due east from center.
        /// </summary>
        public static List<Location> MakeLocationsForThreat(Location center, double wezRadius)
            => [ center, center.GetLocation(90.0 * (Math.PI / 180.0), (wezRadius * METER_PER_NAUTMILE) / EARTH_RADIUS) ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // object change notifications
        //
        // ------------------------------------------------------------------------------------------------------------

        private void DataCollectionPropertyChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= DataCollectionChanged;
            if (newValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += DataCollectionChanged;

            UpdateData();
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateData();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // geometry updates
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void UpdateData()
        {
            UpdateData(Locations);
        }

        protected void UpdateData(IEnumerable<Location> locations)
        {
            PathFigureCollection figures = ((PathGeometry)Data).Figures;
            figures.Clear();

            if ((ParentMap != null) && (locations != null))
                AddThreatRing(figures, locations, GetLongitudeOffset(Location ?? locations.FirstOrDefault()));
        }

        /// <summary>
        /// add arcs to the geometry for the threat ring.
        /// </summary>
        private void AddThreatRing(PathFigureCollection figures, IEnumerable<Location> locations, double lonOffset)
        {
            IEnumerable<Point> points = locations.Select(location => LocationToView(location, lonOffset))
                                                 .Where(point => point.HasValue)
                                                 .Select(point => point.Value);
            if (points.Count() == 2)
            {
                Point center = points.ElementAt(0);
                Point edge = points.ElementAt(1);
                double radius = Math.Sqrt(Math.Pow(center.X - edge.X, 2.0) + Math.Pow(center.Y - edge.Y, 2.0));

                double minX = center.X - radius;
                double maxX = edge.X;
                double minY = center.Y - radius;
                double maxY = center.Y + radius;

                if ((maxX >= 0) && (minX <= ParentMap.ActualWidth) && (maxY >= 0) && (minY <= ParentMap.ActualHeight))
                {
                    // HACK WARNING: windows gets cranky when the geometry gets really big (for exmaple, when
                    // HACK WARNING: zoomed in). this can cause the gemetry to just disappear at high zoom
                    // HACK WARNING: levels. figure out if we are so zoomed in the entire visible map is under
                    // HACK WARNING: the threat ring. if so, we'll change the geometry so it just covers the
                    // HACK WARNING: visible map.
                    //
                    // set up edgeWEZ and centerWEZ for the geometry.
                    //
                    double distDiag = Math.Sqrt(Math.Pow(ParentMap.ActualWidth, 2.0) +
                                                Math.Pow(ParentMap.ActualHeight, 2.0));
                    double distCenters = Math.Sqrt(Math.Pow((0.5 * ParentMap.ActualWidth) - center.X, 2.0) +
                                                   Math.Pow((0.5 * ParentMap.ActualHeight) - center.Y, 2.0));
                    Point edgeWEZ;
                    Point centerWEZ;
                    if (radius > (distCenters + distDiag))
                    {
                        radius = distDiag * 0.60;
                        centerWEZ = new Point(ParentMap.ActualWidth / 2.0, ParentMap.ActualHeight / 2.0);
                        edgeWEZ = new Point(centerWEZ.X + radius, centerWEZ.Y);
                    }
                    else
                    {
                        centerWEZ = center;
                        edgeWEZ = edge;
                    }

// TODO: this is not quite right. we arc between the east and west points on the circle. when either of these
// TODO: points gets "far enough" from the visible area, the gemetry gets dropped. the above code handles the
// TODO: case where the visible area is entirely contained within the circle (and the circle is large). it
// TODO: does not handle the case where the circle passes through the visible area (and the circle is large).
// TODO: in this case, we really need to clip the circle to the visible area. this can only produce one segment
// TODO: crossing the visible area (if there are multiple segments, the circle must be small enough to not trip
// TODO: the "circle is large" constraint).
// TODO:
// TODO: what we probably need to do is the following:
// TODO:
// TODO: (1) determine distances from view center to east/west ref points.
// TODO: (2) if any distance >= critical value and view area lies entirely within circle,
// TODO:     --> reduce circle size so border is just outside view area 
// TODO: (3) if any distance >= critical value and view area crosses circle,
// TODO:     --> clip circle to view area and draw segment of circle lying between view area edges
// TODO: (4) if all distances < critical value,
// TODO:     --> draw as normal

                    // build out the geometry. note that the center, edge, and radius may have been changed by the
                    // code above if the wez is "too big".
                    //
                    PathFigure figure = new()
                    {
                        StartPoint = edgeWEZ,
                        IsClosed = true,
                    };

                    ArcSegment arcBottom = new()
                    {
                        Point = new Windows.Foundation.Point(edgeWEZ.X - (2.0 * radius), edgeWEZ.Y),
                        Size = new Windows.Foundation.Size(radius, radius),
                        IsLargeArc = true,
                        SweepDirection = SweepDirection.Clockwise
                    };
                    figure.Segments.Add(arcBottom);
                    ArcSegment arcTop = new()
                    {
                        Point = edgeWEZ,
                        Size = new Windows.Foundation.Size(radius, radius),
                        IsLargeArc = true,
                        SweepDirection = SweepDirection.Clockwise
                    };
                    figure.Segments.Add(arcTop);
                    figures.Add(figure);
                }
            }
        }
    }
}
