// ********************************************************************************************************************
//
// Keys.cs -- substution keys for kneeboard builder
//
// Copyright(C) 2026 rage
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

namespace JAFDTC.Kneeboard.Models
{
    internal static class Keys
    {
        // NOTE: this key appears in the id of a rect element from a template .svg file. the opacity of this
        // NOTE: element is forced to 0.0 if the kneeboard is not being generated in night mode.

        public const string NIGHT_TINT = "NIGHT_TINT";

        // NOTE: key values are assumed to be strings unless otherwise noted. these keys are found in a textSpan
        // NOTE: element from a template .svg file and replaced according to the mission setup.

        // ---- global keys

        // TODO: header and name are kinda redundant. remove one?
        public const string NAME = "NAME";
        public const string HEADER = "HEADER";
        public const string FOOTER = "FOOTER";
        public const string THEATER = "THEATER";

        // ---- package keys

        // TODO: currently support only single package per mission
        public const string PACKAGE_NAME = "PK*.0:N";

        // ---- flight keys

        // TODO: currently support only single flight per package
        public const string FLIGHT_NAME = "F*.0:N";                             // "VENOM1"
        public const string FLIGHT_NAME_SHORT = "F*.0:NS";                      // "VM1"
        public const string FLIGHT_AIRCRAFT = "F*.0:A";                         // "F-16C"
        public const string FLIGHT_TASKING = "F*.0:T";

        // TODO: since we only are supporting 1 flight all pilots, nav points, and comms tied to first flight...

        // NOTE: generally assume kneeboards are going to be ~flight-based so there is no need for package/flight
        // NOTE: prefixes here. radios and navigation fits this model. for pilots, we can use callsigns to denote
        // NOTE: different packages/flights in event you want a kneeboard with, for example, all viper pilots in
        // NOTE: package.

        // ---- pilot keys

        public const string PILOT_NAME = "P*.0:N";
        public const string PILOT_CALLSIGN = "P*.0:C";                          // "VENOM1-1"
        public const string PILOT_CALLSIGN_SHORT = "P*.0:CS";                   // "VM11"
        public const string PILOT_DATAID = "P*.0:DI";                           // string identifier (eg, viper tndl)
        public const string PILOT_SCL = "P*.0:SC";                              // string stores configuration
        public const string PILOT_BOARD = "P*.0:BN";                            // "23-1234"

        // ---- communications keys

        public const string RADIO_NUM = "R*.0:I";                               // integer on [1, N]
        public const string RADIO_NAME = "R*.0:N";                              // "AN/ARC-210"
        public const string RADIO_PREFIX = "R*.";
        // RADIO_PREFIX + this
        public const string RADIO_PRESET_NUM = "*:I";                           // integer on [1, N]
        public const string RADIO_PRESET_FREQ = "*:F";
        public const string RADIO_PRESET_DESC = "*:D";
        public const string RADIO_PRESET_MOD = "*:M";                           // "AM"

        // ---- navigation keys

        public const string ROUTE_NUM = "N*.0:I";                               // integer on [1, N]
        public const string ROUTE_NAME = "N*.0:D";
        public const string ROUTE_PREFIX = "N*.";
        // ROUTE_PREFIX + this
        public const string NAV_NUM = "*:I";                                    // integer on [1, N]
        public const string NAV_NAME = "*:N";
        public const string NAV_NOTE = "*:NT";
        public const string NAV_ALT = "*:A";                                    // integer
        public const string NAV_TOS = "*:TS";                                   // "00:00:00"
        public const string NAV_TOT = "*:TT";                                   // "00:00:00"
        public const string NAV_SPEED = "*:SP";
        public const string NAV_COORD = "*:CD";                                 // lat/lon in avionics format
        public const string NAV_MGRS = "*:MG";                                  // mgrs in avionics format

        // TODO: WIP

        public const string THREAT_NAME = "THREAT_*_NAME";
        public const string THREAT_TYPE = "THREAT_*_TYPE";
        public const string THREAT_COORD = "THREAT_*_COORD";

        public const string OWNSHIP_NAME = "OWNSHIP_NAME";
        public const string OWNSHIP_STN = "OWNSHIP_STN";
        public const string OWNSHIP_BOARD = "OWNSHIP_BOARD";
        public const string OWNSHIP_TACAN = "OWNSHIP_TACAN";
        public const string OWNSHIP_TACANBAND = "OWNSHIP_TACANBAND";
        public const string OWNSHIP_JOKER = "OWNSHIP_JOKER";
        public const string OWNSHIP_LASE = "OWNSHIP_LASE";
        public const string OWNSHIP_COMM = "OWNSHIP_COMM";

    }
}
