// ********************************************************************************************************************
//
// SettingsData.cs : jafdtc application settings
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.Models.CoreApp;
using JAFDTC.Models.POI;
using System.Collections.Generic;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// class underlying jafdtc settings data. an instance of this class is serialized/deserialized to storage to
    /// persist the settings.
    /// </summary>
    public sealed class SettingsData
    {
        /// <summary>
        /// types of upload feedback.
        /// </summary>
        public enum UploadFeedbackTypes
        {
            AUDIO = 0,
            AUDIO_DONE = 1,
            AUDIO_PROGRESS = 2,
            AUDIO_LIGHTS = 3,
            LIGHTS = 4,
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VersionJAFDTC { get; set; }

        public string SkipJAFDTCVersion { get; set; }

        public bool IsSkipDCSLuaInstall { get; set; }

        public Dictionary<string, DCSLuaManager.DCSLuaVersion> VersionDCSLua { get; }

        public int LastAirframeSelection { get; set; }

        public string LastConfigFilenameSelection { get; set; }

        public ThreatFilterSpec LastThreatFilter { get; set; }

        public string LastStptFilterTheater { get; set; }

        public string LastStptFilterCampaign { get; set; }

        public string LastStptFilterTags { get; set; }

        public PointOfInterestTypeMask LastStptFilterIncludeTypes { get; set; }

        public string LastPoIFilterTheater { get; set; }

        public string LastPoIFilterCampaign { get; set; }

        public string LastPoIFilterTags { get; set; }

        public PointOfInterestTypeMask LastPoIFilterIncludeTypes { get; set; }

        public LLFormat LastPoICoordFmtSelection { get; set; }

        public string LastWindowSetupMain { get; set; }

        public string LastWindowSetupMap { get; set; }

        public MapSettingsData MapSettings { get; set; }

        public string WingName { get; set; }

        public string Callsign { get; set; }

        public UploadFeedbackTypes UploadFeedback { get; set; }

        public bool IsNavPtImportIgnoreAirframe { get; set; }

        public bool IsAlwaysOnTop { get; set; }

        public bool IsNewVersCheckDisabled { get; set; }

        public int TCPPortCmdTx { get; }

        public int TCPPortCfgNameTx { get; }

        public int UDPPortTelRx { get; }

        public int UDPPortCapRx { get; }

        public Dictionary<AirframeTypes, int> CommandDelaysMs { get; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SettingsData()
        {
            VersionJAFDTC = "";
            SkipJAFDTCVersion = "";

            IsSkipDCSLuaInstall = false;
            VersionDCSLua = [];

            LastWindowSetupMain = "";
            LastWindowSetupMap = "";

            LastAirframeSelection = 0;
            LastConfigFilenameSelection = "";

            LastThreatFilter = new();
            LastStptFilterTheater = "";
            LastStptFilterCampaign = "";
            LastStptFilterTags = "";
            LastStptFilterIncludeTypes = PointOfInterestTypeMask.ANY;
            LastPoIFilterTheater = "";
            LastPoIFilterCampaign = "";
            LastPoIFilterTags = "";
            LastPoIFilterIncludeTypes = PointOfInterestTypeMask.ANY;
            LastPoICoordFmtSelection = LLFormat.DDM_P3ZF;

            // main application settings

            WingName = "";
            Callsign = "";
            UploadFeedback = UploadFeedbackTypes.AUDIO;
            IsNavPtImportIgnoreAirframe = false;
            IsAlwaysOnTop = false;
            IsNewVersCheckDisabled = false;

            // map window settings

            MapSettings = new();

            // NOTE: the tx/rx ports need to be kept in sync with corresponding port numbers in Lua and cannot be
            // NOTE: changed without corresponding changes to the Lua files. they are readonly here.
            //
            TCPPortCmdTx = 42001;               // clickable cockpit commands to dcs
            UDPPortTelRx = 42002;               // telemetry from dcs
            UDPPortCapRx = 42003;               // waypoint capture from dcs
            TCPPortCfgNameTx = 42004;           // configuration name to dcs

            CommandDelaysMs = new Dictionary<AirframeTypes, int>()
            {
                [AirframeTypes.A10C] = 200,
                [AirframeTypes.AH64D] = 200,
                [AirframeTypes.AV8B] = 200,
                [AirframeTypes.F14AB] = 200,
                [AirframeTypes.F15E] = 200,
                [AirframeTypes.F16C] = 200,
                [AirframeTypes.FA18C] = 200,
                [AirframeTypes.M2000C] = 200,
            };
        }
    }
}
