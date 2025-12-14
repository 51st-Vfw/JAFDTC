// ********************************************************************************************************************
//
// INavpointSystemImport.cs -- import interface for a navpoint system object
//
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.Models.CoreApp;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// interface for import-related functionality in a navpoint system.
    /// </summary>
    public interface INavpointSystemImport
    {
        /// <summary>
        /// return the identifer for the current route, null for systems that support only a single route.
        /// </summary>
        public string NavptCurrentRoute();

        /// <summary>
        /// return the number of navpoints currently defined for the given route (null => all routes).
        /// </summary>
        public int NavptCurrentCount(string route = null);

        /// <summary>
        /// return the number of navpoints available (i.e., the number that could be added without breaking system
        /// limits) for the given route (null => all routes).
        /// </summary>
        public int NavptAvailableCount(string route = null);

        /// <summary>
        /// deserialize an array of navpoints from .json and incorporate them into the navpoint list for the
        /// current route. the deserialized navpoints can either replace the existing navpoints or be appended to
        /// the end of the navpoint list. returns true on success, false on error (previous navpoints preserved on
        /// errors).
        /// 
        /// imports from serialized navpoints support all navpoint properties.
        /// </summary>
        public bool ImportSerializedNavpoints(string json, bool isReplace = true);

        /// <summary>
        /// incorporate a list of navpoints specified by unit position instances into the navpoint list for the
        /// current route. the new navpoints can either replace the existing navpoints or be appended to the end of
        /// the navpoint list. returns true on success, false on error (previous navpoints preserved on errors).
        /// </summary>
        public bool ImportUnitPositionList(IReadOnlyList<UnitPositionItem> posnList, bool isReplace = true);
    }
}
