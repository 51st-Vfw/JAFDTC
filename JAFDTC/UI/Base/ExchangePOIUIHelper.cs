// ********************************************************************************************************************
//
// ExchangePOIUIHelper.cs : helper class for poi exchange (import/export) ui
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
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// points of interest files.
    /// </summary>
    public partial class ExchangePOIUIHelper : ExchangeUIHelperBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // general functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// display a dialog with a status message if root is non-null.
        /// </summary>
        public static async void ExchangeResultUI(XamlRoot root, bool isGood, string what, string prep, string path,
                                                  string msg)
        {
            string title = $"{what} Successful";
            if (!isGood)
            {
                title = $"{what} Fails";
                msg = msg ?? "";
                msg = $"{msg}Unable to {what.ToLower()} POIs {prep} the file at:\n\n{path}";
            }
            if (root != null)
                await Utilities.Message1BDialog(root, title, msg);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // poi export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// export a list of user pois to a .jafdtc_db file at the given path (null path causes prompt via picker
        /// for non-null root). notes success and failure via a message dialog if root is not null.
        /// </summary>
        public static async void ExportFileForUser(XamlRoot root, List<PointOfInterest> pois, string path = null)
        {
            Debug.Assert((root != null) || (path == null));

            path ??= await SavePickerUI("Export POIs", "JAFDTC POIs", "JAFDTC Db", ".jafdtc_db");
            if ((path != null) && (pois.Count > 0))
            {
                bool isSuccess = FileManager.SaveSharableDatabase(path, pois);
                string what = (pois.Count > 1) ? "points" : "point";
                string message = $"Exported {pois.Count} {what} of interest from JAFDTC User POIs to the file at:\n\n{path}";
                ExchangeResultUI(root, isSuccess, "Export", "to", path, message);

                if (!isSuccess)
                    FileManager.Log($"ExchangePOIUIHelper:ExportFileForUser SaveSharableDatabase fails");
            }
        }

        /// <summary>
        /// export a list of campaign pois from a single campaign to a .jafdtc_db file at the given path (null path
        /// causes prompt via picker for non-null root). if the entire campaign is not exported, inform user whole
        /// campaign will be exported. notes success and failure via a message dialog if root is not null.
        /// 
        /// user interaction disabled if root is null.
        /// </summary>
        public static async void ExportFileForCampaign(XamlRoot root, List<PointOfInterest> pois, string path = null)
        {
            Debug.Assert((root != null) || (path == null));

            if (pois.Count == 0)
                return;

            // if the export pois do not include the entire campaign, inform the user export will pull the whole
            // campaign (if root is non-null).
            //
            string campaignName = pois[0].Campaign;
            if (PointOfInterestDbase.Instance.CountPoIInCampaign(campaignName) != pois.Count)
            {
                ContentDialogResult result = ContentDialogResult.Primary;
                if (root != null)
                {
                    string msg = $"The selected points of interest do not include all points from campaign" +
                                 $" “{campaignName}”. Would you like to include all points of interest from" +
                                 $" this campaign in the export?";
                    result = await Utilities.Message2BDialog(root, "Partial Export", msg, "Include All Points");
                }
                if (result == ContentDialogResult.None)             // EXIT: user cancels
                    return;

                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, campaignName);
                pois = PointOfInterestDbase.Instance.Find(query);
            }

            path ??= await SavePickerUI("Export POIs", campaignName, "JAFDTC Db", ".jafdtc_db");
            if (path != null)
            {
                bool isSuccess = FileManager.SaveSharableDatabase(path, pois);
                string what = (pois.Count > 1) ? "points" : "point";
                string message = $"Exported {pois.Count} {what} of interest from campaign “{campaignName}” to the file at:\n\n{path}";
                ExchangeResultUI(root, isSuccess, "Export", "to", path, message);

                if (!isSuccess)
                    FileManager.Log($"ExchangePOIUIHelper:ExportFileForCampaign SaveSharableDatabase fails");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // poi import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the path identifies a .csv file (per extension), false otherwise.
        /// </summary>
        private static bool IsPathCSV(string path)
            => System.IO.Path.GetExtension(path).Equals(".csv", StringComparison.CurrentCultureIgnoreCase);

        /// <summary>
        /// load the pois from a file, using either csv import (for .csv files), or the shareable database support
        /// (for .jafdtc_db files). returns the results of the load.
        /// </summary>
        private static async Task<List<PointOfInterest>> LoadPOIFromPath(string path)
        {
            if (IsPathCSV(path))
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                string data = await FileIO.ReadTextAsync(file);
                return PointOfInterestDbase.ParseCSV(data) ?? [ ];
            }
            else
            {
                return FileManager.LoadSharableDbase<PointOfInterest>(path) ?? [ ];
            }
        }

        /// <summary>
        /// cleanup the import of a .csv file. three checks are made, (1) theaters, (2) unique names, (3) poi type
        /// mix. may interact with the user to let them clear up ambiguity. returns the updated poi list on success
        /// (list is empty if the user cancels). throws an exception on errors.
        /// </summary>
        private static async Task<List<PointOfInterest>> CleanupImportCSV(XamlRoot root, List<PointOfInterest> pois)
        {
            List<string> theaters = PointOfInterestDbase.ConsensusTheaters(pois);
            if ((theaters == null) || (theaters.Count == 0))
                throw new Exception("POIs in the import file do not share a common theater");

            // if there isn't a consensus theater for the pois (for example, everything is in the overlapping
            // region between syria and sinai), prompt the user to pick a side. update the pois as necessary.
            //
            string theater = theaters[0];
            if (theaters.Count > 1)
            {
                GetPoITagDetails theaterDialog = new(null, null, theaters, null)
                {
                    XamlRoot = root,
                    Title = "Select Theater for Import",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await theaterDialog.ShowAsync(ContentDialogPlacement.Popup);
                if (result == ContentDialogResult.None)
                    return [ ];                                         // **** EXITS: user cancel

                theater = theaterDialog.Theater;
            }
            foreach (PointOfInterest poi in pois)
                poi.Theater = theater;

            // ensure pois have unique UniqueIDs. we will adjust the name to make this hapen. also, track
            // the makeup of the poi types in the list.
            //
            Dictionary<string, PointOfInterest> uidMap = [ ];
            Dictionary<string, int> campaignMap = [ ];
            bool isUser = false;
            foreach (PointOfInterest poi in pois)
            {
                string baseName = poi.Name;
                int index = 0;
                while (uidMap.ContainsKey(poi.UniqueID))
                    poi.Name = $"{baseName} {++index}";
                uidMap[poi.UniqueID] = poi;

                if (poi.Type == PointOfInterestType.USER)
                    isUser = true;
                if (!string.IsNullOrEmpty(poi.Campaign))
                    campaignMap[poi.Campaign] = campaignMap.GetValueOrDefault(poi.Campaign, 0) + 1;
            }

            // poi list should either be all user or all campaign (from a single campaign). validate this.
            //
            if ((isUser && (campaignMap.Count > 0)) || (campaignMap.Count > 1))
                throw new Exception("Import file mixes POIs from different types or campaigns");

            return pois;
        }

        /// <summary>
        /// import points of interest from a .jafdtc_db or .csv file providing status information to user as
        /// needed. returns list of points of interest imported, empty on error or user cancel.
        /// 
        /// user interaction disabled if root is null.
        /// </summary>
        public static async Task<List<PointOfInterest>> ImportFile(XamlRoot root, PointOfInterestDbase dbase,
                                                                   string path = null)
        {
            path ??= await OpenPickerUI("Import POIs", [ ".jafdtc_db", ".csv" ]);
            if (path == null)
                return [ ];                                     // EXITS: picker canceled

            try
            {
                List<PointOfInterest> importPoIs = await LoadPOIFromPath(path);
                if (importPoIs.Count == 0)
                    throw new Exception("Cannot load or parse the import file");

                // for .csv imports, there may be issues in the import set that we need to clean up such as non-unique names
                // ambiguous theaters, bad poi type mixes, etc. make a pass through the import set to clean up any of those
                // issues.
                //
                if (IsPathCSV(path))
                    importPoIs = await CleanupImportCSV(root, importPoIs);

                // on campaign imports, inform import is going to wipe out any existing campaign with the same name.
                //
                string campaign = importPoIs[0].Campaign;
                if (!string.IsNullOrEmpty(campaign) && (dbase.CountPoIInCampaign(campaign) > 0))
                {
                    if (root != null)
                    {
                        ContentDialogResult result = await Utilities.Message2BDialog(
                            root,
                            $"Campaign Exists",
                            $"Import file contains points of interest for the campaign “{campaign}”, a campaign that" +
                            $" is already in the database. Replace the campaign in the database with the imported data?",
                            "Replace",
                            "Cancel");
                        if (result == ContentDialogResult.None)
                            return [ ];                         // EXITS: import canceled
                    }
                    dbase.DeleteCampaign(campaign, false);
                }
                if (!string.IsNullOrEmpty(campaign) && (dbase.CountPoIInCampaign(campaign) == 0))
                    dbase.AddCampaign(campaign, false);

                // update database. create new pois for those that are not in the database; otherwise, update the
                // existing pois to match import.
                //
                foreach (PointOfInterest poi in importPoIs)
                {
                    PointOfInterest poiCur = PointOfInterestDbase.Instance.Find(poi.UniqueID);
                    if (poiCur != null)
                    {
                        poiCur.Theater = poi.Theater;
                        poiCur.Campaign = poi.Campaign;
                        poiCur.Name = poi.Name;
                        poiCur.Tags = poi.Tags;
                        poiCur.Latitude = poi.Latitude;
                        poiCur.Longitude = poi.Longitude;
                        poiCur.Elevation = poi.Elevation;
                    }
                    else
                    {
                        dbase.AddPointOfInterest(new((string.IsNullOrEmpty(campaign)) ? PointOfInterestType.USER
                                                                                      : PointOfInterestType.CAMPAIGN,
                                                     poi.Theater, poi.Campaign, poi.Name, poi.Tags,
                                                     poi.Latitude, poi.Longitude, poi.Elevation),
                                                 false);
                    }
                }
                dbase.Save(campaign);

                string what = (importPoIs.Count > 1) ? "points" : "point";
                string msg = $"Imported {importPoIs.Count} {what} of interest.";
                if (!string.IsNullOrEmpty(campaign))
                    msg = $"Imported {importPoIs.Count} {what} of interest to campaign “{campaign}”.";
                ExchangePOIUIHelper.ExchangeResultUI(root, true, "Import", "from", null, msg);

                return importPoIs;
            }
            catch (Exception ex)
            {
                FileManager.Log($"ExchangePOIUIHelper:ImportFile exception {ex}");
                ExchangeResultUI(root, false, "Import", "from", path, $"{ex.Message}. ");
            }

            return [ ];
        }
    }
}
