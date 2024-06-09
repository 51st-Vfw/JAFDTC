﻿// ********************************************************************************************************************
//
// DLNKBuilder.cs -- f-16c dlnk command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.DLNK;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the datalink system through the ded/ufc according to an
    /// F16CConfiguration. the stream returns the ded to its default page. the builder does not require any state to
    /// function.
    /// </summary>
    internal class DLNKBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DLNKBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure cmds system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.DLNK.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "ENTR", "SEQ" });    // dlnk, tndl page

                AddActions(ufc, new() { "DOWN", "DOWN", "DOWN" });                  // flight/element number
                if (!string.IsNullOrEmpty(_cfg.DLNK.OwnshipFENumber))
                {
                    AddActions(ufc, ActionsForString(_cfg.DLNK.OwnshipFENumber), new() { "ENTR" });
                }
                else
                {
                    AddAction(ufc, "DOWN");
                }

                string cs = _cfg.DLNK.OwnshipCallsign;
                if (!string.IsNullOrEmpty(cs))
                {
                    AddWhileBlock("IsCallSignChar", false, new() { "1", $"{cs[0]}" }, delegate ()
                    {
                        AddAction(ufc, "INC");
                    });
                    AddAction(ufc, "ENTR");
                    AddWhileBlock("IsCallSignChar", false, new() { "2", $"{cs[1]}" }, delegate ()
                    {
                        AddAction(ufc, "INC");
                    });
                    AddAction(ufc, "ENTR");
                }
                else
                {
                    AddAction(ufc, "DOWN");
                }

                string flParam = (_cfg.DLNK.IsOwnshipLead) ? "NO" : "YES";
                AddIfBlock("IsFlightLead", true, new() { flParam }, delegate () { AddAction(ufc, "1"); });

                AddAction(ufc, "SEQ");

                // set up TDOA, TNDL in two passes as hitting "ENTR" to commit TNDL clears TDOA. first pass will fill
                // in all TNDL values, second pass will fill in all TDOA values. setup ownship between passes.

                for (int i = 0; i < _cfg.DLNK.TeamMembers.Length; i++)
                {
                    TeamMember tm = _cfg.DLNK.TeamMembers[i];
                    string tndl = (!string.IsNullOrEmpty(tm.TNDL)) ? tm.TNDL : "00000";
                    AddActions(ufc, new() { "DOWN" }, PredActionsForNumAndEnter(tndl));
                }

                AddActions(ufc, PredActionsForNumAndEnter(_cfg.DLNK.Ownship), new() { "DOWN" });

                for (int i = 0; i < _cfg.DLNK.TeamMembers.Length; i++)
                {
                    bool expect = (_cfg.DLNK.TeamMembers[i].TDOA) ? false : true;
                    AddIfBlock("IsTDOASet", expect, new() { (i + 1).ToString() }, delegate () { AddAction(ufc, "7"); });
                    AddActions(ufc, new() { "DOWN", "DOWN" });
                }

                AddAction(ufc, "RTN");
            }
        }
    }
}
