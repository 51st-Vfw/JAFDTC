// ********************************************************************************************************************
//
// IEditSimulatorKboardPageHelper.cs : interface for EditSimulatorKboardPage helper classes
//
// Copyright(C) 2026 ilominar/raven
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.UI.App;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// interface for the EditSimulatorKboardPage ui page helper class responsible for specializing the
    /// EditSimulatorKboardPage base behavior for a specific airframe.
    /// </summary>
    public interface IEditSimulatorKboardPageHelper
    {
        /// <summary>
        /// return the system to configure from the overall configuration.
        /// </summary>
        public SystemBase GetSystemConfig(IConfiguration config);

        /// <summary>
        /// returns a lists of systems that can contribute to a kneeboard build.
        /// </summary>
        public List<ConfigEditorPageInfo> ContentSystems { get; }

        /// <summary>
        /// validate the kneeboard configuration is correct. this checks to ensure the output path is valid and
        /// the template is known. the configuration is updated if necessary.
        /// </summary>
        public void ValidateKboardSystem(IConfiguration config);

        /// <summary>
        /// update the edit state for dtc from the configuration. the update will perform a deep copy of the
        /// data from the configuration.
        /// </summary>
        public void CopyConfigToEdit(IConfiguration config, SimKboardSystem editKboard);

        /// <summary>
        /// update the configuration dtc from the edit state. the update will perform a deep copy of the
        /// data from the configuration.
        /// </summary>
        public void CopyEditToConfig(SimKboardSystem editKboard, IConfiguration config);
    }
}
