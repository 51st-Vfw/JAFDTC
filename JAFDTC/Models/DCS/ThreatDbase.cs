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

using JAFDTC.Models.CoreApp;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class ThreatDbaseQuery
    {
        public string[]? TypesDCS;
        public ThreatType[]? ThreatTypes;
        public UnitCategoryType[]? Categories;
        public CoalitionType[]? Coalitions;
        public string Name;

        public bool IsSingleMatch;

        public ThreatDbaseQuery(string[]? typesDCS = null, string name = null, ThreatType[]? threatTypes = null,
                                UnitCategoryType[]? categories = null, CoalitionType[]? coalitions = null,
                                bool isSingleMatch = false)
            => (TypesDCS, Name, ThreatTypes, Categories, Coalitions, IsSingleMatch)
                = (typesDCS, name, threatTypes, categories, coalitions, isSingleMatch);
    }

    // ================================================================================================================

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

        private readonly List<Threat> _dbase;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private ThreatDbase()
        {
            _dbase = [ ];
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
            _dbase.AddRange(FileManager.LoadThreats());
        }

        /// <summary>
        /// return the threats in the database that matches the specified criteria.
        /// </summary>
        public IReadOnlyList<Threat> Find(ThreatDbaseQuery query = null, bool isSort = false)
        {
            query ??= new();
            List<Threat> matches = [.. _dbase.LimitThreatTypes(query.ThreatTypes)
                                             .LimitNames(query.Name)
                                             .LimitCategories(query.Categories)
                                             .LimitDCSTypes(query.TypesDCS)
                                             .LimitCoalitions(query.Coalitions) ];
            if (query.IsSingleMatch)
            {
// TODO: handle isSingleMatch
            }
            if (isSort)
                matches = [.. matches.OrderBy(x => x.Type)
                                     .ThenBy(x => x.Coalition)
                                     .ThenBy(x => x.Category)
                                     .ThenBy(x => x.Name) ];

            return matches;
        }

        /// <summary>
        /// add a threat to the database, persisting the database to storage if requested. returns true on success,
        /// false on failure.
        /// </summary>
        public bool AddThreat(Threat threat, bool isPersist = true)
        {
            IReadOnlyList<Threat> matches = Find(new([ threat.TypeDCS ], null, [ threat.Type ]));
            if ((matches.Count == 0) && isPersist)
                Save();

            return (matches.Count > 0);
        }

        /// <summary>
        /// remove a threat from the database, persisting the database to storage if requested.
        /// </summary>
        public void RemoveThreat(Threat threat, bool isPersist = true)
        {
            if (threat.Type == ThreatType.USER)
            {
                IReadOnlyList<Threat> matches = Find(new([ threat.TypeDCS ], null, [ threat.Type ]));
                if (matches.Count > 0)
                {
                    _dbase.Remove(threat);
                    if (isPersist)
                        Save();
                }
            }
        }

        /// <summary>
        /// persist threats to storage. this only updates user threats to the user threat dbase. returns true on
        /// success, false on failure.
        /// </summary>
        public bool Save()
        {
            IReadOnlyList<Threat> matches = Find(new(null, null, [ ThreatType.USER ]));
            return (matches.Count == 0) || FileManager.SaveUserThreats([.. matches ]);
        }
    }
}
