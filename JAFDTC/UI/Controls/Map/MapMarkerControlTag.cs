// ********************************************************************************************************************
//
// MapMarkerControlTag.cs : map marker control tag object
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

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// tag object for a MapMarkerControl that carries marker type, string, and integer fields.
    ///
    /// navpoints        [type]      MapMarkerInfo.MarkerType.NAVPT
    ///                  [string]    path tag for the route this marker belongs to
    ///                  [integer]   number of the navpoint in the route (so, 1-based index)
    ///
    /// edit handles     [type]      MapMarkerInfo.MarkerType.NAVPT_HANDLE
    ///                  [string]    path tag for the route the edit handle is associated with
    ///                  [integer]   position in the route where a new point should be inserted (0 implies before
    ///                              first point)
    ///
    /// all others       [type]      MapMarkerInfo.MarkerType.[others]
    ///                  [string]    unique marker identifier, generally set by the source of the marker
    ///                  [integer]   -1
    ///
    /// properties are read-only
    /// </summary>
    public sealed class MapMarkerControlTag(MapMarkerInfo.MarkerType type = MapMarkerInfo.MarkerType.UNKNOWN,
                                            string tagStr = null, int tagInt = -1)
    {
        public readonly MapMarkerInfo.MarkerType Type = type;
        public readonly string Str = tagStr;
        public readonly int Int = tagInt;

        public bool IsUnknown => (Type == MapMarkerInfo.MarkerType.UNKNOWN);

        public override string ToString() => (IsUnknown) ? "<unknown>" : $"<{Type}:{Str}:{Int}>";
    }
}
