using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F16C
{
    internal class F16CEditSteerpointListHelper : EditWaypointListHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string SystemTag => STPTSystem.SystemTag;

        public override string NavptListTag => STPTSystem.STPTListTag;

        public override AirframeTypes AirframeType => AirframeTypes.F16C;

        public override LLFormat NavptCoordFmt => LLFormat.DDM_P3ZF;

// TODO: validate maximum navpoint count
        public override int NavptMaxCount => int.MaxValue;

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override int NavptCurrentCount(IConfiguration config) => ((F16CConfiguration)config).STPT.Count;

        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            Debug.Assert(false);
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            Debug.Assert(false);
            return false;
        }

        public override INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((F16CConfiguration)config).STPT;
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

        public override string ExportNavpoints(IConfiguration config)
        {
            Debug.Assert(false);
            return null;
        }

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            Debug.Assert(false);
        }

        public override object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror,
                                              IConfiguration config, int indexNavpt)
        {
            Debug.Assert(false);
            return null;
        }
    }
}
