
// ********************************************************************************************************************
//
// MapSetupData.cs -- map window setup data
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

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures setup of map window controls.
    /// </summary>
    public sealed class MapSetupData
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapFilterSpec Filter { get; set; }

        public MapImportSpec Import { get; set; }

        public bool IsSnapMode { get; set; }

        public bool IsLabelMode { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapSetupData() => (Filter, Import, IsSnapMode, IsLabelMode) = (new(), new(), false, false);
    }
}
