// ********************************************************************************************************************
//
// StringExtension.cs -- <one_line_descripti8on>
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
using System.Globalization;

namespace JAFDTC.TacView.Extensions
{
    public static class StringExtension
    {
        public static CategoryType ToCategory(this string? value)
        {
            if (Enum.TryParse<CategoryType>(value.ToNormalized(), true, out var result))
                return result;

            return CategoryType.Unknown;
        }

        public static CoalitionType ToCoalition(this string? value)
        {
            if (Enum.TryParse<CoalitionType>(value.ToNormalized(), true, out var result))
                return result;

            return CoalitionType.Unknown;
        }

        public static ColorType ToColor(this string? value)
        {
            if (Enum.TryParse<ColorType>(value.ToNormalized(), true, out var result))
                return result;

            return ColorType.Unknown;
        }

        public static UnitType ToUnit(this string? value)
        {
            if (Enum.TryParse<UnitType>(value.ToNormalized(), true, out var result))
                return result;

            return UnitType.Unknown;
        }

        public static string ToNormalized(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value
               .Trim()
               .Replace(" ", "_")
               .Replace("-", "_")
               .Replace("+", "_")
               .Replace("/", "_")
               .ToUpper();

            // If starts with digit, prefix with U_
            if (char.IsDigit(normalized[0]))
                normalized = "U_" + normalized;

            return normalized;
        }

        public static string ToCleanValue(this string value)
        {
            return value[(value.IndexOf('=') + 1)..];
        }

        public static string ToCleanValue(this Dictionary<string, string> dict, string keyName)
        {
            if (dict.TryGetValue(keyName, out var v))
                return v;

            return string.Empty;
        }

        public static double ToCleaDouble(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        }

    }
}
