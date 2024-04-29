﻿// ********************************************************************************************************************
//
// RadioBuilder.cs -- a-10c misc system builder
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using JAFDTC.Models.A10C.Misc;
using JAFDTC.Models.DCS;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the radio system in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure misc systems via the cdu and ffds according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build()
        {
            AirframeDevice cdu = _aircraft.GetDevice("CDU");

            if (!_cfg.Misc.IsDefault)
            {
                BuildCoordSystem(cdu, _cfg.Misc);
            }
        }

        /// <summary>
        /// configure the coordinate system according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildCoordSystem(AirframeDevice cdu, MiscSystem miscSystem)
        {
            if (miscSystem.IsCoordSystemDefault)
                return;

            // CDU
            AddActions(cdu, new() { "WP", "LSK_3L" });
            AddWait(WAIT_BASE);
            AddIfBlock("IsCoordFmtLL", null, delegate ()
            {
                AddActions(cdu, new() { "LSK_9R" });
            });

            // TAD
            // Leaving this as a reference for changing the coordinate system on the TAD
            // when we add it as its own system later.
            // AddActions(lmfd, new() { "LMFD_15", "LMFD_09" });

            // TGP
            // Leaving this as a reference for changing the coordinate system on the TGP
            // when we add it as its own system later.
            // AddActions(rmfd, new() { "RMFD_15" });
            // AddWait(WAIT_BASE);
            // AddActions(rmfd, new() { "RMFD_02" });
            // AddWait(WAIT_BASE * 5); // Wait for TGP to go active.
            // AddActions(rmfd, new() { "RMFD_01" });
            // AddWait(WAIT_BASE);
            // AddActions(rmfd, new() { "RMFD_07", "RMFD_01", "RMFD_03", });
        }
    }
}