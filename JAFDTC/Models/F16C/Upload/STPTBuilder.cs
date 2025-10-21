// ********************************************************************************************************************
//
// STPTBuilder.cs -- f-16c steerpoint command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.STPT;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the steerpoint system through the ded/ufc according to an
    /// F16CConfiguration. the stream returns the ded to its default page. the builder does not require any state to
    /// function.
    /// </summary>
    internal class STPTBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb)
                   : F16CBuilderBase(cfg, dm, sb), IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------


        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure steerpoint system via the ded/ufc according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            ObservableCollection<SteerpointInfo> stpts = _cfg.STPT.Points;

            if (stpts.Count == 0)
                return;

            AddExecFunction("NOP", [ "==== STPTBuilder:Build()" ]);

            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            Dictionary<string, SteerpointInfo> jetStpts = [ ];
            for (var i = 0; i < stpts.Count; i++)
                jetStpts.Add(stpts[i].Number.ToString(), stpts[i]);

            List<string> theaters = TheatersForNavpoints([.. stpts ]);
            int zulu = (theaters.Count > 0) ? PointOfInterest.TheaterInfo[theaters[0]].Zulu : 0;

            BuildWaypoints(ufc, jetStpts, -zulu);     // negate zulu to get offset from local to zulu
            BuildVIP(ufc, jetStpts);
            BuildVRP(ufc, jetStpts);

            SelectDEDPageDefault(ufc);
        }

        /// <summary>
        /// add the set of waypoints (given by a dictionary that maps steerpoint_number:steerpoint) to the current
        /// set of navigation points using the ufc. this will enter both the steerpoint as well as any oap's tied
        /// to the steerpoint.
        /// <summary>
        private void BuildWaypoints(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts, int dZ)
        {
            AddActions(ufc, [ "LIST", "1", "SEQ" ]);

            foreach (KeyValuePair<string, SteerpointInfo> kv in jetStpts)
            {
                string stptId = kv.Key;
                SteerpointInfo stpt = kv.Value;

                if (stpt.IsValid)
                {
                    string tos = AdjustHMSForZulu(stpt.TOS, dZ);

                    AddActions(ufc, PredActionsForNumAndEnter(stptId), [ "DOWN" ]);
                    AddActions(ufc, ActionsFor2864CoordinateString(stpt.LatUI), [ "ENTR", "DOWN" ]);
                    AddActions(ufc, ActionsFor2864CoordinateString(stpt.LonUI), [ "ENTR", "DOWN" ]);
                    AddActions(ufc, PredActionsForNumAndEnter(stpt.Alt), [ "DOWN" ]);
                    AddActions(ufc, PredActionsForNumAndEnter(tos, false, true), [ "DOWN" ]);

                    if ((stpt.OAP[0].Type == RefPointTypes.OAP) || (stpt.OAP[1].Type == RefPointTypes.OAP))
                    {
                        AddAction(ufc, "SEQ");
                        if (stpt.OAP[0].Type == RefPointTypes.OAP)
                            BuildOA(ufc, stptId, stpt.OAP[0].Range, stpt.OAP[0].Brng, stpt.OAP[0].Elev);
                        AddAction(ufc, "SEQ");
                        if (stpt.OAP[1].Type == RefPointTypes.OAP)
                            BuildOA(ufc, stptId, stpt.OAP[1].Range, stpt.OAP[1].Brng, stpt.OAP[1].Elev);
                        AddActions(ufc, [ "SEQ", "SEQ" ]);
                    }
                }
            }
            AddActions(ufc, [ "1", "ENTR", "RTN" ]);
        }

        /// <summary>
        /// build the set of commands necessary to enter a single oap into the steerpoint system.
        /// <summary>
        private void BuildOA(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(elev, false, true), [ "DOWN" ]);
        }

        /// <summary>
        /// build the set of commands necessary to enter an vip into the steerpoint system.
        /// <summary>
        private void BuildVIP(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts)
        {
            string stptNum = null;
            SteerpointInfo stpt = null;

            foreach (KeyValuePair<string, SteerpointInfo> kvp in jetStpts)
            {
                if (kvp.Value.VxP[0].Type == RefPointTypes.VIP)
                {
                    stptNum = kvp.Key;
                    stpt = kvp.Value;
                    break;
                }
            }
            if (stptNum != null)
            {
                AddActions(ufc, [ "RTN", "RTN", "LIST", "3" ], null, WAIT_BASE);

                AddIfBlock("IsI2TNotSelected", true, null, delegate () { AddAction(ufc, "SEQ"); });
                AddIfBlock("IsI2TNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });
                BuildVIPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AddAction(ufc, "SEQ");

                AddIfBlock("IsI2PNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });
                BuildVIPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);

                // TODO: not needed?
                // AddAction(ufc, "SEQ");
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vip into
        /// the steerpoint system.
        /// <summary>
        private void BuildVIPDetail(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddAction(ufc, "DOWN");
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(elev, false, true), [ "DOWN" ]);
        }

        /// <summary>
        /// build the set of commands necessary to enter an vrp into the steerpoint system.
        /// <summary>
        private void BuildVRP(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts)
        {
            string stptNum = null;
            SteerpointInfo stpt = null;

            foreach (KeyValuePair<string, SteerpointInfo> kvp in jetStpts)
            {
                if (kvp.Value.VxP[0].Type == RefPointTypes.VRP)
                {
                    stptNum = kvp.Key;
                    stpt = kvp.Value;
                }
            }
            if (stptNum != null)
            {
                AddActions(ufc, [ "RTN", "RTN", "LIST", "9" ], null, WAIT_BASE);

                AddIfBlock("IsT2RNotSelected", true, null, delegate () { AddAction(ufc, "SEQ"); });
                AddIfBlock("IsT2RNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });

                BuildVRPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AddAction(ufc, "SEQ");

                AddIfBlock("IsT2PNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });

                BuildVRPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);
                AddActions(ufc, [ "SEQ", "RTN" ]);
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vrp into
        /// the steerpoint system.
        /// <summary>
        private void BuildVRPDetail(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddAction(ufc, "DOWN");
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), [ "DOWN" ]);
            AddActions(ufc, PredActionsForNumAndEnter(elev, true, true), [ "DOWN" ]);
        }

        /// <summary>
        /// returns a list of theaters that cover the list of navpoints. the list is sorted in order of membership:
        /// first index is the theater with the most matches, last index is the theater with the least.
        /// </summary>
        public static List<string> TheatersForNavpoints(List<INavpointInfo> navpts)
        {
            Dictionary<string, int> theaterMap = [];
            foreach (INavpointInfo navpt in navpts)
                foreach (string theater in PointOfInterest.TheatersForCoords(navpt.Lat, navpt.Lon))
                    theaterMap[theater] = theaterMap.GetValueOrDefault(theater, 0) + 1;

            Dictionary<int, List<string>> freqMap = [];
            foreach (KeyValuePair<string, int> kvp in theaterMap)
                if (freqMap.ContainsKey(kvp.Value))
                    freqMap[kvp.Value].Add(kvp.Key);
                else
                    freqMap[kvp.Value] = [kvp.Key];

            List<string> theaters = [];
            foreach (int freq in freqMap.Keys.OrderByDescending(k => k))
                foreach (string theater in freqMap[freq])
                    theaters.Add(theater);

            return theaters;
        }
    }
}
