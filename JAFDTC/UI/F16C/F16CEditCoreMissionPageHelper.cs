// ********************************************************************************************************************
//
// F16CEditCoreMissionPageHelper.cs : viper specialization for EditCoreMissionPage helper object
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
using JAFDTC.Models.Core;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.Models.Planning;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// helper class for airframe-specific customizations on EditCoreMissionPage.
    /// </summary>
    internal class F16CEditCoreMissionPageHelper : IEditCoreMissionPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CoreMissionSystem.SystemTag, "Mission", "Mission", UI.Glyphs.Mission, typeof(EditCoreMissionPage),
                   typeof(F16CEditCoreMissionPageHelper));

        public AirframeTypes Airframe => AirframeTypes.F16C;

        public NavpointSystemInfo NavptSystemInfo => STPTSystem.SystemInfo;

        public SystemBase GetSystemConfig(IConfiguration config) => ((F16CConfiguration)config).Mission;

        public void CopyConfigToEdit(IConfiguration config, CoreMissionSystem editMsn)
        {
            F16CConfiguration viperConfig = (F16CConfiguration)config;

            editMsn.Callsign = new(viperConfig.Mission.Callsign);
            editMsn.Ships = viperConfig.Mission.Ships;
            editMsn.Tasking = new(viperConfig.Mission.Tasking);
            editMsn.PilotUIDs = new string[4];
            for (int i = 0; i < editMsn.Ships; i++)
            {
                editMsn.PilotUIDs[i] = new(viperConfig.Mission.PilotUIDs[i]);
                editMsn.Loadouts[i] = new(viperConfig.Mission.Loadouts[i]);
            }
            editMsn.ThreatSource = new(viperConfig.Mission.ThreatSource);
            editMsn.Threats = [ ];
            foreach (Threat threat in viperConfig.Mission.Threats)
                editMsn.Threats.Add(new()
                {
                    Coalition = threat.Coalition,
                    Name = new(threat.Name),
                    Type = new(threat.Type),
                    Location = new()
                    {
                        Latitude = new(threat.Location.Latitude),
                        Longitude = new(threat.Location.Longitude),
                        Altitude = new(threat.Location.Altitude)
                    },
                    WEZ = threat.WEZ
                });
        }

        public void CopyEditToConfig(CoreMissionSystem editMsn, IConfiguration config)
        {
            F16CConfiguration viperConfig = (F16CConfiguration)config;

            viperConfig.Mission.Callsign = new(editMsn.Callsign);
            viperConfig.Mission.Ships = editMsn.Ships;
            viperConfig.Mission.Tasking = new(editMsn.Tasking);
            viperConfig.Mission.PilotUIDs = new string[4];
            for (int i = 0; i < editMsn.Ships; i++)
            {
                viperConfig.Mission.PilotUIDs[i] = new(editMsn.PilotUIDs[i]);
                viperConfig.Mission.Loadouts[i] = new(editMsn.Loadouts[i]);
            }
            viperConfig.Mission.ThreatSource = new(editMsn.ThreatSource);
            viperConfig.Mission.Threats = [ ];
            foreach (Threat threat in editMsn.Threats)
                viperConfig.Mission.Threats.Add(new()
                {
                    Coalition = threat.Coalition,
                    Name = new(threat.Name),
                    Type = new(threat.Type),
                    Location = new()
                    {
                        Latitude = new(threat.Location.Latitude),
                        Longitude = new(threat.Location.Longitude),
                        Altitude = new(threat.Location.Altitude)
                    },
                    WEZ = threat.WEZ
                });
        }

        public int NumNavpoints(IConfiguration config) => ((F16CConfiguration)config).STPT.Points.Count;

        public INavpointInfo GetNavpoint(IConfiguration config, string route, int index)
            => ((F16CConfiguration)config).STPT.Points[index];

        public Dictionary<string, List<INavpointInfo>> GetAllNavpoints(IConfiguration config)
            => new()
            {
                [ NavptSystemInfo.RouteNames[0] ] = [.. ((F16CConfiguration)config).STPT.Points ]
            };

        public void AddNavpoint(IConfiguration config, string route, int index, string lat, string lon)
        {
            F16CConfiguration viperConfig = (F16CConfiguration)config;
            SteerpointInfo stpt = viperConfig.STPT.Add(null, index);
            stpt.Lat = lat;
            stpt.Lon = lon;
        }

        public void MoveNavpoint(IConfiguration config, string route, int index, string lat, string lon)
        {
            F16CConfiguration viperConfig = (F16CConfiguration)config;
            viperConfig.STPT.Points[index].Lat = lat;
            viperConfig.STPT.Points[index].Lon = lon;
        }

        public void RemoveNavpoint(IConfiguration config, string route, int index)
        {
            F16CConfiguration viperConfig = (F16CConfiguration)config;
            viperConfig.STPT.Delete(viperConfig.STPT.Points[index]);
        }
    }
}
