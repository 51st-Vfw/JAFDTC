﻿// ********************************************************************************************************************
//
// WYPTBuilder.cs -- a-10c waypoint command builder
//
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

using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.DCS;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the waypoint system in the warthog. translates cmds setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class WYPTBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure waypoint system via the cdu according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build()
        {
            ObservableCollection<WaypointInfo> wypts = _cfg.WYPT.Points;
            AirframeDevice cdu = _aircraft.GetDevice("CDU");

            if (wypts.Count > 0)
            {
                // TODO: set STEER_PT to MISSION
                // TODO: set PAGE to OTHER

                AddActions(cdu, new() { "WP", "LSK_3L" });
                AddWait(WAIT_BASE);

                AddActions(cdu, new() { "CLR", "CLR" });
                AddWait(WAIT_BASE);

                BuildWaypoints(cdu, wypts);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypoints(AirframeDevice cdu, ObservableCollection<WaypointInfo> jetWypts)
        {
            for (var i = 0; i < jetWypts.Count; i++)
            {
                string wyptID = jetWypts[i].Number.ToString();
                WaypointInfo wypt = jetWypts[i];

                if (wypt.IsValid)
                {
                    // TODO: set waypoint id explicitly?
                    // AddActions(cdu, ActionsForString(wyptID), new() { "LSK_3L", "CLR", "CLR" });

                    AddAction(cdu, "LSK_7R");
                    AddWait(WAIT_BASE);
                    AddActions(cdu, new() { "CLR", "CLR" });

                    BuildWaypointName(cdu, wyptID, wypt.Name);
                    
                    BuildWaypointCoords(cdu, wypt);

                    AddActions(cdu, ActionsForString(Math.Max(int.Parse(wypt.Alt), 0).ToString()), new() { "LSK_5L" });
                    AddWait(WAIT_BASE);
                    AddActions(cdu, new() { "CLR", "CLR" });
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypointName(AirframeDevice cdu, string wyptID, string wyptName)
        {
            wyptName = Regex.Replace(wyptName.ToUpper(), "^[^A-Z]+", "");
            wyptName = Regex.Replace(wyptName, "[^A-Z0-9 ]", "");
            if (string.IsNullOrEmpty(wyptName))
            {
                wyptName = $"WP{wyptID}";
            }
            else if (wyptName.Length > 12)
            {
                wyptName = wyptName[..12];
            }
            AddActions(cdu, ActionsForString(wyptName), new(){ "LSK_3R" });
            AddWait(WAIT_BASE);
            AddActions(cdu, new() { "CLR", "CLR" });
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypointCoords(AirframeDevice cdu, WaypointInfo wypt)
        {
            AddActions(cdu, ActionsForString(AdjustNoSeparators(wypt.LatUI.Replace(" ", ""))), new() { "LSK_7L" });
            AddWait(WAIT_BASE);
            AddActions(cdu, new() { "CLR", "CLR" });

            AddActions(cdu, ActionsForString(AdjustNoSeparators(wypt.LonUI.Replace(" ", ""))), new() { "LSK_9L" });
            AddWait(WAIT_BASE);
            AddActions(cdu, new() { "CLR", "CLR" });
        }
    }
}
