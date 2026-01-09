// ********************************************************************************************************************
//
// PilotDbase.cs -- pilot "database" model
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

using JAFDTC.Models.Pilots;
using JAFDTC.Models.Pilots.Extensions;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// pilot database holds information (Pilot instances) known to jafdtc. the database class is a singleton that
    /// supports find operations to query the known pilots. the database is built from user-defined pilots.
    /// </summary>
    public class PilotDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // singleton
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Lazy<PilotDbase> lazy = new(() => new PilotDbase());

        public static PilotDbase Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // the pilot database is a dictionary that maps string Pilot.UID keys to Pilot instances.
        //
        // note that auxiliary keys are case-insensitive. callers are responsible for managing capitalization.

        private readonly Dictionary<string, Pilot> _dbase;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private PilotDbase()
        {
            _dbase = [];
            Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the single pilot that matches a uid, null if no such pilot exists
        /// </summary>
        public Pilot Find(string uid) => (string.IsNullOrEmpty(uid)) ? null : _dbase.GetValueOrDefault(uid, null);

        /// <summary>
        /// return list of pilots containing all pilots that match the specified query criteria: airframes, names,
        /// and board numbers. results are optionally sorted using SortPilots().
        /// </summary>
        public IReadOnlyList<Pilot> Find(PilotDbaseQuery query = null, bool isSorted = false)
        {
            query ??= new();

            IReadOnlyList<Pilot> pilots = [.. _dbase.Values ];
            IReadOnlyList<Pilot> matches = [.. pilots.LimitAirframes(query.Airframes)
                                              .LimitNames(query.Name)
                                              .LimitExactNames(query.ExactName)
                                              .LimitBoardNums(query.BoardNumber) ];
            return (isSorted) ? SortPoIs(matches) : matches;
        }

        /// <summary>
        /// sort the pilots list by airframe then name. returns the sorted list.
        /// </summary>
        public static IReadOnlyList<Pilot> SortPoIs(IReadOnlyList<Pilot> pilots)
        {
            return [.. pilots.OrderBy(p => p.Airframe).ThenBy(p => p.Name) ];
        }

        /// <summary>
        /// reset the database by clearing its current contents and reloading from storage.
        /// </summary>
        public void Reset()
        {
            _dbase.Clear();
            foreach (Pilot pilot in FileManager.LoadPilotDbase())
                AddPilot(pilot, false);
        }

        /// <summary>
        /// add a pilot to the database, persisting the database to storage if requested. returns true on success,
        /// false on failure.
        /// </summary>
        public bool AddPilot(Pilot pilot, bool isPersist = true)
        {
            if (_dbase.ContainsKey(pilot.UniqueID))
            {
                FileManager.Log($"PilotDbase.AddPilot(): warning: pilot with unique id '{pilot.UniqueID}'" +
                                $" already exists in database ({pilot.Name}); skipping add.");
                return false;
            }

            _dbase[pilot.UniqueID] = pilot;
            if (isPersist)
                Save();

            return true;
        }

        /// <summary>
        /// remove a pilot from the database, persisting the database to storage if requested.
        /// </summary>
        public void RemovePilot(Pilot pilot, bool isPersist = true)
        {
            _dbase.Remove(pilot.UniqueID);
            if (isPersist)
                Save();
        }

        /// <summary>
        /// persist points of interest to storage. a null campaign persists the user pois, a non-null campaign persists
        /// the pois for the specified campaign. the isCullCampaign parameter controls whether or not empty campaigns
        /// are removed from storage.
        /// </summary>
        public bool Save()
        {
            return FileManager.SavePilotDbase([.. _dbase.Values ]);
        }
    }
}
