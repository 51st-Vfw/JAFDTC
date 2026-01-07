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
using JAFDTC.Models.F16C.CMDS;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.Models.F16C.STPT;
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
using Windows.Graphics.Printing.PrintSupport;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// configuration object for the viper that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation,
    /// countermeasure, datalink, harm, hts, mfd, radio, and miscellaneous systems.
    /// </summary>
    public partial class F16CConfiguration : ConfigurationBase
    {
        private const string _versionCfg = "F16C-1.1";          // current version

        // v1.0 --> v1.1:
        // - interpretation of ReleaseMode, RipplePulse fields in the sms system changed.
        //
        private const string _versionCfg_10 = "F16C-1.0";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- regular expressions

        [GeneratedRegex(@"^[0-7]{5}$")]
        public static partial Regex RegexTNDL();

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

        public SimDTCSystem DTE { get; set; }

        public SimKboardSystem Kboard { get; set; }

        [JsonIgnore]
        public override List<string> MergeableSysTags =>
        [
            MiscSystem.SystemTag,
            STPTSystem.SystemTag,
            RadioSystem.SystemTag,
            CMDSSystem.SystemTag
        ];

        public override List<string> MergeTagsKneeboard => [.. Kboard.KneeboardTags ];

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
            DTE = new SimDTCSystem();
            Kboard = new SimKboardSystem();
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
                DTE = (SimDTCSystem)DTE.Clone(),
                Kboard = (SimKboardSystem)Kboard.Clone()
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
                case SimDTCSystem.SystemTag: DTE = otherViper.DTE.Clone() as SimDTCSystem; break;
                case SimKboardSystem.SystemTag: Kboard = otherViper.Kboard.Clone() as SimKboardSystem; break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // internal methods
        //
        // ------------------------------------------------------------------------------------------------------------

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

        public override bool ValidateRole(string role) => (ParseRole(role) != null);

        public override void AdjustForRole(string role)
        {
            Dictionary<string, string> roleInfo = ParseRole(role);

            // determine ownship flight/element number. we will check (in order): (1) value in config, (2) value
            // inferred from position of pilot matching callsign from settings in team list, or (3) value from
            // role specification string. later over-rides earlier, so (3) is selected if both (2) and (3) exist.
            //
            string myUid = null;
            foreach (ViperDriver driver in F16CPilotsDbase.LoadDbase())
                if (driver.Name == Settings.Callsign)
                {
                    myUid = driver.UID;
                    break;
                }
            string feNum = DLNK.OwnshipFENumber;
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
                SimDTCSystem.SystemTag => DTE,
                SimKboardSystem.SystemTag => Kboard,
                _ => null,
            };
        }

        public override bool IsMergedToDTC(string systemTag) => DTE.MergedSystemTags.Contains(systemTag);

        public override void ConfigurationUpdated()
        {
            F16CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!STPT.IsDefault)
                stpts = $" along with { STPT.Count } steerpoint" + ((STPT.Count > 1) ? "s" : "");
            UpdatesInfoTextUI = updatesStrings["UpdatesInfoTextUI"] + stpts;
            UpdatesIconsUI = updatesStrings["UpdatesIconsUI"];
            UpdatesIconBadgesUI = updatesStrings["UpdatesIconBadgesUI"];
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
                SimDTCSystem.SystemTag => JsonSerializer.Serialize(DTE, ConfigurationBase.JsonOptions),
                SimKboardSystem.SystemTag => JsonSerializer.Serialize(Kboard, ConfigurationBase.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            CMDS   ??= new CMDSSystem();
            DLNK   ??= new DLNKSystem();
            HARM   ??= new HARMSystem();
            HTS    ??= new HTSSystem();
            MFD    ??= new MFDSystem();
            Misc   ??= new MiscSystem();
            Radio  ??= new RadioSystem();
            SMS    ??= new SMSSystem();
            STPT   ??= new STPTSystem();
            DTE    ??= new SimDTCSystem();
            Kboard ??= new SimKboardSystem();

            // TODO: should parse out version number from version string and compare that as an integer
            // TODO: to allow for "update if version older than x".

            if (Version == _versionCfg_10)
                SMS.UpdateFrom10to11();
            Version = _versionCfg;

            ConfigurationUpdated();
        }

        public override bool SaveMergedSimDTC()
            => (DTE.IsDefault) || SaveMergedSimDTC(DTE.Template, DTE.OutputPath);

        public override bool SaveMergedKboards()
            => (Kboard.IsDefault) || SaveMergedKboards(Kboard.Template, Kboard.OutputPath,
                                                       Kboard.EnableNightValue, Kboard.EnableSVGValue);

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
                    case SimDTCSystem.SystemTag: DTE = JsonSerializer.Deserialize<SimDTCSystem>(json); break;
                    case SimKboardSystem.SystemTag: Kboard = JsonSerializer.Deserialize<SimKboardSystem>(json); break;
                    default: isHandled = false;  break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated();
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
