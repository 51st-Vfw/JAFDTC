// ********************************************************************************************************************
//
// PointOfInterest.cs -- point of interest model
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

// define this to use human-readalbe tags.
//
#define noDEBUG_POI_UID_HUMAN_FRIENDLY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// types for points of interest.
    /// </summary>
    public enum PointOfInterestType
    {
        UNKNOWN = -1,
        DCS_CORE = 0,
        USER = 1,
        CAMPAIGN = 2
    }

    /// <summary>
    /// type mask for PointOfInterestType enum.
    /// </summary>
    [Flags]
    public enum PointOfInterestTypeMask
    {
        NONE = 0,
        ANY = -1,
        DCS_CORE = 1 << PointOfInterestType.DCS_CORE,
        USER = 1 << PointOfInterestType.USER,
        CAMPAIGN = 1 << PointOfInterestType.CAMPAIGN
    }

    // ================================================================================================================

    /// <summary>
    /// defines the properties of a point of interest (poi) known to jafdtc. these instances are managed by the poi
    /// database (PointOfInterestDbase). pois include a theater (set based on lat/lon), optional campaign name,
    /// semicolon-separated list of tags, and a lat/lon/elev. the tuple (type, theater, name) must be unique across
    /// the database.
    /// </summary>
    public sealed class PointOfInterest
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterestType Type { get; set; }           // poi type (airfield, etc)

        // NOTE: Theater may not be null or empty and should be consistent with Latitude and Longitude.

        public string Theater { get; set; }                     // theater (general geographic area)

        public string Campaign { get; set; }                    // campaign name (null unless Type is CAMPAIGN)
        
        public string Name { get; set; }                        // name

        public string Tags { get; set; }                        // tags (";"-separated list)

        public string Latitude { get; set; }                    // latitude (decimal degrees)
        
        public string Longitude { get; set; }                   // longitude (decimal degrees)
        
        public string Elevation { get; set; }                   // elevation (feet)

#if DEBUG_POI_UID_HUMAN_FRIENDLY

        public string UniqueID => $"{(int)Type}:{Theater.ToLower()}:{Name.ToLower()}:{SanitizedTags(Tags).ToLower()}";

#else

        public string UniqueID
        {
            get
            {
                string uid = $"{(int)Type}:{Theater.ToLower()}:{Name.ToLower()}:{SanitizedTags(Tags).ToLower()}";
                byte[] hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(uid));
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

#endif

        public override string ToString()
        {
            return (Name != null) ? $"{Name}" : "";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterest()
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (PointOfInterestType.UNKNOWN, "", "", "", "", "", "", "");

        public PointOfInterest(PointOfInterestType type, string theater, string campaign, string name, string tags,
                               string lat, string lon, string elev)
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (type, theater, campaign, name, tags, lat, lon, elev);

        public PointOfInterest(PointOfInterest poi)
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (poi.Type, poi.Theater, poi.Campaign, poi.Name, poi.Tags, poi.Latitude, poi.Longitude, poi.Elevation);

        /// <summary>
        /// constructs a point of interest from a line of csv text. format of the line is,
        ///
        ///     [name],[tags],[campaign],[latitude],[longitude],[elevation]
        ///     
        /// where the Theater is inferred from the decimal [latitude] and [longitude] and set to null if there are
        /// multiple potential theaters. if the string is unable to be parsed, the PointOfInterest is set to
        /// PointOfInterestType.UNKNOWN.
        /// 
        /// NOTE: this can construct a poi with a null Theater. it is the caller's responsibility to clean that up.
        /// </summary>
        public PointOfInterest(string csv)
        {
            Type = PointOfInterestType.UNKNOWN;
            Theater = "";
            Campaign = "";
            Name = "";
            Tags = "";
            Latitude = "";
            Longitude = "";
            Elevation = "";

            string[] cols = csv.Split(",");
            if (cols.Length == 6)
            {
                string lat = cols[3].Trim();
                string lon = cols[4].Trim();
                List<string> theaters = JAFDTC.Models.DCS.Theater.TheatersForCoords(lat, lon);
                if ((theaters != null) && (theaters.Count >= 1))
                {
                    Theater = (theaters.Count == 1) ? theaters[0] : null;
                    Name = cols[0].Trim();
                    Tags = cols[1].Trim();
                    Campaign = cols[2].Trim();
                    Latitude = lat;
                    Longitude = lon;
                    Elevation = cols[5].Trim();
                    Type = (string.IsNullOrEmpty(Campaign)) ? PointOfInterestType.USER : PointOfInterestType.CAMPAIGN;
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the type of the poi matches a type mask, false otherwise.
        /// </summary>
        public bool IsMatchTypeMask(PointOfInterestTypeMask mask)
        {
            return mask.HasFlag((PointOfInterestTypeMask)(1 << (int)Type));
        }

        /// <summary>
        /// return sanitized tag string with empty tags removed, extra spaces removed, etc.
        /// </summary>
        public static string SanitizedTags(string tags)
        {
            string cleanTags = tags ?? "";
            if (!string.IsNullOrEmpty(tags))
            {
                cleanTags = "";
                foreach (string value in tags.Split(';').ToList<string>())
                {
                    string newValue = value.Trim();
                    if (newValue.Length > 0)
                        cleanTags += $"; {newValue}";
                }
                if (cleanTags.Length >= 3)
                    cleanTags = cleanTags[2..];
            }
            return cleanTags;
        }
    }
}
