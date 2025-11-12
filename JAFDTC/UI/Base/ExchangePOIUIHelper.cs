// ********************************************************************************************************************
//
// ExchangePOIUIHelper.cs : helper classes for poi exchange (import/export) ui
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
using System.Threading.Tasks;
using Windows.Storage;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// helper class to provide a number of static support functions for use in the user interface around exchanging
    /// points of interest files.
    /// </summary>
    public partial class ExchangePOIUIHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // general functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
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
        /// manage a FileSavePicker to select a .jafdtc_db file to save to. returns the path of the selected file,
        /// null if no selection was made.
        /// </summary>
        private static async Task<string> SavePickerUI(string filename = null)
        {
            FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Export POIs",
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = filename ?? "JAFDTC POIs"
            };
            picker.FileTypeChoices.Add("JAFDTC Db", [".jafdtc_db"]);
            PickFileResult resultPick = await picker.PickSaveFileAsync();
            return resultPick?.Path;
        }

        /// <summary>
        /// export a list of user pois to a .jafdtc_db file at the given path (null path causes prompt via picker
        /// for non-null root). notes success and failure via a message dialog if root is not null.
        /// </summary>
        public static async void POIExportUserJAFDTCDb(XamlRoot root, List<PointOfInterest> pois, string path = null)
        {
            Debug.Assert((root != null) || (path == null));

            path ??= await SavePickerUI(null);
            if ((path != null) && (pois.Count > 0))
            {
                bool isSuccess = FileManager.SaveSharableDatabase(path, pois);
                string what = (pois.Count > 1) ? "points" : "point";
                string message = $"Exported {pois.Count} {what} of interest from JAFDTC User POIs to the file at:\n\n{path}";
                ExchangeResultUI(root, isSuccess, "Export", "to", path, message);

                if (!isSuccess)
                    FileManager.Log($"ExchangePOIUIHelper:ConfigExportUserPOIJAFDTCDb SaveSharableDatabase fails");
            }
        }

        /// <summary>
        /// export a list of campaign pois from a single campaign to a .jafdtc_db file at the given path (null path
        /// causes prompt via picker for non-null root). if the entire campaign is not exported, asks user if they
        /// want to export the whole campaign. notes success and failure via a message dialog if root is not null.
        /// 
        /// no ui behavior (ie, root is null) only imports specified pois.
        /// </summary>
        public static async void POIExportCampaignJAFDTCDb(XamlRoot root, List<PointOfInterest> pois, string path = null)
        {
            Debug.Assert((root != null) || (path == null));

            if (pois.Count == 0)
                return;

            // if the export pois do not include the entire campaign, ask the user if they want to pull the whole
            // campaign (if root is non-null).
            //
            ContentDialogResult result = ContentDialogResult.Secondary;
            string campaignName = pois[0].Campaign;
            if ((root != null) && (PointOfInterestDbase.Instance.CountPoIInCampaign(campaignName) != pois.Count))
            {
                string msg = $"The export does not include all points of interest from campaign “{campaignName}”." +
                             $" Would you like to include all points of interest from this campaign?";
                result = await Utilities.Message3BDialog(root, "Partial Export", msg, "All", "Only Selected");
            }
            if (result == ContentDialogResult.None)             // EXIT: user cancels
            {
                return;
            }
            else if (result != ContentDialogResult.Secondary)   // add all
            {
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, campaignName);
                pois = PointOfInterestDbase.Instance.Find(query);
            }

            path ??= await SavePickerUI(campaignName);
            if (path != null)
            {
                bool isSuccess = FileManager.SaveSharableDatabase(path, pois);
                string what = (pois.Count > 1) ? "points" : "point";
                string message = $"Exported {pois.Count} {what} of interest from campaign “{campaignName}” to the file at:\n\n{path}";
                ExchangeResultUI(root, isSuccess, "Export", "to", path, message);

                if (!isSuccess)
                    FileManager.Log($"ExchangePOIUIHelper:POIExportCampaignJAFDTCDb SaveSharableDatabase fails");
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
        /// manage a FileSavePicker to select a .jafdtc_db or .csv file to load from. returns the path of the selected
        /// file, null if no selection was made.
        /// </summary>
        private static async Task<string> OpenPickerUI()
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Import POIs",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".jafdtc_db");
            picker.FileTypeFilter.Add(".csv");
            PickFileResult resultPick = await picker.PickSingleFileAsync();
            return resultPick?.Path;
        }

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

            // poi list should either be all user or all campaign (from a single campaign). check this.
            //
            if ((isUser && (campaignMap.Count > 0)) || (campaignMap.Count > 1))
                throw new Exception("Import file mixes POIs from different types or campaigns");

            return pois;
        }

        /// <summary>
        /// import points of interest from a .jafdtc_db or .csv file. user is prompted for a name for the new configuration
        /// along with a pilot role (if roles are supported).
        /// </summary>
        public static async Task<List<PointOfInterest>> POIImportFile(XamlRoot root, string path = null)
        {
            path ??= await OpenPickerUI();
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

                return (IsPathCSV(path)) ? await CleanupImportCSV(root, importPoIs) : importPoIs;
            }
            catch (Exception ex)
            {
                FileManager.Log($"ExchangePOIUIHelper:POIImportFile exception {ex}");
                ExchangeResultUI(root, false, "Import", "from", path, $"{ex.Message}. ");
            }

            return [ ];
        }
    }
}
