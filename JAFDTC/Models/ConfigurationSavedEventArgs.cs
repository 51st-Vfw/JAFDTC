// ********************************************************************************************************************
//
// ConfigurationSavedEventArgs.cs -- arguments for configuration saved events
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2026 ilominar/raven
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

using System;

namespace JAFDTC.Models
{
    /// <summary>
    /// event arguments for a "configuration saved" event posted when IConfiguration.Save() is called. the InvokedBy
    /// field identifies the object that invoked the save operation on the configuration.
    /// </summary>
    public class ConfigurationSavedEventArgs : EventArgs
    {
        public object InvokedBy { get; }

        public IConfiguration Config { get; }

        public string SyncSysTag { get; }

        public ConfigurationSavedEventArgs(object invokedBy, IConfiguration config, string syncSysTag)
            => (InvokedBy, Config, SyncSysTag) = (invokedBy, config, syncSysTag);
    }
}
