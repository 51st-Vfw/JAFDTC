// ********************************************************************************************************************
//
// F16CEditSteerpointListHelper.xaml.cs : IEditNavpointListPageHelper for the f-16c configuration
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
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// helper class that implements IEditNavpointListPageHelper to support the f16c waypoint list editor in the ui.
    /// this helper works with the specialized F16CEditSteerpointListPage implementation and thus provides empty
    /// implementations of methods from the interface that are not used.
    /// </summary>
    internal class F16CEditSteerpointListHelper : EditWaypointListHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // IEditNavpointListPageHelper
        //
        // ------------------------------------------------------------------------------------------------------------

        public override INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((F16CConfiguration)config).STPT;
        }

        public override NavpointSystemInfo SystemInfo => STPTSystem.SystemInfo;

        public override object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror,
                                              IConfiguration config, int indexNavpt)
        {
            Debug.Assert(false);
            return null;
        }

        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            Debug.Assert(false);
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            Debug.Assert(false);
            return false;
        }

        public override void ResetSystem(IConfiguration config)
        {
            Debug.Assert(false);
        }

        public override int AddNavpoint(IConfiguration config, int atIndex = -1)
        {
            Debug.Assert(false);
            return 0;
        }

        public override void AddNavpointsFromPOIs(IEnumerable<PointOfInterest> pois, IConfiguration config)
        {
            F16CConfiguration f16Config = (F16CConfiguration)config;
            ObservableCollection<SteerpointInfo> points = f16Config.STPT.Points;
            int startNumber = (points.Count == 0) ? 1 : points[^1].Number + 1;
            foreach (PointOfInterest poi in pois)
            {
                SteerpointInfo wypt = new()
                {
                    Number = startNumber++,
                    Name = poi.Name,
                    Lat = poi.Latitude,
                    Lon = poi.Longitude,
                    Alt = poi.Elevation
                };
                f16Config.STPT.Points.Add(new SteerpointInfo(wypt));
            }
        }

        public override bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            Debug.Assert(false);
            return false;
        }

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            Debug.Assert(false);
        }
    }
}
