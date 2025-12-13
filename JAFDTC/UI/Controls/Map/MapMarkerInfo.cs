// ********************************************************************************************************************
//
// MapMarkerInfo.cs : map control marker information
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

using JAFDTC.Models.DCS;
using System;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// information for a map marker (route point, poi, etc.) edited by the WorldMapControl. this includes the
    /// type, string tag, and integer tag that uniquely identify the marker along with the current lat/lon position
    /// of the marker on the map. instances are read-only.
    /// </summary>
    public sealed partial class MapMarkerInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// types for map markers. includes all PointOfInterestType types so a PointOfInterestType can be directly
        /// cast to this type.
        /// </summary>
        public enum MarkerType
        {
            UNKNOWN = PointOfInterestType.UNKNOWN,
            POI_DCS_CORE = PointOfInterestType.DCS_CORE,        // core poi
            POI_USER = PointOfInterestType.USER,                // user poi
            POI_CAMPAIGN = PointOfInterestType.CAMPAIGN,        // campaign poi

            NAV_PT = 16,                                        // point on navigation path
            USER_PT = 17,                                       // point on user annotation
            UNIT_FRIEND = 18,                                   // point for unit (friendly)
            UNIT_ENEMY = 19,                                    // point for unit (enemy)
            BULLSEYE = 20,                                      // point for bullseye
            PATH_EDIT_HANDLE = 28,                              // edit handle for path
            RING_EDIT_HANDLE = 29,                              // edit handle for ring
            ANY = 31
        }

        /// <summary>
        /// type mask for MarkerType enum. includes all PointOfInterestTypeMask values so a PointOfInterestTypeMask
        /// can be directly cast to this type.
        /// </summary>
        [Flags]
        public enum MarkerTypeMask
        {
            NONE = 0,
            ANY = -1,

            POI_DCS_CORE = 1 << MarkerType.POI_DCS_CORE,
            POI_USER = 1 << MarkerType.POI_USER,
            POI_CAMPAIGN = 1 << MarkerType.POI_CAMPAIGN,

            NAV_PT = 1 << MarkerType.NAV_PT,
            USER_PT = 1 << MarkerType.USER_PT,
            UNIT_FRIEND = 1 << MarkerType.UNIT_FRIEND,
            UNIT_ENEMY = 1 << MarkerType.UNIT_ENEMY,
            BULLSEYE = 1 << MarkerType.BULLSEYE,
            PATH_EDIT_HANDLE = 1 << MarkerType.PATH_EDIT_HANDLE,
            RING_EDIT_HANDLE = 1 << MarkerType.RING_EDIT_HANDLE
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public readonly MapMarkerInfo.MarkerType Type;
        public readonly string TagStr;
        public readonly int TagInt;
        public readonly string Lat;
        public readonly string Lon;
        public readonly double Rad;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapMarkerInfo()
            => (Type, TagStr, TagInt, Lat, Lon, Rad) = (MapMarkerInfo.MarkerType.UNKNOWN, null, -1, null, null, 0.0);

        public MapMarkerInfo(MapMarkerInfo.MarkerType type, string tagStr = null, int tagInt = -1, string lat = null,
                             string lon = null, double rad = 0.0)
            => (Type, TagStr, TagInt, Lat, Lon, Rad) = (type, tagStr, tagInt, lat, lon, rad);

        internal MapMarkerInfo(MapMarkerControl marker)
        {
            Tuple<MapMarkerInfo.MarkerType, string, int> tuple = marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int>;
            Type = tuple.Item1;
            TagStr = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? tuple.Item2 : null;
            TagInt = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? tuple.Item3 : -1;
            Lat = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? $"{marker.Location.Latitude:F8}" : null;
            Lon = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? $"{marker.Location.Longitude:F8}" : null;
            Rad = 0.0;
        }
    }
}
