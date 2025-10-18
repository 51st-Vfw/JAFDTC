﻿// ********************************************************************************************************************
//
// EditWaypointListHelperBase.cs : base class for EditNavPointListPage helper classes
//
// Copyright(C) 2023-2025 fizzle
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
using JAFDTC.Models.DCS;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.Base
{
    internal abstract class EditWaypointListHelperBase : IEditNavpointListPageHelper
    {
        public abstract string SystemTag { get; }
        public abstract string NavptListTag { get; }
        public abstract AirframeTypes AirframeType { get; }
        public virtual string NavptName => "Waypoint";
        public virtual LLFormat NavptCoordFmt => LLFormat.DD;
        public virtual Type NavptEditorType => typeof(EditNavpointPage);
        public abstract int NavptMaxCount { get; }

        public abstract int NavptCurrentCount(IConfiguration config);
        public virtual int NavptRemainingCount(IConfiguration config) => NavptMaxCount - NavptCurrentCount(config);
        public abstract int AddNavpoint(IConfiguration config, int atIndex = -1);
        public abstract void AppendFromPOIsToConfig(IEnumerable<PointOfInterest> pois, IConfiguration config);
        public abstract void CaptureNavpoints(IConfiguration config, WyptCaptureDataRx.WyptCaptureData[] wypts, int startIndex);
        public abstract void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit);
        public abstract bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config);
        public abstract string ExportNavpoints(IConfiguration config);
        public abstract object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror, IConfiguration config, int indexNavpt);
        public abstract INavpointSystemImport NavptSystem(IConfiguration config);
        public abstract bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false);
        public abstract void ResetSystem(IConfiguration config);
        public virtual void SetupUserInterface(IConfiguration config, ListView listView) { }
    }
}
