// ********************************************************************************************************************
//
// ThreatDbase.cs -- threat database
//
// Copyright(C) 2025 ilominar/raven
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// threat database holds information (Threat instances) known to jafdtc. the database class is a singleton
    /// that supports find operations to query the known threats. the database is built from fixed dcs threats as
    /// well as user-defined threats.
    /// </summary>
    public class ThreatDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // singleton
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Lazy<ThreatDbase> lazy = new(() => new ThreatDbase());

        public static ThreatDbase Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // the threat database is a dictionary that maps ThreatDbase keys (the primary key) to dictionary values. the
        // dictionary value maps string keys (the dcs unit type) onto a Threat.

        private readonly Dictionary<ThreatType, Dictionary<string, Threat>> _dbase;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private ThreatDbase()
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
        /// reset the database by clearing its current contents and reloading from storage.
        /// </summary>
        public void Reset()
        {
            _dbase.Clear();

            List<Threat> threats = FileManager.LoadThreats();
            foreach (Threat threat in threats)
                AddThreat(threat, false);
        }

        /// <summary>
        /// return the threat in the database that matches the specified dcs type, null if no match is found. the
        /// method reports the user threat if there are both user and dcs core threats matching the type.
        /// </summary>
        public Threat Find(string dcsType)
        {
            if (string.IsNullOrEmpty(dcsType))
            {
                return null;
            }
            else if (_dbase.TryGetValue(ThreatType.USER, out Dictionary<string, Threat> userThreats) &&
                     userThreats.TryGetValue(dcsType, out Threat userThreat))
            {
                return userThreat;
            }
            else if (_dbase.TryGetValue(ThreatType.DCS_CORE, out Dictionary<string, Threat> coreThreats) &&
                     coreThreats.TryGetValue(dcsType, out Threat coreThreat))
            {
                return coreThreat;
            }
            return null;
        }

        /// <summary>
        /// add a threat to the database, persisting the database to storage if requested. returns true on success,
        /// false on failure.
        /// </summary>
        public bool AddThreat(Threat threat, bool isPersist = true)
        {
            if (!_dbase.TryGetValue(threat.Type, out Dictionary<string, Threat> threats))
                _dbase[threat.Type] = new() { [threat.TypeDCS] = threat };
            else if (!threats.ContainsKey(threat.TypeDCS))
                threats[threat.TypeDCS] = threat;
            else
                return false;

            if (isPersist)
                Save();

            return true;
        }

        /// <summary>
        /// remove a threat from the database, persisting the database to storage if requested.
        /// </summary>
        public void RemoveThreat(Threat threat, bool isPersist = true)
        {
            if ((threat.Type == ThreatType.USER) &&
                _dbase.TryGetValue(threat.Type, out Dictionary<string, Threat> threats) &&
                threats.ContainsKey(threat.TypeDCS))
            {
                threats.Remove(threat.TypeDCS);
                if (isPersist)
                    Save();
            }
        }

        /// <summary>
        /// persist threats to storage. this only updates user threats to the user threat dbase. returns true on
        /// success, false on failure.
        /// </summary>
        public bool Save()
        {
            if (_dbase.TryGetValue(ThreatType.USER, out Dictionary<string, Threat> threats))
                return FileManager.SaveUserThreats([.. threats.Values ]);
            return true;
        }
    }
}
