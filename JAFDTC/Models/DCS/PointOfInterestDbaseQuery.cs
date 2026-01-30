// ********************************************************************************************************************
//
// PointOfInterestDbaseQuery.cs -- point of interest "database" query model
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2026 ilominar/raven
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

using JAFDTC.Models.POI;
using System;
using System.Collections.Generic;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// flags to control paramters of a query in the point of interest database via Find().
    /// </summary>
    [Flags]
    public enum PointOfInterestDbaseQueryFlags
    {
        NONE = 0,                                               // no flags
        NAME_PARTIAL_MATCH = 1 << 0,                            // allow partial match of name
        TAGS_ANY_MATCH = 1 << 1,                                // allow at least one tag match
        TAG_PARTIAL_MATCH = 1 << 2,                             // allow partial match of a tag
    }

    /// <summary>
    /// parameters for a query of the point of interest database via Find(). for a poi to match a query,
    /// the following must hold:
    /// 
    ///     1) query.Types contains poi.Type
    ///     2) query.Theater matches poi.Theater exactly
    ///     3) query.Campaigns has an element that matches poi.Campaign exactly
    ///     4) query.Name matches poi.Name per query.Flags, given poi.Name "abcdef"
    ///             NAME_PARTIAL_MATCH => Name "bcd" matches
    ///            !NAME_PARTIAL_MATCH => Name "bcd" does not match
    ///     4) query.Tags matches poi.Tags per query.Flags, given poi.Tags "aa ; bb"
    ///             TAGS_ANY_MATCH => to match, at least one tag in query.Tags must match a tag in poi.Tags
    ///            !TAGS_ANY_MATCH => to match, all tags in query.Tags must match a tag in poi.Tags
    ///             TAG_PARTIAL_MATCH => allows partial tag matches, "a" matches "aa"
    ///            !TAG_PARTIAL_MATCH => disallows partial tag matches, "a" does not match "aa"
    ///
    /// string comparisons are always case-insensitive.
    /// </summary>
    public class PointOfInterestDbaseQuery
    {
        public readonly PointOfInterestTypeMask Types;          // types of points of interest to search

        public readonly string Theater;                         // theater (null => match any)

        public readonly List<string> Campaigns;                 // campaign name (null, empty => match any)

        public readonly string Name;                            // name (null => match any)

        public readonly string Tags;                            // tags (";"-separated list, null => match any)

        public readonly PointOfInterestDbaseQueryFlags Flags;   // query flags

        public PointOfInterestDbaseQuery(PointOfInterestTypeMask types = PointOfInterestTypeMask.ANY,
                                         string theater = null, List<string> campaigns = null, string name = null,
                                         string tags = null,
                                         PointOfInterestDbaseQueryFlags flags = PointOfInterestDbaseQueryFlags.NONE)
            => (Types, Theater, Campaigns, Name, Tags, Flags) = (types, theater, campaigns, name, tags, flags);
    }
}
