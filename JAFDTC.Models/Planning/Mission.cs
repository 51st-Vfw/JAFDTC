// ********************************************************************************************************************
//
// Mission.cs -- planning model mission information
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
    public class Mission
    {
        public required string Name { get; set; }
        public required string Theater { get; set; }
        public required Ownship Owner { get; set; }

        public required IReadOnlyList<Package> Packages { get; set; }
        public IReadOnlyList<Threat>? Threats { get; set; }
    }
}
