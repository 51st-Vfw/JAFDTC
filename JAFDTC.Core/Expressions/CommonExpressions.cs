// ********************************************************************************************************************
//
// CommonExpressions.cs -- core common expressions
//
// Copyright(C) 2025-2026 rage
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

using System.Text.RegularExpressions;

namespace JAFDTC.Core.Expressions
{
    public static partial class CommonExpressions
    {
        // time callsign ("0:00:00 AM", "23:00:00 PM", etc.)
        //
        [GeneratedRegex(@"(?i)^.+\s+(\d\d|\d):(\d\d):(\d\d)\s+(am|pm)")]
        public static partial Regex TimeRegex();

        // flight callsign regex for flights and ships ("Venom 1-1", "Jedi 3", "JEDI3", etc.)
        //
        [GeneratedRegex(@"(?i)([a-z]+)[\s\-_]*([\d]+)[^\s\-_]*([\d]+){0,1}")]
        public static partial Regex CallsignRegex();
    }
}
