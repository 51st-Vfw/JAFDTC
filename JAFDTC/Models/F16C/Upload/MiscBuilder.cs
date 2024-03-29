﻿// ********************************************************************************************************************
//
// MiscBuilder.cs -- f-16c miscellaneous command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2024 ilominar/raven
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.Misc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the miscellaneous setups in the viper (TACAN/ILS, ALOW, BINDO, LASR, BULLS, and HMCS).
    /// translates cmds setup in F16CConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(F16CConfiguration cfg, F16DeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure misc systems (TACAN/ILS, ALOW, BINDO, LASR, BULLS, and HMCS) via the icp/ded according to the
        /// non-default programming settings (this function is safe to call with a configuration with default settings:
        /// defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice ehsi = _aircraft.GetDevice("EHSI");
            AirframeDevice hmcsInt = _aircraft.GetDevice("HMCS_INT");

            if (!_cfg.Misc.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN" });

                BuildTILS(ufc, ehsi);
                BuildALOW(ufc);
                BuildBingo(ufc);
                BuildLaserSettings(ufc);
                BuildBullseye(ufc);
                BuildHMCS(ufc, hmcsInt);
            }
        }

        /// <summary>
        /// configure icp t-ils (1) programming includes tacan (and ehsi mode) and ils via the icp/ded according to the
        /// non-default programming settings (this function is safe to call with a configuration with default settings:
        /// defaults are skipped as necessary).
        /// <summary>
        private void BuildTILS(AirframeDevice ufc, AirframeDevice ehsi)
        {
            AddAction(ufc, "1");

            // TODO: do a better job detecting defaults here to avoid just moving around ded.

            // ---- tacan mode

            if (_cfg.Misc.TACANIsYardstickValue)
            {
                // TODO: ideally, want a start condition here on current mode, assume default rec here
                AddActions(ufc, new() { "SEQ", "SEQ" });
            }

            // ---- tacan channel

            AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.TACANChannel));

            // ---- tacan channel

            string cond = (_cfg.Misc.TACANBandValue == TACANBands.X) ? "TACANBandY" : "TACANBandX";
            AddIfBlock(cond, null, delegate () { AddActions(ufc, new() { "0", "ENTR" }); });

            // ---- ehsi mode

            // TODO: ideally, want a start condition here on current ehsi mode, assume default nav here
            AddActions(ehsi, new() { "MODE", "MODE" });

            // ---- ils

            AddActions(ufc, new() { "DOWN", "DOWN" });

            List<string> actions = PredActionsForCleanNumAndEnter(_cfg.Misc.ILSFrequency);
            AddActions(ufc, actions);
            if (actions.Count == 0)
            {
                AddAction(ufc, "DOWN");
            }
            AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.ILSCourse));

            AddAction(ufc, "RTN");
        }

        /// <summary>
        /// configure icp alow (icp 2) programming via the icp/ded according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        private void BuildALOW(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsALOWDefault)
            {
                AddAction(ufc, "2");
                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.ALOWCARAALOW), new() { "DOWN" });
                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.ALOWMSLFloor), new() { "DOWN" });
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure ded bngo (list 2) programming via the icp/ded according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        private void BuildBingo(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                AddActions(ufc, new() { "LIST", "2" });
                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.Bingo));
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure ded lasr (list misc, 5) programming via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped
        /// as necessary).
        /// <summary>
        private void BuildLaserSettings(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsLaserDefault)
            {
                AddActions(ufc, new() { "LIST", "0", "5" });

                // ---- tgp designator laser code

                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.LaserTGPCode), new() { "DOWN" });

                // ---- tgp lst laser code

                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.LaserLSTCode), new() { "DOWN" });

                // ---- tgp laser start time

                AddActions(ufc, PredActionsForNumAndEnter(_cfg.Misc.LaserStartTime));

                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure ded bull (list misc, 8) via the icp/ded according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        private void BuildBullseye(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsBULLDefault)
            {
                AddActions(ufc, new() { "LIST", "0", "8" });

                AddWait(WAIT_BASE);

                // TODO: assumes bullseye state
                AddIfBlock("BullseyeNotSelected", null, delegate () { AddAction(ufc, "0"); });
                AddAction(ufc, "DOWN");

                AddActions(ufc, PredActionsForCleanNumAndEnter(_cfg.Misc.BullseyeWP), new() { "DOWN" });

                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure jhms (list misc, rcl) programming via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped
        /// as necessary).
        /// <summary>
        private void BuildHMCS(AirframeDevice ufc, AirframeDevice hmcsInt)
        {
            if (!_cfg.Misc.IsHMCSDefault)
            {
                AddActions(ufc, new() { "LIST", "0", "RCL" });

                // TODO: check current state, assume enabled by default for now
                AddAction(ufc, (!_cfg.Misc.HMCSBlankHUDValue) ? "0" : "DOWN");
                AddWait(WAIT_BASE);

                // TODO: check current state, assume enabled by default for now
                AddAction(ufc, (!_cfg.Misc.HMCSBlankCockpitValue) ? "0" : "DOWN");
                AddWait(WAIT_BASE);

                // TODO: check current state, assume lvl1 by default for now
                if (_cfg.Misc.HMCSDeclutterLvlValue != HMCSDeclutterLevels.LVL1)
                {
                    AddAction(ufc, "1");
                }
                if (_cfg.Misc.HMCSDeclutterLvlValue == HMCSDeclutterLevels.LVL3)
                {
                    AddAction(ufc, "1");
                }
                AddWait(WAIT_BASE);
                AddAction(ufc, "DOWN");

                // TODO: check current state, assume enabled by default for now
                if (!_cfg.Misc.HMCSDisplayRWRValue)
                {
                    AddAction(ufc, "0");
                }
                AddWait(WAIT_BASE);

                if (!string.IsNullOrEmpty(_cfg.Misc.HMCSIntensity))
                {
                    double intensity = double.Parse(_cfg.Misc.HMCSIntensity);
                    AddDynamicAction(hmcsInt, "INT", intensity, intensity);
                }

                AddAction(ufc, "RTN");
            }
        }
    }
}