// ********************************************************************************************************************
//
// PointOfInterestType.cs : jafdtc point of interest type
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

namespace JAFDTC.Models.POI
{
    /// <summary>
    /// types for points of interest.
    /// </summary>
    public enum PointOfInterestType
    {
        UNKNOWN = -1,
        SYSTEM = 0,             // system pois
        USER = 1,               // user-specified pois
        CAMPAIGN = 2            // user-specified pois associated with a campaign
    }

    /// <summary>
    /// type mask for PointOfInterestType enum.
    /// </summary>
    [Flags]
    public enum PointOfInterestTypeMask
    {
        NONE = 0,
        ANY = -1,
        SYSTEM = 1 << PointOfInterestType.SYSTEM,
        USER = 1 << PointOfInterestType.USER,
        CAMPAIGN = 1 << PointOfInterestType.CAMPAIGN
    }
}
