// ********************************************************************************************************************
//
// ISystem.cs -- interface for airframe system configuration class
//
// Copyright(C) 2023-2026 ilominar/raven
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

using JAFDTC.Models.Planning;
using System.Text.Json.Nodes;

namespace JAFDTC.Models
{
    /// <summary>
    /// interface for classes that hold configuration information for a particular avionics system in the jet.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// returns true if the system is in a default setup (i.e., state is unchanged from avionics defaults,
        /// false otherwise.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// sanatize a system prior to exporting. this clears any information that might refer to local host
        /// resources such as paths.
        /// </summary>
        public void Sanitize();

        /// <summary>
        /// returns the json node built by merging data from the system configuration into a dcs dtc configuration.
        /// the dcs dtc configuration is presented as a JsonObject for the root of the "data" object in the dtc
        /// file that encodes configuration data. this method will update the JsonObject and/or its children as
        /// necessary to complete the merge and may update the input in-place.
        /// 
        /// systems that do not support dcs dtc will return dataRoot unchanged.
        /// </summary>
        public JsonNode MergeIntoSimDTC(JsonNode dataRoot);

        /// <summary>
        /// returns the mission created by merging data from the system configuration into a mission plan. this
        /// method may update the input in-place.
        /// 
        /// systems that do not support merging into a mission plan will return mission unchanged.
        /// </summary>
        public Mission MergeIntoMission(Mission mission, int indexPackage = 0, int indexFlight = 0);

        /// <summary>
        /// reset the setup of the system to avionics defaults.
        /// </summary>
        public void Reset();
    }
}
