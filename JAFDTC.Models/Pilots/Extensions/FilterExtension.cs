// ********************************************************************************************************************
//
// FilterExtension.cs -- filter extensions jafdtc pilot objects
//
// Copyright(C) 2026 ilominar/raven
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

namespace JAFDTC.Models.Pilots.Extensions
{
    /// <summary>
    /// filter extensions for processing lists of Pilot instances.
    /// </summary>
    public static class FilterExtension
    {
        public static IEnumerable<Pilot> LimitAirframes(this IEnumerable<Pilot> values, AirframeTypes[]? types)
            => (types == null || types.Length == 0) ? values : values.Where(u => types.Contains(u.Airframe));

        public static IEnumerable<Pilot> LimitBoardNums(this IEnumerable<Pilot> values, string? boardNum)
            => (string.IsNullOrEmpty(boardNum))
                    ? values
                    : values.Where(u => u.Name.Contains(boardNum, System.StringComparison.CurrentCultureIgnoreCase));

        public static IEnumerable<Pilot> LimitNames(this IEnumerable<Pilot> values, string? name)
            => (string.IsNullOrEmpty(name))
                    ? values
                    : values.Where(u => u.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase));

        public static IEnumerable<Pilot> LimitExactNames(this IEnumerable<Pilot> values, string? name)
            => (string.IsNullOrEmpty(name))
                    ? values
                    : values.Where(u => u.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase));
    }
}
