// ********************************************************************************************************************
//
// ValidationExtension.cs -- <one_line_descripti8on>
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
namespace JAFDTC.Core.Extensions
{
    public static class ValidationExtension
    {
        public static void Required(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Parameter is required");
        }

        public static void Required(this object obj)
        {
            if (obj == null)
                throw new ArgumentException("Parameter is required");
        }
    }
}
