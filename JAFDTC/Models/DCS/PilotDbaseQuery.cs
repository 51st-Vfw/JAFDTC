// ********************************************************************************************************************
//
// PilotDbaseQuery.cs -- pilot database query model
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

namespace JAFDTC.Models.DCS
{
    public class PilotDbaseQuery
    {
        public AirframeTypes[]? Airframes;      // airframe(s); null => any
        public string? Name;                    // name, partial match, case insensitive; null => any
        public string? ExactName;               // name, full match, case insensitive; null => any
        public string? BoardNumber;             // board num, partial match, case insensitive; null => any
    }
}
