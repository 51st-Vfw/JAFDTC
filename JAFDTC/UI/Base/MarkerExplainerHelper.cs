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
        /// returns the default display glyph(s) for the marker with the specified information, null if the glyphs
        /// cannot be determined. returned string may return up to two glyphs (which will be overlaid), [0] is the
        /// foreground and [1] (if present) is the background.
        /// </summary>
        public static string MarkerDisplayGlyphs(MapMarkerInfo info)
            => info.Tag.Type switch
            {
                MapMarkerInfo.MarkerType.UNKNOWN => $"{Glyphs.StatusCircle}{Glyphs.StatusQuestion}",
                MapMarkerInfo.MarkerType.POI_SYSTEM => Glyphs.PoISystem,
                MapMarkerInfo.MarkerType.POI_USER => Glyphs.PoIUser,
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => Glyphs.PoICampaign,
                MapMarkerInfo.MarkerType.NAV_PT => $"{Glyphs.NumberBox}{Glyphs.Number1}",
                MapMarkerInfo.MarkerType.UNIT_FRIEND => Glyphs.Shield,
                MapMarkerInfo.MarkerType.UNIT_ENEMY => Glyphs.ShieldExclaim,
// TODO: USER_PT, BULLSEYE?
                _ => null
            };

        /// <summary>
        /// returns the display type of the marker with the specified information. this only handles poi marker
        /// types, reuturning null for other types
        /// </summary>
        public static string MarkerDisplayType(MapMarkerInfo info)
        {
            string campaign = "";
            PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.Tag.Str);
            if (poi != null)
                campaign = poi.Campaign;
            return info.Tag.Type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM => $"System POI",
                MapMarkerInfo.MarkerType.POI_USER => $"User POI",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => $"{campaign} POI",
                MapMarkerInfo.MarkerType.UNIT_ENEMY => $"REDFOR",
                MapMarkerInfo.MarkerType.UNIT_FRIEND => $"BLUEFOR",
                MapMarkerInfo.MarkerType.BULLSEYE => $"BULLS",
                _ => null
            };
        }

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayName(MapMarkerInfo info)
        {
            string name = null;
            if ((info.Tag.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Tag.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Tag.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.Tag.Str);
                if (poi != null)
                    name = poi.Name;
            }
            else if (info.Tag.Type == MapMarkerInfo.MarkerType.BULLSEYE)
            {
                name = "Bullseye";
            }
            else if ((info.Tag.Type == MapMarkerInfo.MarkerType.PATH_EDIT_HANDLE) ||
                     (info.Tag.Type == MapMarkerInfo.MarkerType.RING_EDIT_HANDLE))
            {
                name = "Edit Handle";
            }
            return name;
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            string elev = null;
            if ((info.Tag.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Tag.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Tag.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.Tag.Str);
                if (poi != null)
                    elev = $"{poi.Elevation}{units}";
            }
            return elev;
        }
    }
}
