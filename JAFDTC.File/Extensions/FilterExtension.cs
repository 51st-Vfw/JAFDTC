// ********************************************************************************************************************
//
// FilterExtension.cs -- filter extensions jafdtc model objects
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

using JAFDTC.Core.Extensions;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.File.Extensions
{
    public static class FilterExtension
    {
        public static IEnumerable<UnitGroupItem> LimitCoalitions(this IEnumerable<UnitGroupItem> values, CoalitionType[]? coalitions)
        {
            if ((coalitions == null) || coalitions.IsEmpty())
                return values;

            return values.Where(u => coalitions.Contains(u.Coalition));
        }

        public static IEnumerable<UnitGroupItem> LimitUnitCategories(this IEnumerable<UnitGroupItem> values, UnitCategoryType[]? unitCategories)
        {
            if ((unitCategories == null) || unitCategories.IsEmpty())
                return values;

            return values.Where(u => unitCategories.Contains(u.Category));
        }

        public static IEnumerable<UnitGroupItem> LimitGroupsWithUnits(this IEnumerable<UnitGroupItem> values)
        {
            return values.Where(u => u.Units.HasData());
        }

        public static IEnumerable<UnitGroupItem> LimitUnitTypes(this IEnumerable<UnitGroupItem> values, string[]? unitTypes)
        {
            if ((unitTypes == null) || unitTypes.IsEmpty())
                return values;

            var temp = values.ToList();
            foreach (var group in temp)
                group.Units = group.Units.LimitUnitTypes(unitTypes).ToList();

            return temp.Where(u => u.Units.HasData());
        }

        public static IEnumerable<UnitItem> LimitUnitTypes(this IEnumerable<UnitItem> values, string[]? unitTypes)
        {
            if ((unitTypes == null) || unitTypes.IsEmpty())
                return values;

            return values.Where(u => unitTypes.Contains(u.Type));
        }

        public static IEnumerable<UnitGroupItem> LimitAlive(this IEnumerable<UnitGroupItem> values, bool? isAlive)
        {
            if (!isAlive.HasValue)
                return values;

            var temp = values.ToList();
            foreach (var group in temp)
                group.Units = group.Units.LimitAlive(isAlive).ToList();

            return temp.Where(u => u.Units.HasData());
        }

        public static IEnumerable<UnitItem> LimitAlive(this IEnumerable<UnitItem> values, bool? isAlive)
        {
            if (!isAlive.HasValue)
                return values;

            return values.Where(u => u.IsAlive == isAlive.Value);
        }
    }
}
