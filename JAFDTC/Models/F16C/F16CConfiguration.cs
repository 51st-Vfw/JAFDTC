// ********************************************************************************************************************
//
// F16CConfiguration.cs -- f-16c airframe configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.CMDS;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.Models.Pilots;
using JAFDTC.UI.F16C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// configuration object for the viper that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation,
    /// countermeasure, datalink, harm, hts, mfd, radio, and miscellaneous systems.
    /// </summary>
    public partial class F16CConfiguration : ConfigurationBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private const string _versionCfg = "F16C-1.1";          // current version

        // v1.0 --> v1.1:
        // - interpretation of ReleaseMode, RipplePulse fields in the sms system changed.
        //
        private const string _versionCfg_10 = "F16C-1.0";

        // ---- regular expressions

        [GeneratedRegex(@"^[0-7]{5}$")]
        public static partial Regex RegexTNDL();

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public CMDSSystem CMDS { get; set; }

        public DLNKSystem DLNK { get; set; }

        public HARMSystem HARM { get; set; }

        public HTSSystem HTS { get; set; }

        public MFDSystem MFD { get; set; }

        public MiscSystem Misc { get; set; }

        public RadioSystem Radio { get; set; }

        public SMSSystem SMS { get; set; }

        public STPTSystem STPT { get; set; }

        public CoreSimDTCSystem DTE { get; set; }

        public CoreKboardSystem Kboard { get; set; }

        public CoreMissionSystem Mission { get; set; }

        [JsonIgnore]
        public override List<string> MergeableSysTags =>
        [
            CoreMissionSystem.SystemTag,
            MiscSystem.SystemTag,
            STPTSystem.SystemTag,
            RadioSystem.SystemTag,
            CMDSSystem.SystemTag
        ];

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new F16CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.F16C, uid, name, linkedSysMap)
        {
            CMDS = new CMDSSystem();
            DLNK = new DLNKSystem();
            HARM = new HARMSystem();
            HTS = new HTSSystem();
            MFD = new MFDSystem();
            Misc = new MiscSystem();
            Radio = new RadioSystem();
            SMS = new SMSSystem();
            STPT = new STPTSystem();
            DTE = new CoreSimDTCSystem();
            Kboard = new CoreKboardSystem();
            Mission = new CoreMissionSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = [ ];
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            F16CConfiguration clone = new("", Name, linkedSysMap)
            {
                CMDS = (CMDSSystem)CMDS.Clone(),
                DLNK = (DLNKSystem)DLNK.Clone(),
                HARM = (HARMSystem)HARM.Clone(),
                HTS = (HTSSystem)HTS.Clone(),
                MFD = (MFDSystem)MFD.Clone(),
                Misc = (MiscSystem)Misc.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                SMS = (SMSSystem)SMS.Clone(),
                STPT = (STPTSystem)STPT.Clone(),
                DTE = (CoreSimDTCSystem)DTE.Clone(),
                Kboard = (CoreKboardSystem)Kboard.Clone(),
                Mission = (CoreMissionSystem)Mission.Clone()
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            F16CConfiguration otherViper = other as F16CConfiguration;
            switch (systemTag)
            {
                case CMDSSystem.SystemTag: CMDS = otherViper.CMDS.Clone() as CMDSSystem; break;
                case DLNKSystem.SystemTag: DLNK = otherViper.DLNK.Clone() as DLNKSystem; break;
                case HARMSystem.SystemTag: HARM = otherViper.HARM.Clone() as HARMSystem; break;
                case HTSSystem.SystemTag: HTS = otherViper.HTS.Clone() as HTSSystem; break;
                case MFDSystem.SystemTag: MFD = otherViper.MFD.Clone() as MFDSystem; break;
                case MiscSystem.SystemTag: Misc = otherViper.Misc.Clone() as MiscSystem; break;
                case RadioSystem.SystemTag: Radio = otherViper.Radio.Clone() as RadioSystem; break;
                case SMSSystem.SystemTag: SMS = otherViper.SMS.Clone() as SMSSystem; break;
                case STPTSystem.SystemTag: STPT = otherViper.STPT.Clone() as STPTSystem; break;
                case CoreSimDTCSystem.SystemTag: DTE = otherViper.DTE.Clone() as CoreSimDTCSystem; break;
                case CoreKboardSystem.SystemTag: Kboard = otherViper.Kboard.Clone() as CoreKboardSystem; break;
                case CoreMissionSystem.SystemTag: Mission = otherViper.Mission.Clone() as CoreMissionSystem; break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // internal methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// parse the role string from import role setup. the string consists of either callsign or tacan fields.
        /// callsign fields are of the form CCNN, where C is [a-zA-Z] and N is [0-9] (eg, "VN11", "CY23"). tacan
        /// fields are of the form NM where N is [0-9]{1,3] and M is [xyXY] (eg, "1Y", "132X"). retruns a dictionary
        /// with role fields broken out, null on error.
        /// </summary>
        private static Dictionary<string, string> ParseRole(string role)
        {
            Dictionary<string, string> info = [ ];
            List<string> fields = [.. role.ToUpper().Split(' ') ];
            foreach (string field in fields)
            {
                if ((field.Length > 2) &&
                    ((field[^1] == 'X') || (field[^1] == 'Y')) &&
                    (int.TryParse(field[..^1], out int channel) && ((channel >= 1) && (channel <= 63))))
                {
                    info["TACAN_CHAN"] = $"{channel}";
                    info["TACAN_BAND"] = $"{field[^1]}";
                }
                else if ((field.Length == 4) &&
                         (int.TryParse($"{field[^2]}", out int flight) && ((flight >= 1) && (flight <= 9))) &&
                         (int.TryParse($"{field[^1]}", out int elem) && ((elem >= 1) && (elem <= 4))) &&
                         ($"{field[..1]}".All(char.IsLetter)))
                {
                    info["CALLSIGN"] = $"{field[..2]}";
                    info["FLIGHT"] = $"{flight}";
                    info["ELEM"] = $"{elem}";
                }
                else if (field.Length > 0)
                {
                    return null;
                }
            }
            return info;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // overriden class methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Sanitize(bool isResetUID = false)
        {
            base.Sanitize(isResetUID);

            CMDS.Sanitize();
            DLNK.Sanitize();
            HARM.Sanitize();
            HTS.Sanitize();
            MFD.Sanitize();
            Misc.Sanitize();
            Radio.Sanitize();
            SMS.Sanitize();
            STPT.Sanitize();
            DTE.Sanitize();
            Kboard.Sanitize();
            Mission.Sanitize();
        }

        public override bool ValidateRole(string role) => (ParseRole(role) != null);

        public override void AdjustForRole(string role)
        {
            Dictionary<string, string> roleInfo = ParseRole(role);

            PilotDbaseQuery query = new()
            {
                Airframes = [ AirframeTypes.F16C ]
            };

            // determine ownship flight/element number. check (in order): (1) value in dlnk config, (2) value
            // inferred from position of pilot matching callsign from settings in team list, (3) value from
            // role specification string, or (4) value from mission config. later over-rides earlier, so (3) is
            // selected if both (2) and (3) exist but not (4). with that information update dlnk configuration.
            //
            // note that if (4) hits, there is no need to update ownship information as the mission and dlnk
            // configs should already be consistent.
            //
            string myUid = null;
            foreach (Pilot driver in PilotDbase.Instance.Find(query))
                if (driver.Name == Settings.Callsign)
                {
                    myUid = driver.UniqueID;
                    foreach (string uid in Mission.PilotUIDs)
                        if (uid == myUid)
                        {
                            myUid = null;
                            break;
                        }
                    break;
                }
            string feNum = DLNK.OwnshipFENumber;
            if (myUid != null)
            {
                for (int i = 0; i < DLNK.TeamMembers.Length; i++)
                    if (DLNK.TeamMembers[i].DriverUID == myUid)
                    {
                        int flight = (i / 4) + 1;
                        int elem = (i % 4) + 1;
                        if (string.IsNullOrEmpty(feNum))
                            feNum = $"{flight}{elem}";
                        else
                            feNum = $"{feNum[0]}{elem}";
                    }
                if (roleInfo.TryGetValue("FLIGHT", out string f) && roleInfo.TryGetValue("ELEM", out string e))
                    feNum = $"{f}{e}";
                if (!string.IsNullOrEmpty(feNum))
                {
                    DLNK.OwnshipFENumber = feNum;
                    if (roleInfo.TryGetValue("CALLSIGN", out string cs))
                        DLNK.OwnshipCallsign = cs;
                    DLNK.IsOwnshipLead = (feNum[1] == '1');
                }
            }

            // determine tacan setup. if we have tacan in role spec string, use that value as lead tacan and
            // adjust based on flight/element determined above. if we don't have tacan in role spec string but we
            // do have flight/element number, we'll infer from the tacan value in the config.
            //
            string tcnChan = Misc.TACANChannel;
            if (roleInfo.TryGetValue("TACAN_CHAN", out string c) && roleInfo.TryGetValue("TACAN_BAND", out string b))
            {
                if (!string.IsNullOrEmpty(feNum) && (feNum[1] == '1'))
                    tcnChan = c;
                else if (!string.IsNullOrEmpty(feNum))
                    tcnChan = $"{int.Parse(c) + 63}";

                Misc.TACANChannel = tcnChan;
                Misc.TACANBand = (b == "X") ? $"{(int)TACANBands.X}" : $"{(int)TACANBands.Y}";
                Misc.TACANMode = $"{(int)TACANModes.AA_TR}";
            }
            else if (!string.IsNullOrEmpty(feNum))
            {
                int tcnChanNum = int.Parse(tcnChan);
                if ((feNum[1] == '1') && (tcnChanNum > 63))
                    tcnChanNum -= 63;
                else if ((feNum[1] != '1') && (tcnChanNum <= 63))
                    tcnChanNum += 63;

                Misc.TACANChannel = $"{tcnChanNum}";
                Misc.TACANMode = $"{(int)TACANModes.AA_TR}";
            }
        }

        public override string RoleHelpText() => "Can include callsign and lead TACAN; for exmaple, “CY11 38Y”";

        public override ISystem SystemForTag(string tag)
        {
            return tag switch
            {
                CMDSSystem.SystemTag => CMDS,
                DLNKSystem.SystemTag => DLNK,
                HARMSystem.SystemTag => HARM,
                HTSSystem.SystemTag => HTS,
                MFDSystem.SystemTag => MFD,
                MiscSystem.SystemTag => Misc,
                RadioSystem.SystemTag => Radio,
                SMSSystem.SystemTag => SMS,
                STPTSystem.SystemTag => STPT,
                CoreSimDTCSystem.SystemTag => DTE,
                CoreKboardSystem.SystemTag => Kboard,
                CoreMissionSystem.SystemTag => Mission,
                _ => null,
            };
        }

        public override void ConfigurationUpdated(string updateSysTag = null)
        {
            F16CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!STPT.IsDefault)
                stpts = $" along with { STPT.Count } steerpoint" + ((STPT.Count > 1) ? "s" : "");
            SystemInfoTextUI = updatesStrings["SystemInfoTextUI"] + stpts;
            SystemInfoIconsUI = updatesStrings["SystemInfoIconsUI"];
            SystemInfoIconBadgesUI = updatesStrings["SystemInfoIconBadgesUI"];

            // when updating the mission system, we may need to push changes down into the dlnk system. if the
            // mission is default, we unlink the datalink system (as what's the point?). if not, we need to
            // update the pilots in slots 1-4 of the dlnk team table to match the mission, minding cases where
            // the mission is decreasing the number of ships in play.
            // 
            if (updateSysTag == CoreMissionSystem.SystemTag)
            {
                if (Mission.IsDefault)
                {
                    DLNK.IsLinkedMission = false;
                }
                else if (DLNK.IsLinkedMission)
                {
                    for (int i = 0; i < Mission.Ships; i++)
                        for (int j = 0; j < DLNK.TeamMembers.Length; j++)
                            if (DLNK.TeamMembers[j].DriverUID == Mission.PilotUIDs[i])
                                DLNK.TeamMembers[j].Reset();
                    for (int i = 0; i < Mission.Ships; i++)
                        DLNK.TeamMembers[i].DriverUID = Mission.PilotUIDs[i];
                }
            }
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null                 => JsonSerializer.Serialize(this, ConfigurationBase.JsonOptions),
                CMDSSystem.SystemTag => JsonSerializer.Serialize(CMDS, ConfigurationBase.JsonOptions),
                DLNKSystem.SystemTag => JsonSerializer.Serialize(DLNK, ConfigurationBase.JsonOptions),
                HARMSystem.SystemTag => JsonSerializer.Serialize(HARM, ConfigurationBase.JsonOptions),
                HTSSystem.SystemTag  => JsonSerializer.Serialize(HTS, ConfigurationBase.JsonOptions),
                MFDSystem.SystemTag  => JsonSerializer.Serialize(MFD, ConfigurationBase.JsonOptions),
                MiscSystem.SystemTag => JsonSerializer.Serialize(Misc, ConfigurationBase.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, ConfigurationBase.JsonOptions),
                SMSSystem.SystemTag => JsonSerializer.Serialize(SMS, ConfigurationBase.JsonOptions),
                STPTSystem.SystemTag => JsonSerializer.Serialize(STPT, ConfigurationBase.JsonOptions),
                CoreSimDTCSystem.SystemTag => JsonSerializer.Serialize(DTE, ConfigurationBase.JsonOptions),
                CoreKboardSystem.SystemTag => JsonSerializer.Serialize(Kboard, ConfigurationBase.JsonOptions),
                CoreMissionSystem.SystemTag => JsonSerializer.Serialize(Mission, ConfigurationBase.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            CMDS    ??= new CMDSSystem();
            DLNK    ??= new DLNKSystem();
            HARM    ??= new HARMSystem();
            HTS     ??= new HTSSystem();
            MFD     ??= new MFDSystem();
            Misc    ??= new MiscSystem();
            Radio   ??= new RadioSystem();
            SMS     ??= new SMSSystem();
            STPT    ??= new STPTSystem();
            DTE     ??= new CoreSimDTCSystem();
            Kboard  ??= new CoreKboardSystem();
            Mission ??= new CoreMissionSystem();

            // TODO: should parse out version number from version string and compare that as an integer
            // TODO: to allow for "update if version older than x".

            if (Version == _versionCfg_10)
                SMS.UpdateFrom10to11();
            Version = _versionCfg;

            ConfigurationUpdated();
        }

        public override bool SaveMergedSimDTC() => (DTE.IsDefault) || SaveMergedSimDTC(DTE);

        public override bool SaveMergedKboards() => (Kboard.IsDefault) || SaveMergedKboards(Kboard);

        public override void AfterSystemEditorCompletes(string systemTag)
        {
            if (MergeableSysTags.Contains(systemTag))
            {
                // kick off a lambda to asynchronously handle any merges. the lambda will operate on a clone of the
                // configuration and must hold the configuration merge mutex to avoid any problems.
                //
                F16CConfiguration mergeCfg = (F16CConfiguration)Clone();
                Task.Run(() =>
                {
                    if (mergeCfg.MergeMutexAcquire())
                    {
                        if (!mergeCfg.DTE.IsDefault && mergeCfg.DTE.EnableRebuildValue)
                            mergeCfg.SaveMergedSimDTC();
                        if (!mergeCfg.Kboard.IsDefault && mergeCfg.Kboard.EnableRebuildValue)
                            mergeCfg.SaveMergedKboards();
                        mergeCfg.MergeMutexRelease();
                    }
                });
            }
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == CMDSSystem.SystemTag) ||
                                              (cboardTag == DLNKSystem.SystemTag) ||
                                              (cboardTag == HARMSystem.SystemTag) ||
                                              (cboardTag == HTSSystem.SystemTag) ||
                                              (cboardTag == MFDSystem.SystemTag) ||
                                              (cboardTag == MiscSystem.SystemTag) ||
                                              (cboardTag == RadioSystem.SystemTag) ||
                                              (cboardTag == SMSSystem.SystemTag) ||
                                              (cboardTag == STPTSystem.SystemTag) ||
                                              (cboardTag == STPTSystem.STPTListTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case CMDSSystem.SystemTag: CMDS = JsonSerializer.Deserialize<CMDSSystem>(json); break;
                    case DLNKSystem.SystemTag: DLNK = JsonSerializer.Deserialize<DLNKSystem>(json); break;
                    case HARMSystem.SystemTag: HARM = JsonSerializer.Deserialize<HARMSystem>(json); break;
                    case HTSSystem.SystemTag: HTS = JsonSerializer.Deserialize<HTSSystem>(json); break;
                    case MFDSystem.SystemTag: MFD = JsonSerializer.Deserialize<MFDSystem>(json); break;
                    case MiscSystem.SystemTag: Misc = JsonSerializer.Deserialize<MiscSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
                    case SMSSystem.SystemTag: SMS = JsonSerializer.Deserialize<SMSSystem>(json); break;
                    case STPTSystem.SystemTag: STPT = JsonSerializer.Deserialize<STPTSystem>(json); break;
                    case STPTSystem.STPTListTag: STPT.ImportSerializedNavpoints(json, false); break;
                    case CoreSimDTCSystem.SystemTag: DTE = JsonSerializer.Deserialize<CoreSimDTCSystem>(json); break;
                    case CoreKboardSystem.SystemTag: Kboard = JsonSerializer.Deserialize<CoreKboardSystem>(json); break;
                    case CoreMissionSystem.SystemTag: Mission = JsonSerializer.Deserialize<CoreMissionSystem>(json); break;
                    default: isHandled = false;  break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated(systemTag);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
