// ********************************************************************************************************************
//
// MapImportSpec.cs -- map element import specification
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
using System.Text.Json.Serialization;

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures parameters for the importing threats/elements markers for inclusion in a map control.
    /// </summary>
    public sealed class MapImportSpec
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public string Path { get; set; }
        public CoalitionType FriendlyCoalition { get; set; }
        public bool IsEnemyOnly { get; set; }
        public bool IsSummaryOnly { get; set; }
        public bool IsAliveOnly { get; set; }

        // ---- constructed properties

        [JsonIgnore]
        public bool IsDefault => ((FriendlyCoalition == CoalitionType.BLUE) && IsEnemyOnly && !IsSummaryOnly && IsAliveOnly);

        [JsonIgnore]
        public static MapImportSpec Default => new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapImportSpec()
            => (Path, FriendlyCoalition, IsEnemyOnly, IsSummaryOnly, IsAliveOnly)
                = ("", CoalitionType.BLUE, true, false, true);

        public MapImportSpec(string path, CoalitionType friendlyCoalition, bool isEnemyOnly, bool isSummaryOnly,
                             bool isAliveOnly)
            => (Path, FriendlyCoalition, IsEnemyOnly, IsSummaryOnly, IsAliveOnly)
                = (path, friendlyCoalition, isEnemyOnly, isSummaryOnly, isAliveOnly);
    }
}
