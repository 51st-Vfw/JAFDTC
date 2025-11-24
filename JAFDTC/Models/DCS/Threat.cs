// ********************************************************************************************************************
//
// threat.cs -- threat model
//
// Copyright(C) 2025 ilominar/raven
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

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// types for threats.
    /// </summary>
    public enum ThreatType
    {
        UNKNOWN = -1,
        DCS_CORE = 0,
        USER = 1
    }

    /// <summary>
    /// coalitions for threats.
    /// </summary>
    public enum ThreatCoalition
    {
        UNKNOWN = -1,
        RED = 0,
        BLUE = 1
    }

    /// <summary>
    /// defines the properties of a potential threat. these instances are managed by threat database (ThreatDbase).
    /// threats include a type, dcs type, coalition, name, and wez radius.
    /// </summary>
    public sealed class Threat
    {
        public ThreatType Type { get; set; }                    // general threat type (ThreatType)

        public string TypeDCS { get; set; }                     // dcs .miz unit "type" value for threat

        public ThreatCoalition Coalition { get; set; }          // unit coalition (ThreatCoalition)

        public string Name { get; set; }                        // display name for threat
        
        public double RadiusWEZ { get; set; }                   // radius of wez (nm, 0 => "point" threat)
    }
}
