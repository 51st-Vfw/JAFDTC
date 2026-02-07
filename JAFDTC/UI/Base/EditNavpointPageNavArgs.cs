// ********************************************************************************************************************
//
// EditNavpointPageNavArgs.cs : navigation arguments for general navigation point editor page
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

using JAFDTC.Models;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml.Controls;
using System;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// navigation argument for pages that push to the EditNavpointPage navpoint editor. this identifies the specific
    /// navpoint being edited. this class enables targeting the general EditNavpointPage to a specific airframe.
    /// </summary>
    public sealed class EditNavpointPageNavArgs
    {
        public Page ParentEditor { get; }                       // parent editor (typically a navpoint list)

        public IMapControlVerbMirror VerbMirror { get; set; }   // map window (may be null)

        public IConfiguration Config { get; }                   // configuration

        public int IndexNavpt { get; }                          // index of navpoint being edited

        public bool IsUnlinked { get; }                         // true => navpoints not linked to other configuration

        public string NavptName { get; }                        // "name" of a navpoint ("Waypoint", "Steerpoint", etc.)

        public Type EditorHelperType { get; }                   // helper class for EditNavpointPage

        public EditNavpointPageNavArgs(Page parent, IMapControlVerbMirror mirror, IConfiguration config, int index,
                                       bool isUnlinked, Type helper)
            => (ParentEditor, VerbMirror, Config,
                IndexNavpt, IsUnlinked, EditorHelperType) = (parent, mirror, config, index, isUnlinked, helper);
    }
}
