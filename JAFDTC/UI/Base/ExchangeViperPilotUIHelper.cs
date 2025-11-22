// ********************************************************************************************************************
//
// ExchangeViperPilotUIHelper.cs : helper class for viper pilot exchange (import/export) ui
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

using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// viper driver database files.
    /// </summary>
    public partial class ExchangeViperPilotUIHelper : ExchangeUIHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // viper export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// export a viper pilot database to a path and inform the user of the results. if path is null, prompts the
        /// user for a file location via a FileSavePicker. returns null on cancel, true on success, and false on
        /// failure.
        /// 
        /// user interaction (with the exceptoin of pickters) is disabled if xaml root is null.
        /// </summary>
        public static async Task<bool?> ExportFile(XamlRoot root, List<ViperDriver> dbase, string path = null)
        {
            // Debug.Assert((root != null) || (path != null));

            path ??= await SavePickerUI("Export Viper Pilots", "Viper Pilots", "JAFDTC Db", ".jafdtc_db");
            if (path != null)
            {
                bool isSuccess = false;
                string msg = null;
                if (FileManager.SaveSharableDatabase(path, dbase))
                {
                    string what = (dbase.Count > 1) ? "pilots" : "pilot";
                    msg = $"Exported {dbase.Count} {what} to the databse file:\n\n{path}";
                    isSuccess = true;
                }
                ExchangeResultUI(root, isSuccess, "Export", "pilots", "to", path, msg);
                return isSuccess;
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // viper import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// import a viper pilot database from a path and inform the user of the results. if path is null, prompts
        /// prompts the user for a file location via a FileOpenPicker. returns null on cancel, true on success, and
        /// false on failure.
        ///
        /// user interaction (with the exception of pickers) is disabled if xaml root is null.
        /// </summary>
        public static async Task<bool?> ImportFile(XamlRoot root, string path = null)
        {
            // Debug.Assert((root != null) || (path != null));

            path ??= await OpenPickerUI("Import Viper Pilots", [ ".jafdtc_db" ]);
            if ((path != null) && (Path.GetExtension(path.ToLower()) == ".jafdtc_db"))
            {
                bool isSuccess = false;
                string msg = null;
                List<ViperDriver> importDb = FileManager.LoadSharableDbase<ViperDriver>(path);
                if (importDb.Count > 0)
                {
                    F16CPilotsDbase.UpdateDbase(importDb);
                    string what = (importDb.Count > 1) ? "pilots" : "pilot";
                    msg = $"Imported {importDb.Count} {what} from from the database file:\n\n{path}";
                    isSuccess = true;
                }
                ExchangeResultUI(root, isSuccess, "Import", "pilots", "from", path, msg);
                return isSuccess;
            }
            return null;
        }
    }
}
