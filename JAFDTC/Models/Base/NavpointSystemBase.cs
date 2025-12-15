// ********************************************************************************************************************
//
// NavpointSystemBase.cs -- navigation point system abstract base class
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

using JAFDTC.Models.CoreApp;
using JAFDTC.Models.Units;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a navigation point system (such as steerpoints or waypoints), system consists of an
    /// array of navigation points of type T (where T should be conform to INavpointInfo, provide new(), and is
    /// typically derived from the NavpointInfoBase abstract base class).
    /// </summary>
    public abstract class NavpointSystemBase<T> : SystemBase, INavpointSystemImport
                                                  where T : class, INavpointInfo, new()
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        [JsonIgnore]
        public virtual NavpointSystemInfo SysInfo { get; }

        // ---- INotifyPropertyChanged properties

        public virtual ObservableCollection<T> Points { get; set; }

        // ---- synthesized properties

        /// <summary>
        /// returns true if the instance indicates a default setup (no navpoints are defined), false otherwise.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault => (Points.Count == 0);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the navpoint system to defaults by removing all navpoints.
        /// </summary>
        public override void Reset()
        {
            Points.Clear();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // INavpointSystemImport functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the identifer for the current route, null for systems that support only a single route.
        /// 
        /// derived classes must override this if they support routes.
        /// </summary>
        public virtual string NavptCurrentRoute() => null;

        /// <summary>
        /// return the number of navpoints currently defined for the given route (null => all routes).
        /// 
        /// derived classes must override this if they support routes.
        /// </summary>
        public virtual int NavptCurrentCount(string route = null) => Points.Count;

        /// <summary>
        /// return the number of navpoints available (i.e., the number that could be added without breaking system
        /// limits) for the given route (null => all routes).
        /// 
        /// derived classes must override this if they support routes.
        /// </summary>
        public virtual int NavptAvailableCount(string route = null) => Math.Max(0, SysInfo.NavptMaxCount - Points.Count);

        /// <summary>
        /// deserialize an array of navpoints from .json and incorporate them into the navpoint list. the deserialized
        /// navpoints can either replace the existing navpoints or be appended to the end of the navpoint list. returns
        /// true on success, false on error (previous navpoints preserved on errors).
        /// </summary>
        public virtual bool ImportSerializedNavpoints(string json, bool isReplace = true)
        {
            ObservableCollection<T> prevPoints = Points;
            try
            {
                ObservableCollection<T> navpts = JsonSerializer.Deserialize<ObservableCollection<T>>(json);
                if (isReplace)
                    Points.Clear();
                foreach (T navpt in navpts)
                    Add(navpt);
                return true;
            }
            catch
            {
                Points = prevPoints;
            }
            return false;
        }

        /// <summary>
        /// incorporate a list of navpoints specified by unit position instances into the navpoint list for the
        /// current route. the new navpoints can either replace the existing navpoints or be appended to the end of
        /// the navpoint list. returns true on success, false on error (previous navpoints preserved on errors).
        /// </summary>
        public virtual bool ImportUnitPositionList(IReadOnlyList<UnitPositionItem> posnList, bool isReplace = true)
        {
            ObservableCollection<T> prevPoints = new(Points);
            try
            {
                if (isReplace)
                    Points.Clear();
                AddNavpointsFromPositionList(posnList);
                return true;
            }
            catch
            {
                Points = prevPoints;
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// add navpoints to the system according to the list of position instances. note that not all fields in a
        /// navpoint for a specific system may be able to be set with a position instance.
        /// </summary>
        public abstract void AddNavpointsFromPositionList(IReadOnlyList<UnitPositionItem> posnList);

        /// <summary>
        /// returns the number of navpoints in the system.
        /// </summary>
        [JsonIgnore]
        public int Count { get => Points.Count; }

        /// <summary>
        /// returns index of given navpoint.
        /// </summary>
        public virtual int IndexOf(T navpt)
        {
            return Points.IndexOf(navpt);
        }

        /// <summary>
        /// renumber the points in the system starting from the given starting number.
        /// </summary>
        public virtual void RenumberFrom(int startNumber)
        {
            for (int i = 0; i < Count; i++)
                Points[i].Number = startNumber + i;
        }

        /// <summary>
        /// add an existing navpoint to the navpoint list at the specified index (default is end of list).
        /// returns the navpoint added.
        /// </summary>
        public virtual T Add(T navpt, int atIndex = -1)
        {
            if (atIndex == -1)
                Points.Add(navpt);
            else
                Points.Insert(atIndex, navpt);
            return navpt;
        }

        /// <summary>
        /// remove the given navpoint from the list of navpoints.
        /// </summary>
        public virtual void Delete(T navpt)
        {
            Points.Remove(navpt);
        }
    }
}
