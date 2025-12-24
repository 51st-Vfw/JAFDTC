// ********************************************************************************************************************
//
// FilterExtension.cs -- filter extensions jafdtc threat objects
//
// Copyright(C) 2025 rage
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

namespace JAFDTC.Models.Threats.Extensions
{
    /// <summary>
    /// filter extensions for processing lists of Threat instances.
    /// </summary>
    public static class FilterExtension
    {
        public static IEnumerable<Threat> LimitThreatTypes(this IEnumerable<Threat> values, ThreatType[]? threatTypes)
            => (threatTypes == null || threatTypes.Length == 0) ? values : values.Where(u => threatTypes.Contains(u.Type));

        public static IEnumerable<Threat> LimitDCSTypes(this IEnumerable<Threat> values, string[]? dcsTypes)
            => (dcsTypes == null || dcsTypes.Length == 0) ? values : values.Where(u => dcsTypes.Contains(u.UnitTypeDCS));

        public static IEnumerable<Threat> LimitCoalitions(this IEnumerable<Threat> values, CoalitionType[]? coalitions)
            => (coalitions == null || coalitions.Length == 0) ? values : values.Where(u => coalitions.Contains(u.Coalition));

        public static IEnumerable<Threat> LimitCategories(this IEnumerable<Threat> values, UnitCategoryType[]? categories)
            => (categories == null || categories.Length == 0) ? values : values.Where(u => categories.Contains(u.Category));

        public static IEnumerable<Threat> LimitNames(this IEnumerable<Threat> values, string? name)
            => (string.IsNullOrEmpty(name))
                    ? values
                    : values.Where(u => u.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase));
    }
}
