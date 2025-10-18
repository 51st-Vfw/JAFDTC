﻿// ********************************************************************************************************************
//
// FA18CConfiguration.cs -- fa-18c airframe configuration
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

using JAFDTC.Models.Base;
using JAFDTC.Models.FA18C.CMS;
using JAFDTC.Models.FA18C.PP;
using JAFDTC.Models.FA18C.Radio;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.UI.FA18C;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;

namespace JAFDTC.Models.FA18C
{
    /// <summary>
    /// configuration object for the hornet that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation,
    /// countermeasure, and radio systems.
    /// </summary>
    public partial class FA18CConfiguration : Configuration
    {
        private const string _versionCfg = "FA18C-1.0";         // current version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMSSystem CMS { get; set; }

        public PPSystem PP { get; set; }

        public RadioSystem Radio { get; set; }

        public WYPTSystem WYPT { get; set; }

        public SimDTCSystem MUMI { get; set; }

        [JsonIgnore]
        public override List<string> MergeableSysTagsForDTC => new()
        {
            RadioSystem.SystemTag,
            CMSSystem.SystemTag
        };

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new FA18CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.FA18C, uid, name, linkedSysMap)
        {
            CMS = new CMSSystem();
            PP = new PPSystem();
            Radio = new RadioSystem();
            WYPT = new WYPTSystem();
            MUMI = new SimDTCSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = new();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            FA18CConfiguration clone = new("", Name, linkedSysMap)
            {
                CMS = (CMSSystem)CMS.Clone(),
                PP = (PPSystem)PP.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                WYPT = (WYPTSystem)WYPT.Clone(),
                MUMI = (SimDTCSystem)MUMI.Clone()
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            FA18CConfiguration otherHornet = other as FA18CConfiguration;
            switch (systemTag)
            {
                case CMSSystem.SystemTag: CMS = otherHornet.CMS.Clone() as CMSSystem; break;
                case PPSystem.SystemTag: PP = otherHornet.PP.Clone() as PPSystem; break;
                case RadioSystem.SystemTag: Radio = otherHornet.Radio.Clone() as RadioSystem; break;
                case WYPTSystem.SystemTag: WYPT = otherHornet.WYPT.Clone() as WYPTSystem; break;
                case SimDTCSystem.SystemTag: MUMI = otherHornet.MUMI.Clone() as SimDTCSystem; break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // overriden class methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override ISystem SystemForTag(string tag)
        {
            return tag switch
            {
                CMSSystem.SystemTag => CMS,
                PPSystem.SystemTag => PP,
                RadioSystem.SystemTag => Radio,
                WYPTSystem.SystemTag => WYPT,
                SimDTCSystem.SystemTag => MUMI,
                _ => null,
            };
        }

        public override bool IsMerged(string systemTag) => MUMI.MergedSystemTags.Contains(systemTag);

        public override void ConfigurationUpdated()
        {
            FA18CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!WYPT.IsDefault)
                stpts = $" along with {WYPT.Count} waypoint" + ((WYPT.Count > 1) ? "s" : "");
            UpdatesInfoTextUI = updatesStrings["UpdatesInfoTextUI"] + stpts;
            UpdatesIconsUI = updatesStrings["UpdatesIconsUI"];
            UpdatesIconBadgesUI = updatesStrings["UpdatesIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null => JsonSerializer.Serialize(this, Configuration.JsonOptions),
                CMSSystem.SystemTag => JsonSerializer.Serialize(CMS, Configuration.JsonOptions),
                PPSystem.SystemTag => JsonSerializer.Serialize(PP, Configuration.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, Configuration.JsonOptions),
                WYPTSystem.SystemTag => JsonSerializer.Serialize(WYPT, Configuration.JsonOptions),
                SimDTCSystem.SystemTag => JsonSerializer.Serialize(MUMI, Configuration.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            CMS ??= new CMSSystem();
            PP ??= new PPSystem();
            Radio ??= new RadioSystem();
            WYPT ??= new WYPTSystem();
            MUMI ??= new SimDTCSystem();

            // TODO: if the version number is older than current, may need to update object
            Version = _versionCfg;

            ConfigurationUpdated();
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == CMSSystem.SystemTag) ||
                                              (cboardTag == PPSystem.SystemTag) ||
                                              (cboardTag == RadioSystem.SystemTag) ||
                                              (cboardTag == WYPTSystem.SystemTag) ||
                                              (cboardTag == SimDTCSystem.SystemTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case CMSSystem.SystemTag: CMS = JsonSerializer.Deserialize<CMSSystem>(json); break;
                    case PPSystem.SystemTag: PP = JsonSerializer.Deserialize<PPSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
                    case WYPTSystem.SystemTag: WYPT = JsonSerializer.Deserialize<WYPTSystem>(json); break;
                    case SimDTCSystem.SystemTag: MUMI = JsonSerializer.Deserialize<SimDTCSystem>(json); break;
                    default: isHandled = false; break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated();
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"FA18CConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
