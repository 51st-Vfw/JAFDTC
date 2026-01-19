// ********************************************************************************************************************
//
// Threat.cs -- planning model threat information
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

using JAFDTC.Models.Core;

namespace JAFDTC.Models.Planning
{
    /// <summary>
    /// planning threat model that includes coalition, name, type, location, and wez information. by convention,
    /// setting Type to an empty string (null or "") identifies a threat region that is not associated with a
    /// specific unit (for example, the overall threat region of a sam site as a whole). locations are specified in
    /// decimal degrees.
    /// </summary>
    public class Threat
    {
        public required CoalitionType Coalition { get; set; }               // coalition of threat
        public required string Name { get; set; }                           // name of threat
        public required string? Type { get; set; }                          // dcs threat type, null => threat region
        public required Location Location { get; set; }                     // location of threat, decimal degrees
        public double? WEZ { get; set; }                                    // size of wez, centered on location (nm)
    }
}
