//
// IMapControlMarkerExplainer.cs : interfaces for a map window marker control helper to provide marker descriptions
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

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// defines the interface for an object that can provide details on markers.
    /// </summary>
    public interface IMapControlMarkerExplainer
    {
        /// <summary>
        /// returns a string for the type of the marker with the specified information for use in the ui, "" if the
        /// display type cannot be determined.
        /// </summary>
        public string MarkerDisplayType(MapMarkerInfo info);

        /// <summary>
        /// returns a string for the name of the marker with the specified information for use in the ui, "" if the
        /// display name cannot be determined.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info);

        /// <summary>
        /// returns a string for the elevation of the marker with the specified information for use in the ui, ""
        /// if elevation cannot be determined.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "");
    }
}
