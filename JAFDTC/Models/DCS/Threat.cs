// ********************************************************************************************************************
//
// threat.cs -- threat model
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
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.Units;
using System.Collections.Generic;
using System.Linq;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// types for threats.
    /// </summary>
    public enum ThreatType
    {
        UNKNOWN = -1,
        DCS_CORE = 0,
        USER = 1
    }

    // ================================================================================================================

    /// <summary>
    /// filter extensions for processing lists of threats.
    /// </summary>
    public static class FilterExtension
    {
        public static IEnumerable<Threat> LimitThreatTypes(this IEnumerable<Threat> values, ThreatType[]? threatTypes)
            => (threatTypes == null || threatTypes.Length == 0) ? values : values.Where(u => threatTypes.Contains(u.Type));

        public static IEnumerable<Threat> LimitDCSTypes(this IEnumerable<Threat> values, string[]? dcsTypes)
            => (dcsTypes == null || dcsTypes.Length == 0) ? values : values.Where(u => dcsTypes.Contains(u.TypeDCS));

        public static IEnumerable<Threat> LimitCoalitions(this IEnumerable<Threat> values, CoalitionType[]? coalitions)
            => (coalitions == null || coalitions.Length == 0) ? values : values.Where(u => coalitions.Contains(u.Coalition));

        public static IEnumerable<Threat> LimitCategories(this IEnumerable<Threat> values, UnitCategoryType[]? categories)
            => (categories == null || categories.Length == 0) ? values : values.Where(u => categories.Contains(u.Category));

        public static IEnumerable<Threat> LimitNames(this IEnumerable<Threat> values, string? name)
            => (string.IsNullOrEmpty(name))
                    ? values
                    : values.Where(u => u.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase));
    }

    // ================================================================================================================

    /// <summary>
    /// defines the properties of a potential threat. these instances are managed by threat database (ThreatDbase).
    /// threats include a type, dcs type, coalition, name, and wez radius. the ( Type, Coalition, TypeDCS ) tuple
    /// always uniquely identifies a threat.
    /// </summary>
    public sealed class Threat
    {
        // ---- properties

        public ThreatType Type { get; set; }                    // general threat type (ThreatType)

        public string TypeDCS { get; set; }                     // dcs .miz unit "type" value for threat

        public UnitCategoryType Category { get; set; }          // unit category

        public CoalitionType Coalition { get; set; }            // primary unit coalition

        public string Name { get; set; }                        // display name for threat
        
        public double RadiusWEZ { get; set; }                   // radius of wez (nm, 0 => "point" threat)

        // ---- computed properties

        public string UniqueID => $"{Type}:{Coalition}:{TypeDCS.Replace(' ', '_').Replace('-', '_')}";
    }
}
