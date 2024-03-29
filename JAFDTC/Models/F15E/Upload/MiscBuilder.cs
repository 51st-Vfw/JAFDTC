﻿// ********************************************************************************************************************
//
// MiscBuilder.cs -- f-15e misc system command builder
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
using JAFDTC.Models.F15E.Misc;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the miscellaneous systems (BINGO, CARA, TACAN, ILS) in the mudhen. translates misc setup
    /// in F15EConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure miscellaneous system (bingo, cara, tacan, ils) via the ufc according to the non-default
        /// programming settings (this function is safe to call with a configuration with default settings: defaults
        /// are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC_PILOT");
            AirframeDevice fltInst = _aircraft.GetDevice("FLTINST");

            BuildBingo(fltInst);
            BuildCARA(ufc);
            BuildTACAN(ufc); 
            BuildILS(ufc);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildBingo(AirframeDevice fltInst)
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                int bingo = int.Parse(_cfg.Misc.Bingo);
                // TODO: handle this through a dcs-side loop?
                for (int i = 0; i < 140; i++)
                {
                    AddAction(fltInst, "BingoDecrease");
                }
                for (int i = 0; i < bingo / 100; i++)
                {
                    AddAction(fltInst, "BingoIncrease");
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildCARA(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsLowAltDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU" });
                AddActions(ufc, ActionsForString(_cfg.Misc.LowAltWarn), new() { "PB1" });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildTACAN(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsTACANDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU", "PB2" });

                AddActions(ufc, ActionsForString(_cfg.Misc.TACANChannel.ToString()), new() { "PB1" });

                string band = (_cfg.Misc.TACANBandValue == TACANBands.X) ? "Y" : "X";
                AddIfBlock("IsTACANBand", new() { band }, delegate () { AddAction(ufc, "PB1"); });

                string modeButton = _cfg.Misc.TACANModeValue switch
                {
                    TACANModes.A2A => "PB2",
                    TACANModes.TR => "PB3",
                    TACANModes.REC => "PB4",
                    _ => "PB2"
                };

                AddActions(ufc, new() { modeButton, "PB10", "MENU" });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildILS(AirframeDevice ufc)
        {
            if (!_cfg.Misc.IsILSDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU", "MENU", "PB3" });

                AddActions(ufc, ActionsForString(AdjustNoSeparators(_cfg.Misc.ILSFrequency)), new() { "PB3" });

                AddAction(ufc, "MENU");
            }
        }
    }
}
