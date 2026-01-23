// ********************************************************************************************************************
//
// MapThreatPath.cs : path geometry for a threat ring in map window
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

        // (again) don't EVEN think about asking, dude...
        //
        private const double CLIP_HACK_RMAG = 8192.0;

        private const double CLIP_OFFSET = 4.0;
        private const double EPSILON = 1.0e-6;

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
            => [center, center.GetLocation(90.0 * (Math.PI / 180.0), (wezRadius * METER_PER_NAUTMILE) / EARTH_RADIUS)];

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
        /// add arcs to the geometry for the threat ring. locations should contain two points: the first is the
        /// center, and the second is a point on the ring.
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
                    // HACK WARNING: the threat ring based on the wez radius. if so, we'll draw the threat ring
                    // HACK WARNING: as the intersection of a rect and circle, rather than just a circle.
                    //
                    if (radius > CLIP_HACK_RMAG)
                    {
                        List<Point> intersect = FindIntersections(ParentMap.ActualWidth, ParentMap.ActualHeight,
                                                                  center, radius);
                        if (intersect.Count > 0)
                            figures.Add(ClippedCirclePath(center, radius, intersect));
                    }
                    else
                    {
                        figures.Add(CirclePath(edge, radius));
                    }
                }
            }
        }

        /// <summary>
        /// return a path for a circle of radius r that passes through the point due east of the center at e.
        /// </summary>
        private static PathFigure CirclePath(Point e, double r)
        {
            PathFigure figure = new()
            {
                StartPoint = e,
                IsClosed = true,
            };

            ArcSegment arcBottom = new()
            {
                Point = new Windows.Foundation.Point(e.X - (2.0 * r), e.Y),
                Size = new Windows.Foundation.Size(r, r),
                IsLargeArc = true,
                SweepDirection = SweepDirection.Clockwise
            };
            figure.Segments.Add(arcBottom);
            ArcSegment arcTop = new()
            {
                Point = e,
                Size = new Windows.Foundation.Size(r, r),
                IsLargeArc = true,
                SweepDirection = SweepDirection.Clockwise
            };
            figure.Segments.Add(arcTop);
            return figure;
        }

        /// <summary>
        /// return a path for a circle of radius r and center c intersected with a rectangle. the points list are
        /// cw ordered intersection points between the circle and rectangle.
        /// </summary>
        private static PathFigure ClippedCirclePath(Point c, double r, List<Point> points)
        {
            PathFigure figure = new()
            {
                StartPoint = points[0],
                IsClosed = true,
            };

            for (int i = 0; i < (points.Count - 1); i++)
            {
                Point cur = points[i];
                Point nxt = points[(i + 1) % points.Count];         // wrap around

                // Check if both points are on the circle's edge (within tolerance)
                bool isCurOnCircle = (Math.Abs(Distance(cur, c) - r) < EPSILON);
                bool isNxtOnCircle = (Math.Abs(Distance(nxt, c) - r) < EPSILON);

                if (isCurOnCircle && isNxtOnCircle)
                    figure.Segments.Add(new ArcSegment()
                    {
                        Point = nxt,
                        Size = new Windows.Foundation.Size(r, r),
                        IsLargeArc = false,
                        SweepDirection = SweepDirection.Clockwise
                    });
                else
                    figure.Segments.Add(new LineSegment() { Point = nxt });
            }
            return figure;
        }

        /// <summary>
        /// returns distance between two points.
        /// </summary>
        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// return intersection points for a rectangle and circle. rectangle origin is at (0, 0) and is slightly
        /// expanded.
        /// </summary>
        private static List<Point> FindIntersections(double w, double h, Point c, double r)
        {
            // Rectangle bounds
            double xmin = -CLIP_OFFSET, xmax = w + CLIP_OFFSET;
            double ymin = -CLIP_OFFSET, ymax = h + CLIP_OFFSET;

            List<Point> points = [];

            // 1. Add rectangle corners inside circle
            AddIfInsideCircle(new Point(xmin, ymin), c, r, points);
            AddIfInsideCircle(new Point(xmax, ymin), c, r, points);
            AddIfInsideCircle(new Point(xmax, ymax), c, r, points);
            AddIfInsideCircle(new Point(xmin, ymax), c, r, points);

            // 2. Check each rectangle edge for intersection with circle
            AddLineCircleIntersections(xmin, ymin, xmax, ymin, c, r, points); // Bottom
            AddLineCircleIntersections(xmin, ymax, xmax, ymax, c, r, points); // Top
            AddLineCircleIntersections(xmin, ymin, xmin, ymax, c, r, points); // Left
            AddLineCircleIntersections(xmax, ymin, xmax, ymax, c, r, points); // Right

            // 3. Remove duplicates (within tolerance) and order points into polygon
            return OrderPoints(RemoveDuplicates(points, EPSILON));
        }

        /// <summary>
        /// adds a point to a list of points if it falls within a circle.
        /// </summary>
        private static void AddIfInsideCircle(Point p, Point c, double r, List<Point> list)
        {
            if (((p.X - c.X) * (p.X - c.X)) + ((p.Y - c.Y) * (p.Y - c.Y)) <= (r * r))
                list.Add(p);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static void AddLineCircleIntersections(double x1, double y1, double x2, double y2,
                                                       Point c, double r, List<Point> list)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;

            double fx = x1 - c.X;
            double fy = y1 - c.Y;

            double qa = (dx * dx) + (dy * dy);
            double qb = 2 * ((fx * dx) + (fy * dy));
            double qc = (fx * fx) + (fy * fy) - r * r;

            double discriminant = (qb * qb) - (4 * qa * qc);
            if (discriminant >= 0.0)
            {
                discriminant = Math.Sqrt(discriminant);

                double t1 = (-qb - discriminant) / (2.0 * qa);
                double t2 = (-qb + discriminant) / (2.0 * qa);

                if ((t1 >= 0.0) && (t1 <= 1.0))
                    list.Add(new Point(x1 + (t1 * dx), y1 + (t1 * dy)));
                if ((t2 >= 0.0) && (t2 <= 1.0) && (discriminant > 0.0))
                    list.Add(new Point(x1 + (t2 * dx), y1 + (t2 * dy)));
            }
        }

        /// <summary>
        /// remove duplicates from a list of points. to be considered a duplicate, a point must be within a
        /// tolerance of another point in x and y.
        /// </summary>
        private static List<Point> RemoveDuplicates(List<Point> points, double tolerance)
        {
            List<Point> unique = [ ];
            foreach (var p in points)
            {
                bool exists = false;
                foreach (var q in unique)
                    if ((Math.Abs(p.X - q.X) < tolerance) && (Math.Abs(p.Y - q.Y) < tolerance))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    unique.Add(p);
            }
            return unique;
        }

        /// <summary>
        /// order a list of points by polar angle around their centroid, points are ordered in cw order.
        /// </summary>
        private static List<Point> OrderPoints(List<Point> points)
        {
            if (points.Count > 1)
            {
                // 1. Find centroid
                double cx = 0, cy = 0;
                foreach (Point p in points)
                {
                    cx += p.X;
                    cy += p.Y;
                }
                cx /= points.Count;
                cy /= points.Count;

                // 2. Sort by polar angle around centroid
                points.Sort((a, b) =>
                {
                    double angleA = Math.Atan2(a.Y - cy, a.X - cx);
                    double angleB = Math.Atan2(b.Y - cy, b.X - cx);
                    return angleA.CompareTo(angleB);
                });
            }
            return points;
        }
    }
}
