// ********************************************************************************************************************
//
// IMapControlHelper.cs : interfaces for a map window control helpers
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

using Microsoft.UI.Xaml.Controls.Primitives;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// defines the interface for an object that can provide a detail/explanatory popup for markers on mouse over.
    /// </summary>
    public interface IMapControlMarkerPopupFactory
    {
        /// <summary>
        /// returns a popup element to be used to display information during mouse overs on the marker with the
        /// specified information.
        /// </summary>
        public Popup MarkerPopup(MapMarkerInfo info);
    }

    /// <summary>
    /// defines the interface for an object that can provide details on markers.
    /// </summary>
    public interface IMapControlMarkerExplainer
    {
        /// <summary>
        /// returns the display type of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayType(MapMarkerInfo info);

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info);

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "");
    }
}
