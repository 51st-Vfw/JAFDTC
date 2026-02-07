// ********************************************************************************************************************
//
// F16CEditSteerpointPageNavArgs.xaml.cs : navigation arguments for viper steerpoint editor page
//
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.Models.F16C;
using JAFDTC.UI.Controls.Map;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// navigation argument to pass from pages that navigate to the steerpoint editor (F16CEditSteerpointPage). this
    /// provides the configuration being edited along with the specific steerpoint within the configuration that
    /// should be edited.
    /// </summary>
    public sealed class F16CEditSteerpointPageNavArgs
    {
        public F16CEditSteerpointListPage ParentEditor { get; set; }

        public IMapControlVerbMirror VerbMirror { get; set; }   // map window (may be null)

        public F16CConfiguration Config { get; set; }

        public int IndexStpt { get; set; }

        public bool IsUnlinked { get; set; }

        public F16CEditSteerpointPageNavArgs(F16CEditSteerpointListPage parent, IMapControlVerbMirror mirror,
                                             F16CConfiguration config, int indexStpt, bool isUnlinked)
            => (ParentEditor, VerbMirror,
                Config, IndexStpt, IsUnlinked) = (parent, mirror, config, indexStpt, isUnlinked);
    }
}
