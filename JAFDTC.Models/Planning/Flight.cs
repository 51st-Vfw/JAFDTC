// ********************************************************************************************************************
//
// flight.cs -- planning model flight information
//
// Copyright(C) 2026 rage, ilominar/raven
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

namespace JAFDTC.Models.Planning
{
    public class Flight
    {
        public required string Name { get; set; }        
        public required string Aircraft { get; set; }

        public required IReadOnlyList<Pilot> Pilots { get; set; }
        public IReadOnlyList<Radio>? Radios { get; set; }
        public IReadOnlyList<Route>? Routes { get; set; }
    }
}
