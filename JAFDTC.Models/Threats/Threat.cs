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

using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.Models.Threats
{
    /// <summary>
    /// defines the properties of a potential threat. these instances are managed by threat database (ThreatDbase).
    /// threats include a type, dcs type, coalition, name, and wez radius. the ( Type, Coalition, TypeDCS ) tuple
    /// always uniquely identifies a threat.
    /// </summary>
    public sealed class Threat
    {
        // ---- properties

        public ThreatType Type { get; set; }                    // general threat type (ThreatType)

        public UnitCategoryType Category { get; set; }          // unit category

        public CoalitionType Coalition { get; set; }            // primary unit coalition

        public string UnitTypeDCS { get; set; }                 // dcs .miz unit "type" value for threat

        public string Name { get; set; }                        // display name for threat
        
        public double RadiusWEZ { get; set; }                   // radius of wez (nm, 0 => "point" threat)

        // ---- computed properties

        public string UniqueID => $"{Type}:{Coalition}:{UnitTypeDCS.Replace(' ', '_').Replace('-', '_')}";
    }
}
