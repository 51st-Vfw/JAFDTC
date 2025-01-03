﻿// ********************************************************************************************************************
//
// HTSBuilder.cs -- f-16c hts threat command builder
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the hts manual table through the ded/ufc according to an
    /// F16CConfiguration. the stream returns the ded to its default page. the builder does not require any state to
    /// function.
    /// </summary>
    internal class HTSManTableBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HTSManTableBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure hts system via the ded/ufc according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.HTS.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== HTSManTableBuilder:Build()" });

            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            // NOTE: hts is only shown on ded list in a-g master mode

            SwitchMasterModeAG(ufc, true);                      // nav to a-g
            SelectDEDPage(ufc, "0");
            AddIfBlock("IsHTSOnDED", true, null, delegate ()
            {
                AddAction(ufc, "ENTR");
                for (int row = 0; row < _cfg.HTS.MANTable.Count; row++)
                {
                    List<string> actions = PredActionsForNumAndEnter(_cfg.HTS.MANTable[row].Code);
                    AddActions(ufc, actions);
                    if (actions.Count == 0)
                        AddAction(ufc, "DOWN");
                }
            });
            SwitchMasterModeAG(ufc, false);                     // a-g to nav, ded to default
        }
    }
}