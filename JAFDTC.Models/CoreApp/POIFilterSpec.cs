// ********************************************************************************************************************
//
// POIFilterSpec.cs -- points of interest filter specification
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

using JAFDTC.Models.POI;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures parameters for the filters that can be applied to points of interest.
    /// </summary>
    public sealed class POIFilterSpec
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public string Theater { get; set; }
        public List<string> Campaigns { get; set; }
        public string Tags { get; set; }
        public PointOfInterestTypeMask IncludeTypes { get; set; }

        // ---- constructed properties

        [JsonIgnore]
        public bool IsDefault => (string.IsNullOrEmpty(Theater) &&
                                  ((Campaigns == null) || (Campaigns.Count == 0)) &&
                                  string.IsNullOrEmpty(Tags) &&
                                  (IncludeTypes == PointOfInterestTypeMask.ANY));

        [JsonIgnore]
        public static POIFilterSpec Default => new();


        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public POIFilterSpec() => (Theater, Campaigns, Tags, IncludeTypes) = ("", [ ], "", PointOfInterestTypeMask.ANY);

        public POIFilterSpec(string theater, List<string> campaigns, string tags, PointOfInterestTypeMask includeTypes)
            => (Theater, Campaigns, Tags, IncludeTypes) = (theater, campaigns, tags, includeTypes);

        public POIFilterSpec(POIFilterSpec src)
            => (Theater, Campaigns, Tags, IncludeTypes) = (src.Theater, src.Campaigns, src.Tags, src.IncludeTypes);
    }
}
