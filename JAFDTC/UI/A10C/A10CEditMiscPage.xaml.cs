// ********************************************************************************************************************
//
// A10CEditMiscPage.xaml.cs : ui c# for warthog miscellaneous page
//
// Copyright(C) 2024 fizzle
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.Misc;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 Miscellaneous editor.
    /// </summary>
    public sealed partial class A10CEditMiscPage : SystemEditorPageBase
    {
        private const string SYSTEM_NAME = "Miscellaneous";

        public override SystemBase SystemConfig => ((A10CConfiguration)Config).Misc;
        protected override string SystemTag => MiscSystem.SystemTag;
        protected override string SystemName => SYSTEM_NAME;

        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.MISC, typeof(A10CEditMiscPage));

        public A10CEditMiscPage()
        {
            InitializeComponent();
            InitializeBase(new MiscSystem(), uiTextTACANChannel, uiCtlLinkResetBtns);
        }
    }
}
