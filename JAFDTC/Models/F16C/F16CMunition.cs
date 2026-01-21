// ********************************************************************************************************************
//
// F16CMunition.cs -- Properties of F-16C weapons, hydrated from FileManager.LoadF16CMunitions().
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2024-2026 ilominar/raven, fizzle
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

using JAFDTC.Models.F16C.SMS;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// information on a munition for the viper including descriptive information useful for the ui and avionics
    /// defaults for avionics systems that configure the munition (eg, stores management system).
    /// </summary>
    public sealed class F16CMunition
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties are deserialized from DB JSON

        public SMSSystem.Munitions ID { get; set; }             // unique ID used in configuration files

        public string Label { get; set; }                       // munition label, human readable

        public string Description { get; set; }                 // munition long(ish) description, human readable

        public string[] LabelSMS { get; set; }                  // munition labels used by sms page

        public string[] LabelSCL { get; set; }                  // munition labels used in scl

        public string Image { get; set; }                       // munition image for ui, relative to Images/

        public MunitionSettings MunitionInfo { get; set; }      // munition information/settings for sms

        // ---- following properties are synthesized.

        [JsonIgnore]
        public string ImageFullPath => "/Images/" + Image;
    }
}
