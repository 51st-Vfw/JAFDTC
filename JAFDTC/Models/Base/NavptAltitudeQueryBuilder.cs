// ********************************************************************************************************************
//
// NavptAltitudeQueryBuilder.cs -- navpoint altitude query builder
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
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// dcs query builder for a query on the elevation at a particular lat/lon. QueryNavpointAltitudes() method
    /// generates a stream of queries to gather elevation information for every navpoint with an empty Alt. this
    /// class produces a state dictionary with,
    ///
    ///     NavPtElev.conversions: int
    ///         number of navpoint conversions in the state dict
    ///     NavPtElev {lat} {lon}: int
    ///         elevation (in feet) at lat/lon {lat}/{lon}
    /// 
    /// entries for navpoint with an empty Alt.
    /// </summary>
    public class NavptAltitudeQueryBuilder(IAirframeDeviceManager dm, StringBuilder sb)
                 : QueryBuilderBase(dm, sb), IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private const double M_TO_FT = 3.2808399;

        // ------------------------------------------------------------------------------------------------------------
        //
        // query
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// gather elevation information for each navpoint in the list with an empty value for Alt. returns a state
        /// dictionary with,
        ///
        ///     NavPtElev.conversions: int
        ///         number of navpoint conversions in the state dict
        ///     NavPtElev {lat} {lon}: int
        ///         elevation (in feet) at lat/lon {lat}/{lon}
        ///
        /// where "lat" and "lon" is a lat/lon for a navpoint with an empty Alt. when isUpdatePushed is true, the
        /// empty Alt fields of the INavpointInfo instacnes in navpts list are updated with the altitude the query
        /// returns.
        /// </summary>
        public Dictionary<string, object> QueryNavpointAltitudes(List<INavpointInfo> navpts,
                                                                 bool isUpdatePushed = false,
                                                                 Dictionary<string, object> state = null)
        {
            int numCoversions = 0;
            state ??= [ ];
            foreach (INavpointInfo navpt in navpts)
            {
                string key = $"NavptElev {navpt.Lat} {navpt.Lon}";
                if (!state.ContainsKey(key) && string.IsNullOrEmpty(navpt.Alt))
                {
                    ClearCommands();
                    AddQuery("QueryAltAtLatLon", [ navpt.Lat, navpt.Lon ]);
                    if (double.TryParse(Query(), out var altitude))
                    {
                        int alt = Convert.ToInt32(altitude * M_TO_FT);
                        if (isUpdatePushed)
                            navpt.Alt = $"{alt}";
                        state[key] = alt;
                        numCoversions++;
                    }
                }
            }
            state["NavPtElev.conversions"] = numCoversions;
            return state;
        }
    }
}
