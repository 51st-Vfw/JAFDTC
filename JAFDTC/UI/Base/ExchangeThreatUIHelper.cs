// ********************************************************************************************************************
//
// ExchangeThreatUIHelper.cs : helper class for poi exchange (import/export) ui
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.Threats;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// threats files.
    /// </summary>
    public partial class ExchangeThreatUIHelper : ExchangeUIHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // threat export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// export a list of user threats to a .jafdtc_db file at the given path (null path causes prompt via picker
        /// for non-null root). notes success and failure via a message dialog if root is not null.
        /// </summary>
        public static async void ExportFileForUser(XamlRoot root, List<Threat> threats, string path = null)
        {
            Debug.Assert((root != null) || (path == null));

            path ??= await SavePickerUI("Export Threats", "JAFDTC Threats", "JAFDTC Db", ".jafdtc_db");
            if ((path != null) && (threats.Count > 0))
            {
                bool isSuccess = FileManager.SaveSharableDatabase(path, threats);
                string what = (threats.Count > 1) ? "threats" : "threat";
                string message = $"Exported {threats.Count} {what} from JAFDTC User threats to the database" +
                                 $" file:\n\n{path}";
                ExchangeResultUI(root, isSuccess, "Export", "POIs", "to", path, message);

                if (!isSuccess)
                    FileManager.Log($"ExchangeThreatUIHelper:ExportFileForUser SaveSharableDatabase fails");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // threat import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// import threats from a .jafdtc_db file providing status information to user as needed. returns true on
        /// success, false on error, null on cancel.
        /// 
        /// user interaction disabled if root is null.
        /// </summary>
        public static async Task<bool?> ImportFile(XamlRoot root, PointOfInterestDbase dbase, string path = null)
        {
            Debug.Assert((root != null) || (path != null));

            path ??= await OpenPickerUI("Import Threats", [ ".jafdtc_db" ]);
            if (path == null)
                return null;                                    // EXITS: picker canceled

            try
            {
                List<Threat> importThreats = FileManager.LoadSharableDbase<Threat>(path) ?? [];
                if (importThreats.Count == 0)
                    throw new Exception("Cannot load or parse the import file");

                HashSet<string> knownTypes = [ ];
                ThreatDbaseQuery query = new(null, null, [ ThreatType.USER ]);
                foreach (Threat threat in ThreatDbase.Instance.Find(query))
                    knownTypes.Add(threat.UnitTypeDCS);

                int matches = 0;
                foreach (Threat threat in importThreats)
                    if (knownTypes.Contains(threat.UnitTypeDCS))
                        matches++;

                if (matches > 0)
                {
                    ContentDialogResult result = await Utilities.Message2BDialog(
                        root,
                        $"Threats Exists",
                        $"Import file contains threats that are already in the database. Update those threats to" +
                        $" match the imported data?",
                        "Update",
                        "Cancel");
                    if (result == ContentDialogResult.None)
                        return null;                            // EXITS: import canceled
                }

                foreach (Threat threat in importThreats)
                {
                    if (knownTypes.Contains(threat.UnitTypeDCS))
                        ThreatDbase.Instance.RemoveThreat(threat, false);
                    ThreatDbase.Instance.AddThreat(threat, false);
                }
                ThreatDbase.Instance.Save();

                string what = (importThreats.Count > 1) ? "threats" : "threat";
                string msg = $"Imported {importThreats.Count} {what}.";
                ExchangePOIUIHelper.ExchangeResultUI(root, true, "Import", "Threats", "from", null, msg);
            }
            catch (Exception ex)
            {
                FileManager.Log($"ExchangeThreatUIHelper:ImportFile exception {ex}");
                ExchangeResultUI(root, false, "Import", "Threat", "from", path, $"{ex.Message}. ");
                return false;
            }

            return true;
        }
    }
}
