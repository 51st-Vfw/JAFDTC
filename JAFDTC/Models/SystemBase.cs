// ********************************************************************************************************************
//
// SystemBase.cs : abstract base class for system configurations
//
// Copyright(C) 2024-2026 fizzle, ilominar/raven
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

using JAFDTC.Models.Planning;
using JAFDTC.Utilities;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace JAFDTC.Models
{
    /// <summary>
    /// abstract base class for an object that carries configuration information for a system or portion of a
    /// system. derived classes must provide implementations for IsDefault and Reset() and may override base
    /// MergeIntoSimDTC() and MergeIntoMission() implementations.
    /// </summary>
    public abstract class SystemBase : BindableObject, ISystem
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // abstract method signatures
        //
        // ------------------------------------------------------------------------------------------------------------

        public abstract bool IsDefault { get; }

        public abstract void Reset();

        // ------------------------------------------------------------------------------------------------------------
        //
        // virtual method base implementations
        //
        // ------------------------------------------------------------------------------------------------------------

        // base implementations of the merge methods are functionally nops. subclasses for mergable systems should
        // override implementations as necessary to provide desired functionality.

        public virtual JsonNode MergeIntoSimDTC(JsonNode dataRoot) => dataRoot;

        public virtual Mission MergeIntoMission(Mission mission, int indexPackage = 0, int indexFlight = 0) => mission;

        // ------------------------------------------------------------------------------------------------------------
        //
        // property validate and set methods
        //
        // ------------------------------------------------------------------------------------------------------------

        private const string INVALID_FORMAT = "Invalid format";

        protected void ValidateAndSetIntProp(string value, int min, int max, ref string property,
                                             [CallerMemberName] string propertyName = null)
        {
            string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, min, max)) ? null : INVALID_FORMAT;
            SetProperty(ref property, value, error, propertyName);
        }

        protected void ValidateAndSetBoolProp(string value, ref string property,
                                              [CallerMemberName] string propertyName = null)
        {
            string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : INVALID_FORMAT;
            SetProperty(ref property, value, error, propertyName);
        }
    }
}
