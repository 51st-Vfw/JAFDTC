﻿// ********************************************************************************************************************
//
// ImportHelper.cs -- abstract base class for helpers to import navpoints
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

using JAFDTC.Models.Base;
using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// abstract base class for helpers for import data sources. implements the IImportHelper interface. base class
    /// provides a helper that does not support flights.
    /// </summary>
    public abstract class ImportHelper : IImportHelper
    {
        public virtual bool HasFlights => false;

        public virtual List<string> Flights() => new();

        public virtual Dictionary<string, string> OptionTitles(string what = "Steerpoint") => null;

        public virtual Dictionary<string, object> OptionDefaults => null;

        public abstract bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true,
                                    Dictionary<string, object> options = null);
    }
}
