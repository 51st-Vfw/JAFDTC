// ********************************************************************************************************************
//
// PilotFilterSpec.cs -- pilot filter specification
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
using System.Text.Json.Serialization;

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures parameters for the filters that can be applied to points of interest.
    /// </summary>
    public sealed class PilotFilterSpec
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public AirframeTypes Airframe { get; set; }

        // ---- constructed properties

        [JsonIgnore]
        public bool IsDefault => (Airframe == AirframeTypes.UNKNOWN);

        [JsonIgnore]
        public static PilotFilterSpec Default => new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PilotFilterSpec() => (Airframe) = (AirframeTypes.UNKNOWN);

        public PilotFilterSpec(AirframeTypes airframe) => (Airframe) = (airframe);

        public PilotFilterSpec(PilotFilterSpec src) => (Airframe) = (src.Airframe);
    }
}
