// ********************************************************************************************************************
//
// F16CEditSimulatorKboardPageHelper.cs : viper specialization for EditSimulatorKboardPage helper object
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
using JAFDTC.Models.F16C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// helper class for airframe-specific customizations on EditSimulatorKboardPage.
    /// </summary>
    internal class F16CEditSimulatorKboardPageHelper : IEditSimulatorKboardPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new("TODO_TAG", "Kneeboards", "Kneeboards", "\xF0E3", typeof(EditSimulatorKboardPage),
                   typeof(F16CEditSimulatorKboardPageHelper));

        public SystemBase GetSystemConfig(IConfiguration config) => ((F16CConfiguration)config).Kboard;

        public List<ConfigEditorPageInfo> ContentSystems =>
        [
            F16CEditSteerpointListPage.PageInfo,
            F16CEditRadioPageHelper.PageInfo,
            F16CEditDLNKPage.PageInfo
        ];

        public void ValidateKboardSystem(IConfiguration config)
        {
//            ((F16CConfiguration)config).DTE.ValidateForAirframe(config.Airframe);
        }

        public void CopyConfigToEdit(IConfiguration config, SimKboardSystem editKboard)
        {
        }

        public void CopyEditToConfig(SimKboardSystem editKboard, IConfiguration config)
        {
        }
    }
}
