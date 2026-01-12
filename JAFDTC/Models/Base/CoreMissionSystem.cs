// ********************************************************************************************************************
//
// CoreMissionSystem.cs : core mission system
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

using JAFDTC.Core.Expressions;
using JAFDTC.Models.DCS;
using JAFDTC.Models.Planning;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// class to capture the settings of the mission "system".
    /// 
    /// this system is airframe-agnostic and should not be subclassed.
    /// </summary>
    public partial class CoreMissionSystem : SystemBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        public const string SystemTag = "JAFDTC:Generic:Mission";

        public const int NUM_SHIPS_IN_FLIGHT = 4;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events.

        private string _callsign;                               // "VENOM 1", "VENOM1", "Venom1", etc.
        public string Callsign
        {
            get => _callsign;
            set
            {
                string error = null;
                if (!string.IsNullOrEmpty(value))
                {
                    value = value.Trim();
                    MatchCollection matches = CommonExpressions.CallsignRegex().Matches(value);
                    if ((matches.Count != 1) ||
                        (matches[0].Groups.Count != 4) ||
                        !matches[0].Groups[0].Value.Equals(value) ||
                        string.IsNullOrEmpty(matches[0].Groups[1].Value) ||
                        string.IsNullOrEmpty(matches[0].Groups[2].Value) ||
                        !string.IsNullOrEmpty(matches[0].Groups[3].Value))
                    {
                        error = "Invalid Format";
                    }
                }
                SetProperty(ref _callsign, value, error);
            }
        }

        private int _ships;                                     // ship count (1 => 1-ship, 4 => 4-ship)
        public int Ships
        {
            get => _ships;
            set => SetProperty(ref _ships, value);
        }

        private string _tasking;                                // tasking information
        public string Tasking
        {
            get => _tasking;
            set => SetProperty(ref _tasking, value);
        }

        // ---- following properties do not post change and validation events.

        public string[] PilotUIDs { get; set; }                 // Pilot instance unique identifier

        public string[] Loadouts { get; set; }                  // loadouts, null => same as "lead"

        // ---- computed properties

        /// <summary>
        /// returns true if the instance indicates a default setup, false otherwise.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                for (int i = 0; i < NUM_SHIPS_IN_FLIGHT; i++)
                    if (!string.IsNullOrEmpty(PilotUIDs[i]) || !string.IsNullOrEmpty(Loadouts[i]))
                        return false;
                return (string.IsNullOrEmpty(Callsign) &&
                        string.IsNullOrEmpty(Tasking) &&
                        (Ships == NUM_SHIPS_IN_FLIGHT));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CoreMissionSystem()
        {
            Callsign = "";
            Ships = NUM_SHIPS_IN_FLIGHT;
            Tasking = "";
            PilotUIDs = new string[NUM_SHIPS_IN_FLIGHT];
            Loadouts = new string[NUM_SHIPS_IN_FLIGHT];
        }

        public CoreMissionSystem(CoreMissionSystem other)
        {
            Callsign = new(other.Callsign);
            Ships = other.Ships;
            Tasking = new(other.Tasking);
            PilotUIDs = new string[NUM_SHIPS_IN_FLIGHT];
            Loadouts = new string[NUM_SHIPS_IN_FLIGHT];
            for (int i = 0; i < NUM_SHIPS_IN_FLIGHT; i++)
            {
                PilotUIDs[i] = (!string.IsNullOrEmpty(other.PilotUIDs[i])) ? new(other.PilotUIDs[i]) : null;
                Loadouts[i] = (!string.IsNullOrEmpty(other.Loadouts[i])) ? new(other.Loadouts[i]) : null;
            }
        }

        public virtual object Clone() => new CoreMissionSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public override void Reset()
        {
            Callsign = "";
            Ships = NUM_SHIPS_IN_FLIGHT;
            Tasking = "";
            PilotUIDs = new string[NUM_SHIPS_IN_FLIGHT];
            Loadouts = new string[NUM_SHIPS_IN_FLIGHT];
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ISystem overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the mission created by merging data from the system configuration into a mission plan. this
        /// method may update the input in-place.
        /// </summary>
        public override Mission MergeIntoMission(Mission mission, int indexPackage = 0, int indexFlight = 0)
        {
            List<Pilot> pilots = [ ];
            for (int i = 0; i < NUM_SHIPS_IN_FLIGHT; i++)
            {
                JAFDTC.Models.Pilots.Pilot pilot = PilotDbase.Instance.Find(PilotUIDs[i]);
                if (pilot != null)
                {
                    pilots.Add(new()
                    {
                        Name = pilot.Name,
                        Position = i,
                        DataId = pilot.AvionicsID,
                        SCL = Loadouts[i],
                    });
                }
            }
            mission.Packages[indexPackage].Flights[indexFlight].Tasking = Tasking;
            mission.Packages[indexPackage].Flights[indexFlight].Pilots = pilots;
            return mission;
        }
    }
}
