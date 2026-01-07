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
            => new(CoreKboardSystem.SystemTag, "Kneeboards", "Kneeboards", "\xF0E3", typeof(EditSimulatorKboardPage),
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
            ((F16CConfiguration)config).Kboard.ValidateForAirframe(config.Airframe);
        }

        public void CopyConfigToEdit(IConfiguration config, CoreKboardSystem editKboard)
        {
            editKboard.Template = new(((F16CConfiguration)config).Kboard.Template);
            editKboard.OutputPath = new(((F16CConfiguration)config).Kboard.OutputPath);
            editKboard.KneeboardTags = [ ];
            foreach (string tag in ((F16CConfiguration)config).Kboard.KneeboardTags)
                editKboard.KneeboardTags.Add(tag);
            editKboard.EnableRebuild = new(((F16CConfiguration)config).Kboard.EnableRebuild);
            editKboard.EnableSVG = new(((F16CConfiguration)config).Kboard.EnableSVG);
        }

        public void CopyEditToConfig(CoreKboardSystem editKboard, IConfiguration config)
        {
            ((F16CConfiguration)config).Kboard.Template = new(editKboard.Template);
            ((F16CConfiguration)config).Kboard.OutputPath = new(editKboard.OutputPath);
            ((F16CConfiguration)config).Kboard.KneeboardTags = [ ];
            foreach (string tag in editKboard.KneeboardTags)
                ((F16CConfiguration)config).Kboard.KneeboardTags.Add(tag);
            ((F16CConfiguration)config).Kboard.EnableRebuild = new(editKboard.EnableRebuild);
            ((F16CConfiguration)config).Kboard.EnableSVG = new(editKboard.EnableSVG);
        }
    }
}
