// ********************************************************************************************************************
//
// NavpointSystemInfo.cs -- navigation point system information class
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

using JAFDTC.Utilities;
using System.Collections.Generic;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// TODO
    /// </summary>
    public sealed class NavpointSystemInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the system tag for the navpoint system.
        /// </summary>
        public string SystemTag { get; }

        /// <summary>
        /// return the tag to use on navpoint lists.
        /// </summary>
        public string NavptListTag { get; }

        /// <summary>
        /// return the airframe type for the navpoint list.
        /// </summary>
        public AirframeTypes AirframeType { get; }

        /// <summary>
        /// return the name to use to refer to a navpoint (waypoint, steerpoint, etc.) by in the user interface. the
        /// string should be singular and capitalized.
        /// </summary>
        public string NavptName { get; }

        /// <summary>
        /// return the coordinate format used by navpoints in the navigation system.
        /// </summary>
        public LLFormat NavptCoordFmt { get; }

        /// <summary>
        /// return the maximum number of characters the jet will allow for a navpoint name. if zero, the UI will not
        /// indicate a limit.
        /// </summary>
        public int NavptMaxNameLength { get; }

        /// <summary>
        /// return maximum number of navpoints that can be present in a route.
        /// </summary>
        public int NavptMaxCount { get; }

        /// <summary>
        /// return list of names of the independent routes the avionics can handle.
        /// </summary>
        public List<string> RouteNames { get; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public NavpointSystemInfo(string systemTag, string navptListTag, AirframeTypes airframeType, string navptName,
                                  LLFormat navptCoordFmt, int navptMaxNameLength, int navptMaxCount, List<string> routeNames)
            => (SystemTag, NavptListTag, AirframeType, NavptName, NavptCoordFmt, NavptMaxNameLength, NavptMaxCount, RouteNames)
               = (systemTag, navptListTag, airframeType, navptName, navptCoordFmt, navptMaxNameLength, navptMaxCount, routeNames);
    }
}
