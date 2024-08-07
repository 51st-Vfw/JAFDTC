﻿// ********************************************************************************************************************
//
// NavpointInfoBase.cs -- navigation point information abstract base class
//
// Copyright(C) 2023-2024 ilominar/raven
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
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a navigation point description. this inclues number, name, lat, lon, and altitude.
    /// the lat and lon are always given in decimal degrees. derived classes are responsible for converting between
    /// dd and the airframe-appropriate format by over-riding LatUI, LonUI as necessary. the class provides functions
    /// to convert between common formats.
    /// 
    /// a NavpointInfoBase can only be set to valid coordinates, it can be reset to an invalid/empty state using the
    /// Reset() method.
    /// </summary>
    public abstract class NavpointInfoBase : BindableObject, INavpointInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties, posts change/validation events

        private int _number;                        // positive integer > 1
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value, (value < 1) ? "Invalid number format" : null);
        }

        private string _name;                       // string
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, null);
        }

        private string _lat;                        // string, decimal degrees (raw, no units)
        public string Lat
        {
            get => _lat;
            set
            {
                string error = "Invalid latitude DD format";
                if (IsDecimalFieldValid(value, -90.0, 90.0, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lat, value, error);
            }
        }

        public virtual string LatUI                 // string, decimal degrees (raw, no units)
        {
            get => Lat;
            set => Lat = value;
        }

        private string _lon;                        // string, decimal degrees (raw, no units)
        public string Lon
        {
            get => _lon;
            set
            {
                string error = "Invalid longitude DD format";
                if (IsDecimalFieldValid(value, -180.0, 180.0, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lon, value, error);
            }
        }

        public virtual string LonUI                 // string, decimal degrees
        {
            get => Lon;
            set => Lon = value;
        }

        // NOTE: to allow derived classes to override the default range check via the public Alt accessor, define the
        // NOTE: backing store as protected. derived classes that want to check a different range need only override
        // NOTE: the public accessor.

        protected string _alt;                      // positive integer, on [-1500, 80000]
        public virtual string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -80000, 80000, false))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        // ---- public properties, computed

        // NOTE: IsValid provides checks for widest possible ranges, smaller ranges can still use this version of
        // NOTE: the function as long as the set accessors guarantee any narrower range.

        [JsonIgnore]
        public virtual bool IsValid => (IsIntegerFieldValid(_alt, -80000, 80000, false) &&
                                        IsDecimalFieldValid(_lat, -90.0, 90.0, false) &&
                                        IsDecimalFieldValid(_lon, -180.0, 180.0, false));

        [JsonIgnore]
        public virtual bool IsEmpty => (string.IsNullOrEmpty(_name) && 
                                        string.IsNullOrEmpty(_alt) &&
                                        string.IsNullOrEmpty(_lat) &&
                                        string.IsNullOrEmpty(_lon));

        [JsonIgnore]
        public virtual string Location => ((string.IsNullOrEmpty(Lat)) ? "Unknown" : Coord.RemoveLLDegZeroFill(LatUI)) + ", " +
                                          ((string.IsNullOrEmpty(Lon)) ? "Unknown" : Coord.RemoveLLDegZeroFill(LonUI)) + " / " +
                                          ((string.IsNullOrEmpty(Alt)) ? "Unknown" : Alt + "’");

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public NavpointInfoBase() => (Name, Lat, Lon, Alt) = ("", "", "", "");

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the steerpoint to default values. the Number field is not changed.
        /// </summary>
        public virtual void Reset()
        {
            Name = "";

            // set accessors treat "" as illegal. to work around the way SetProperty() handles updating backing store
            // and error state, first set the fields to "" with no error to set backing store. then, use the set
            // accessor with a known bad to set error state (which will not update backing store).
            //
            SetProperty(ref _lat, "", null, nameof(Lat));
            SetProperty(ref _lon, "", null, nameof(Lon));
            SetProperty(ref _alt, "", null, nameof(Alt));

            Lat = null;
            Lon = null;
            Alt = null;
        }
    }
}
