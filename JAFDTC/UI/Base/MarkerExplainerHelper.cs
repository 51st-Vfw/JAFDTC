// ********************************************************************************************************************
//
// MarkerExplainerHelper.cs : helper to provide basic marker descriptions
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.Controls.Map;
using System;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper methods for the baseline behavior for marker explainers. this tracks IMapControlMarkerExplainer.
    /// </summary>
    public partial class MarkerExplainerHelper
    {
        /// <summary>
        /// returns the display type of the marker with the specified information. this only handles poi marker
        /// types, reuturning null for other types
        /// </summary>
        public static string MarkerDisplayType(MapMarkerInfo info)
            => info.Type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM => $"Core POI",
                MapMarkerInfo.MarkerType.POI_USER => $"User POI",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => $"Campaign POI",
                _ => null
            };

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayName(MapMarkerInfo info)
        {
            string name = null;
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    name = poi.Type switch
                    {
                        PointOfInterestType.SYSTEM => $"POI: {poi.Name}",
                        PointOfInterestType.USER => $"User: {poi.Name}",
                        PointOfInterestType.CAMPAIGN => $"{poi.Campaign}: {poi.Name}",
                        _ => throw new NotImplementedException(),
                    };
            }
            return name;
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            string elev = null;
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    elev = $"{poi.Elevation}{units}";
            }
            return elev;
        }
    }
}
