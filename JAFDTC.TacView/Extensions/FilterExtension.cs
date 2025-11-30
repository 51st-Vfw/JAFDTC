// ********************************************************************************************************************
//
// FilterExtension.cs -- <one_line_descripti8on>
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
using JAFDTC.TacView.Models;
using System.Linq;

namespace JAFDTC.TacView.Extensions
{
    public static class FilterExtension
    {
        public static IEnumerable<UnitItem> LimitColors(this IEnumerable<UnitItem> values, ColorType[]? colors)
        {
            if (colors == null || !colors.Any())
                return values;

            return values.Where(u => colors.Contains(u.Color));
        }

        public static IEnumerable<UnitItem> LimitCoalitions(this IEnumerable<UnitItem> values, CoalitionType[]? coalitions)
        {
            if (coalitions == null || !coalitions.Any())
                return values;

            return values.Where(u => coalitions.Contains(u.Coalition));
        }

        public static IEnumerable<UnitItem> LimitCategories(this IEnumerable<UnitItem> values, CategoryType[]? categories)
        {
            if (categories == null || !categories.Any())
                return values;

            return values.Where(u => categories.Contains(u.Category));
        }

        public static IEnumerable<UnitItem> LimitAlive(this IEnumerable<UnitItem> values, bool? isAlive)
        {
            if (!isAlive.HasValue)
                return values;

            return values.Where(u => u.IsAlive == isAlive.Value);
        }
    }
}
