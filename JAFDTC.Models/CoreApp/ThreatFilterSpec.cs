// ********************************************************************************************************************
//
// ThreatFilterSpec.cs -- threat filter specification
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

using JAFDTC.Models.Core;
using JAFDTC.Models.Units;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures parameters for the filters that can be applied to elements in the threat editors. this allows various
    /// combinations of threats to be shown/hidden by the user interface.
    /// </summary>
    public class ThreatFilterSpec
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public CoalitionType Coalition { get; set; }
        public UnitCategoryType Category { get; set; }
        public bool ShowThreatsDCS { get; set; }
        public bool ShowThreatsUser { get; set; }

        // ---- constructed properties

        [JsonIgnore]
        public bool IsDefault => ((Coalition == CoalitionType.UNKNOWN) &&
                                  (Category == UnitCategoryType.UNKNOWN) &&
                                  ShowThreatsDCS &&
                                  ShowThreatsUser);

        [JsonIgnore]
        public static ThreatFilterSpec Default => new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ThreatFilterSpec()
            => (Coalition, Category, ShowThreatsDCS, ShowThreatsUser)
                = (CoalitionType.UNKNOWN, UnitCategoryType.UNKNOWN, true, true);

        public ThreatFilterSpec(CoalitionType coalition, UnitCategoryType category, bool showThreatsDCS,
                                bool showThreatsUser)
            => (Coalition, Category, ShowThreatsDCS, ShowThreatsUser)
                = (coalition, category, showThreatsDCS, showThreatsUser);
    }
}
