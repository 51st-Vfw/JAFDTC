﻿// ********************************************************************************************************************
//
// A10CConfiguration.cs -- a-10c airframe configuration
//
// Copyright(C) 2023-2025 ilominar/raven, JAFDTC contributors
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

using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Models.A10C.Misc;
using JAFDTC.Models.A10C.Radio;
using JAFDTC.Models.A10C.TAD;
using JAFDTC.Models.A10C.TGP;
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.UI.A10C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using JAFDTC.Models.A10C.IFFCC;

namespace JAFDTC.Models.A10C
{
    /// <summary>
    /// configuration object for the warthog that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation
    /// system.
    /// </summary>
    public class A10CConfiguration : Configuration
    {
        private const string _versionCfg = "A10C-1.0";          // current version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public DSMSSystem DSMS { get; set; }

        public HMCSSystem HMCS { get; set; }

        public IFFCCSystem IFFCC { get; set; }

        public MiscSystem Misc { get; set; }

        public RadioSystem Radio { get; set; }

        public TADSystem TAD { get; set; }

        public TGPSystem TGP { get; set; }

        public WYPTSystem WYPT { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new A10CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.A10C, uid, name, linkedSysMap)
        {
            DSMS = new DSMSSystem();
            HMCS = new HMCSSystem();
            IFFCC = new IFFCCSystem();
            Misc = new MiscSystem();
            Radio = new RadioSystem();
            TAD = new TADSystem();
            TGP = new TGPSystem();
            WYPT = new WYPTSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = new();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
            {
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            }
            A10CConfiguration clone = new(UID, Name, linkedSysMap)
            {
                DSMS = (DSMSSystem)DSMS.Clone(),
                HMCS = (HMCSSystem)HMCS.Clone(),
                IFFCC = (IFFCCSystem)IFFCC.Clone(),
                Misc = (MiscSystem)Misc.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                TAD = (TADSystem)TAD.Clone(),
                TGP = (TGPSystem)TGP.Clone(),
                WYPT = (WYPTSystem)WYPT.Clone(),
            };
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            A10CConfiguration otherHawg = (A10CConfiguration)other;
            switch (systemTag)
            {
                case DSMSSystem.SystemTag: DSMS = (DSMSSystem)otherHawg.DSMS.Clone(); break;
                case HMCSSystem.SystemTag: HMCS = (HMCSSystem)otherHawg.HMCS.Clone(); break;
                case IFFCCSystem.SystemTag: IFFCC = (IFFCCSystem)otherHawg.IFFCC.Clone(); break;
                case MiscSystem.SystemTag: Misc = (MiscSystem)otherHawg.Misc.Clone(); break;
                case RadioSystem.SystemTag: Radio = (RadioSystem)otherHawg.Radio.Clone(); break;
                case TADSystem.SystemTag: TAD = (TADSystem)otherHawg.TAD.Clone(); break;
                case TGPSystem.SystemTag: TGP = (TGPSystem)otherHawg.TGP.Clone(); break;
                case WYPTSystem.SystemTag: WYPT = (WYPTSystem)otherHawg.WYPT.Clone(); break;
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
                DSMSSystem.SystemTag => DSMS,
                HMCSSystem.SystemTag => HMCS,
                IFFCCSystem.SystemTag => IFFCC,
                MiscSystem.SystemTag => Misc,
                RadioSystem.SystemTag => Radio,
                TADSystem.SystemTag => TAD,
                TGPSystem.SystemTag => TGP,
                WYPTSystem.SystemTag => WYPT,
                _ => null,
            };
        }

        public override void ConfigurationUpdated()
        {
            A10CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!WYPT.IsDefault)
            {
                stpts = $" along with {WYPT.Count} waypoint" + ((WYPT.Count > 1) ? "s" : "");
            }
            UpdatesInfoTextUI = updatesStrings["UpdatesInfoTextUI"] + stpts;
            UpdatesIconsUI = updatesStrings["UpdatesIconsUI"];
            UpdatesIconBadgesUI = updatesStrings["UpdatesIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null => JsonSerializer.Serialize(this, Configuration.JsonOptions),
                DSMSSystem.SystemTag => JsonSerializer.Serialize(DSMS, Configuration.JsonOptions),
                HMCSSystem.SystemTag => JsonSerializer.Serialize(HMCS, Configuration.JsonOptions),
                IFFCCSystem.SystemTag => JsonSerializer.Serialize(IFFCC, Configuration.JsonOptions),
                MiscSystem.SystemTag => JsonSerializer.Serialize(Misc, Configuration.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, Configuration.JsonOptions),
                TADSystem.SystemTag => JsonSerializer.Serialize(TAD, Configuration.JsonOptions),
                TGPSystem.SystemTag => JsonSerializer.Serialize(TGP, Configuration.JsonOptions),
                WYPTSystem.SystemTag => JsonSerializer.Serialize(WYPT, Configuration.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            DSMS ??= new DSMSSystem();
            HMCS ??= new HMCSSystem();
            IFFCC ??= new IFFCCSystem();
            Misc ??= new MiscSystem();
            Radio ??= new RadioSystem();
            TAD ??= new TADSystem();
            TGP ??= new TGPSystem();
            WYPT ??= new WYPTSystem();

            // TODO: if the version number is older than current, may need to update object
            Version = _versionCfg;

            Save(this);
            DSMS.FixupMunitionReferences(); // TODO use JsonConstructer like HMCSSystem
            ConfigurationUpdated();
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return !string.IsNullOrEmpty(cboardTag) &&
                   (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                   ((systemTag == null) && (cboardTag == DSMSSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == HMCSSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == IFFCCSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == MiscSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == RadioSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == TADSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == TGPSystem.SystemTag)) ||
                   ((systemTag == null) && (cboardTag == WYPTSystem.SystemTag)));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case DSMSSystem.SystemTag: DSMS = JsonSerializer.Deserialize<DSMSSystem>(json); break;
                    case HMCSSystem.SystemTag: HMCS = JsonSerializer.Deserialize<HMCSSystem>(json); break;
                    case IFFCCSystem.SystemTag: IFFCC = JsonSerializer.Deserialize<IFFCCSystem>(json); break;
                    case MiscSystem.SystemTag: Misc = JsonSerializer.Deserialize<MiscSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
                    case TADSystem.SystemTag: TAD = JsonSerializer.Deserialize<TADSystem>(json); break;
                    case TGPSystem.SystemTag: TGP = JsonSerializer.Deserialize<TGPSystem>(json); break;
                    case WYPTSystem.SystemTag: WYPT = JsonSerializer.Deserialize<WYPTSystem>(json); break;
                    default: isHandled = false; break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated();
                    Save(this);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"A10CConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
