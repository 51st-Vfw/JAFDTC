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
using JAFDTC.Models.Planning;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;

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

        public SystemBase GetSystemConfig(IConfiguration config) => ((F16CConfiguration)config).Mission;

        public void CopyConfigToEdit(IConfiguration config, CoreMissionSystem editMsn)
        {
            editMsn.Callsign = new(((F16CConfiguration)config).Mission.Callsign);
            editMsn.Ships = ((F16CConfiguration)config).Mission.Ships;
            editMsn.Tasking = new(((F16CConfiguration)config).Mission.Tasking);
            editMsn.PilotUIDs = new string[4];
            for (int i = 0; i < editMsn.Ships; i++)
            {
                editMsn.PilotUIDs[i] = new(((F16CConfiguration)config).Mission.PilotUIDs[i]);
                editMsn.Loadouts[i] = new(((F16CConfiguration)config).Mission.Loadouts[i]);
            }
            editMsn.ThreatSource = new(((F16CConfiguration)config).Mission.ThreatSource);
            editMsn.Threats = [ ];
            foreach (Threat threat in ((F16CConfiguration)config).Mission.Threats)
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
            ((F16CConfiguration)config).Mission.Callsign = new(editMsn.Callsign);
            ((F16CConfiguration)config).Mission.Ships = editMsn.Ships;
            ((F16CConfiguration)config).Mission.Tasking = new(editMsn.Tasking);
            ((F16CConfiguration)config).Mission.PilotUIDs = new string[4];
            for (int i = 0; i < editMsn.Ships; i++)
            {
                ((F16CConfiguration)config).Mission.PilotUIDs[i] = new(editMsn.PilotUIDs[i]);
                ((F16CConfiguration)config).Mission.Loadouts[i] = new(editMsn.Loadouts[i]);
            }
            ((F16CConfiguration)config).Mission.ThreatSource = new(editMsn.ThreatSource);
            ((F16CConfiguration)config).Mission.Threats = [ ];
            foreach (Threat threat in editMsn.Threats)
                ((F16CConfiguration)config).Mission.Threats.Add(new()
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
    }
}
